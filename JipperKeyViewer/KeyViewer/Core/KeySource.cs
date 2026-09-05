// Central key-state source. Two facts drive the design:
// 1) Windows keeps an OS-level async key-state table (user32 GetAsyncKeyState) that reflects
//    hardware AND injected presses; Unity's message-queue Input misses the injected ones.
// 2) Injected replay taps can be shorter than one frame, so per-frame sampling never observes
//    them — they must be captured on a millisecond-resolution background poller (the approach
//    community key viewers verified against replay mods), then latched long enough (~35 ms)
//    for this mod's per-frame consumers to see a full press/release cycle. Genuine holds are
//    not delayed: the latch window is anchored to the press START, so a hold longer than the
//    window releases instantly. Non-Windows or unfocused windows fall back to Unity Input.
// / 中心化按键状态源。设计基于两个事实：
// 1) Windows 维护操作系统级异步按键状态表（user32 GetAsyncKeyState），硬件与注入按压都
//    会反映其中；而基于消息队列的 Unity Input 看不到注入的按压。
// 2) 回放类注入的按压时长可能短于一帧，逐帧采样永远观察不到——必须在毫秒级分辨率的后
//    台轮询线程上捕获（社区按键显示器已对回放 Mod 验证可行的方案），并闩锁约 35ms，让本
//    Mod 逐帧消费的组件能看到完整的一次按下/释放。真实长按不会被延迟：闩锁窗口锚定在按
//    下起点，长于窗口的按压立即释放。非 Windows 或窗口失焦时回落 Unity Input。

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    internal static class KeySource
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int LatchMs = 35;   // min time a press stays visible / 按压最少可见时长

        // Diagnostic probe (temporary): per-frame dump of every input layer to
        // Mods/JipperKeyViewer/keysource-probe.log. Leave on until replay visibility is
        // confirmed, then remove. / 诊断探针（临时）：逐帧转储全部输入层到
        // Mods/JipperKeyViewer/keysource-probe.log。回放可见性确认后移除。
        private const bool ProbeEnabled = true;

        private static readonly bool IsWindows =
            Environment.OSVersion.Platform == PlatformID.Win32NT;

        // Written by the poll thread, read by the main thread. Aligned bool/long field access
        // is atomic on x64; staleness of one poll interval (1-2 ms) is irrelevant here.
        // 由轮询线程写、主线程读。x64 上对齐的 bool/long 字段访问是原子的；
        // 一个轮询间隔（1-2ms）的陈旧度在此无关紧要。
        private static readonly bool[] _down = new bool[256];
        private static readonly long[] _downSinceMs = new long[256];
        private static readonly long[] _visibleUntilMs = new long[256];
        private static long _nowMs; // plain field: aligned 64-bit writes are atomic / 对齐 64 位写为原子操作
        private static volatile bool _running;
        private static Thread _pollThread;

        // ---------------- replay-hit synthesis / 回放命中合成 ----------------
        // Some replay systems drive the game's judgment directly without touching ANY input
        // layer (proven by probe: OS state, Unity Input, game masks and event queue all stay
        // silent during replay). The only observable trace is level progress: each frame the
        // floor cursor advances is one hit. When no physical key was seen recently, such an
        // advance flashes one of the layout's main keys (alternating), so counters, KPS and
        // rain behave like a real press. Auto-floor chains advance without hits and are
        // skipped. / 某些回放系统直接驱动判定，完全不经过任何输入层（探针已证实：回放期间
        // OS 状态表、Unity Input、游戏掩码与事件队列全部无动静）。唯一可观测痕迹是关卡进度：
        // 地板游标推进的那一帧即一次命中。近期无物理按键时，该推进让布局主键之一（交替）闪烁
        // 一帧，计数、KPS、雨滴与真实按压行为一致。auto 地板链自动推进、无命中，跳过。
        private static KeyCode[] _replayKeys;
        private static int _replayParity;
        private static KeyCode _synthKey;
        private static int _synthUntilFrame = -1;
        private static int _lastSeq = -1;
        private static int _pulseFrame = -1;
        private static long _lastPhysMs = -1000; // any non-mouse VK down, per poll thread / 轮询线程记录任意非鼠标 VK 按下
        private const int SynthQuietMs = 150;    // physical input within this window suppresses synthesis / 该窗口内有物理输入则不合成
        private const int SynthMaxKeys = 12;     // above this a hit can't be attributed to a key / 超过则命中无法归因到键

        /// <summary>Register the layout's main keys as synthesis candidates (mouse-free,
        /// KeyCode.None-free, ≤12 entries; null/empty clears). Content-compared, so callers
        /// may pass their per-frame list. / 注册布局主键作为合成候选（不含鼠标与空键、≤12 个；
        /// null/空清除）。按内容比较，调用方可每帧传列表。</summary>
        public static void SetReplayKeys(IList<KeyCode> keys)
        {
            if (keys == null || keys.Count == 0 || keys.Count > SynthMaxKeys)
            {
                _replayKeys = null;
                return;
            }
            if (SameKeys(_replayKeys, keys)) return;
            var copy = new KeyCode[keys.Count];
            int n = 0;
            for (int i = 0; i < keys.Count; i++)
                if (keys[i] != KeyCode.None) copy[n++] = keys[i];
            _replayKeys = Shrink(copy, n);
        }

        private static bool SameKeys(KeyCode[] a, IList<KeyCode> b)
        {
            if (a == null || b == null || a.Length != b.Count) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        private static KeyCode[] Shrink(KeyCode[] src, int n)
        {
            if (n == 0) return null;
            var dst = new KeyCode[n];
            Array.Copy(src, dst, n);
            return dst;
        }

        private static void UpdateReplayPulse(int frame)
        {
            if (frame == _pulseFrame || _pulseBroken) return;
            _pulseFrame = frame;
            try
            {
                var ctl = scrController.instance;
                if (ctl == null || ctl.state != States.PlayerControl)
                {
                    _lastSeq = -1;
                    _synthKey = KeyCode.None;
                    return;
                }
                int seq = ctl.currentSeqID;
                if (_lastSeq < 0) { _lastSeq = seq; return; }   // entering state → baseline / 进入状态 → 建立基线
                bool advanced = seq > _lastSeq;
                _lastSeq = seq;
                if (!advanced || _replayKeys == null || _replayKeys.Length == 0) return;
                // Auto chains advance without hits — same check the game's own progress UI
                // uses. / auto 链自动推进、无命中——与游戏自身进度界面的判据相同。
                var floor = ctl.currFloor;
                if (floor != null && floor.nextfloor != null && floor.nextfloor.auto) return;
                if (_nowMs - _lastPhysMs < SynthQuietMs) return; // physical play → real keys already flash / 物理游玩 → 真实按键已在闪
                _replayParity = (_replayParity + 1) % _replayKeys.Length;
                _synthKey = _replayKeys[_replayParity];
                _synthUntilFrame = frame + 1; // visible for this frame's polls / 本帧各轮询可见
            }
            catch
            {
                _pulseBroken = true;   // game internals unavailable → feature off for good / 游戏内部不可用 → 永久停用
                _synthKey = KeyCode.None;
            }
        }

        private static bool _pulseBroken;

        private static bool SynthActive(KeyCode code)
        {
            return code == _synthKey
                && Time.frameCount <= _synthUntilFrame
                && _nowMs - _lastPhysMs >= SynthQuietMs;
        }

        public static bool GetKey(KeyCode code)
        {
            EnsurePollThread();
            if (ProbeEnabled) ProbeThisFrame();
            UpdateReplayPulse(Time.frameCount);
            if (IsWindows)
            {
                int[] vks;
                if (VkTable.Value.TryGetValue(code, out vks))
                {
                    long now = _nowMs;
                    foreach (int vk in vks)
                    {
                        if (_down[vk] || now < _visibleUntilMs[vk])
                            return true;
                    }
                    if (SynthActive(code)) return true;
                    return false;
                }
                // unmapped key → legacy poll / 未映射键回落轮询
            }
            if (SynthActive(code)) return true;
            return Input.GetKey(code);
        }

        private static void EnsurePollThread()
        {
            if (!IsWindows || _running) return;
            _running = true;
            _pollThread = new Thread(PollLoop) { IsBackground = true, Name = "JipperKeyViewer.KeyPoll" };
            _pollThread.Start();
        }

        private static void PollLoop()
        {
            var sw = Stopwatch.StartNew();
            while (_running)
            {
                long now = sw.ElapsedMilliseconds;
                _nowMs = now;
                for (int vk = 1; vk < _down.Length; vk++)
                {
                    if ((GetAsyncKeyState(vk) & 0x8000) != 0)
                    {
                        if (!_down[vk])
                        {
                            _down[vk] = true;
                            _downSinceMs[vk] = now;
                        }
                        _visibleUntilMs[vk] = _downSinceMs[vk] + LatchMs;
                        if (vk > 0x06) _lastPhysMs = now; // mouse excluded / 排除鼠标
                    }
                    else if (_down[vk])
                    {
                        _down[vk] = false;
                    }
                }
                // Spin-yield to the next millisecond: Thread.Sleep(1) can round up to the OS
                // timer resolution (~15.6 ms) and miss multi-millisecond injected taps.
                // 自旋让出到下一毫秒：Thread.Sleep(1) 会取整到系统定时器分辨率（约15.6ms），
                // 从而漏掉仅数毫秒的注入按压。
                while (_running && sw.ElapsedMilliseconds == now)
                    Thread.Yield();
            }
        }

        // ---------------- diagnostic probe / 诊断探针 ----------------
        // One line per frame (on change, plus heartbeat) recording every input layer, so a
        // single replay run pinpoints where replay-injected presses actually surface.
        // 每帧一行（有变化时记录，另加心跳），覆盖全部输入层——一次回放即可定位注入按压
        // 真正出现的位置。
        private static int _probeFrame = -1;
        private static int _probeBeat;
        private static string _probeLast = "";
        private static StreamWriter _probeWriter;
        private static bool _probeBroken;

        private static void ProbeThisFrame()
        {
            int frame = Time.frameCount;
            if (frame == _probeFrame) return;
            _probeFrame = frame;
            if (_probeBroken) return;
            try
            {
                string line = BuildProbeLine(frame);
                if (line == _probeLast && ++_probeBeat % 120 != 0) return;
                _probeLast = line;
                if (_probeWriter == null)
                {
                    string path = Path.Combine(Loader.ModPath ?? ".", "keysource-probe.log");
                    _probeWriter = new StreamWriter(path, false) { AutoFlush = true };
                }
                _probeWriter.WriteLine(line);
            }
            catch (Exception e)
            {
                _probeBroken = true;
                try { Loader.Warning("KeySource probe disabled: " + e.Message); } catch { }
            }
        }

        private static string BuildProbeLine(int frame)
        {
            var sb = new StringBuilder(160);
            sb.Append("t=").Append(frame)
              .Append("|foc=").Append(Application.isFocused ? 1 : 0)
              .Append("|uAny=").Append(Input.anyKey ? 1 : 0)
              .Append("|uAnyD=").Append(Input.anyKeyDown ? 1 : 0)
              .Append("|os=").Append(OsHeldList());
            try
            {
                sb.Append("|hook=").Append(AsyncInputManager.isActive ? 1 : 0)
                  .Append("|q=").Append(AsyncInputManager.keyQueue.Count)
                  .Append("|held=").Append(MaskList(AsyncInputManager.keyMask))
                  .Append("|down=").Append(MaskList(AsyncInputManager.keyDownMask))
                  .Append("|fd=").Append(MaskList(AsyncInputManager.frameDependentKeyMask))
                  .Append("|fdd=").Append(MaskList(AsyncInputManager.frameDependentKeyDownMask));
                var ctl = scrController.instance;
                sb.Append("|state=").Append(ctl == null ? "-" : ctl.state.ToString())
                  .Append("|seq=").Append(ctl == null ? "-" : ctl.currentSeqID.ToString())
                  .Append("|synth=").Append(_synthKey == KeyCode.None ? "0" : _synthKey.ToString());
            }
            catch (Exception e)
            {
                sb.Append("|gameErr=").Append(e.GetType().Name);
            }
            return sb.ToString();
        }

        private static string OsHeldList()
        {
            int count = 0;
            var sb = new StringBuilder(48);
            for (int vk = 1; vk < _down.Length && count < 8; vk++)
            {
                if (!_down[vk]) continue;
                if (count > 0) sb.Append(',');
                sb.Append(vk.ToString("X2"));
                count++;
            }
            return count == 0 ? "0" : sb.ToString();
        }

        private static string MaskList(HashSet<AsyncKeyCode> mask)
        {
            if (mask == null || mask.Count == 0) return "0";
            var sb = new StringBuilder(64);
            int count = 0;
            foreach (var code in mask)
            {
                if (count >= 8) { sb.Append("…"); break; }
                if (count > 0) sb.Append(',');
                sb.Append(code.label.ToString());
                count++;
            }
            return sb.ToString();
        }

        private static Dictionary<KeyCode, int[]> BuildVkTable()
        {
            var pairs = new List<KeyValuePair<KeyCode, int[]>>();
            void Add(KeyCode code, params int[] vks) => pairs.Add(new KeyValuePair<KeyCode, int[]>(code, vks));

            // Mouse / 鼠标
            Add(KeyCode.Mouse0, 0x01);
            Add(KeyCode.Mouse1, 0x02);
            Add(KeyCode.Mouse2, 0x04);
            Add(KeyCode.Mouse3, 0x05);
            Add(KeyCode.Mouse4, 0x06);

            // Digits & letters / 数字与字母
            Add(KeyCode.Alpha0, 0x30); Add(KeyCode.Alpha1, 0x31); Add(KeyCode.Alpha2, 0x32);
            Add(KeyCode.Alpha3, 0x33); Add(KeyCode.Alpha4, 0x34); Add(KeyCode.Alpha5, 0x35);
            Add(KeyCode.Alpha6, 0x36); Add(KeyCode.Alpha7, 0x37); Add(KeyCode.Alpha8, 0x38);
            Add(KeyCode.Alpha9, 0x39);
            Add(KeyCode.A, 0x41); Add(KeyCode.B, 0x42); Add(KeyCode.C, 0x43);
            Add(KeyCode.D, 0x44); Add(KeyCode.E, 0x45); Add(KeyCode.F, 0x46);
            Add(KeyCode.G, 0x47); Add(KeyCode.H, 0x48); Add(KeyCode.I, 0x49);
            Add(KeyCode.J, 0x4A); Add(KeyCode.K, 0x4B); Add(KeyCode.L, 0x4C);
            Add(KeyCode.M, 0x4D); Add(KeyCode.N, 0x4E); Add(KeyCode.O, 0x4F);
            Add(KeyCode.P, 0x50); Add(KeyCode.Q, 0x51); Add(KeyCode.R, 0x52);
            Add(KeyCode.S, 0x53); Add(KeyCode.T, 0x54); Add(KeyCode.U, 0x55);
            Add(KeyCode.V, 0x56); Add(KeyCode.W, 0x57); Add(KeyCode.X, 0x58);
            Add(KeyCode.Y, 0x59); Add(KeyCode.Z, 0x5A);

            // Navigation & editing / 导航与编辑键
            Add(KeyCode.LeftArrow, 0x25); Add(KeyCode.UpArrow, 0x26);
            Add(KeyCode.RightArrow, 0x27); Add(KeyCode.DownArrow, 0x28);
            Add(KeyCode.Backspace, 0x08); Add(KeyCode.Tab, 0x09);
            Add(KeyCode.Clear, 0x0C); Add(KeyCode.Return, 0x0D);
            Add(KeyCode.Pause, 0x13); Add(KeyCode.CapsLock, 0x14);
            Add(KeyCode.Escape, 0x1B); Add(KeyCode.Space, 0x20);
            Add(KeyCode.PageUp, 0x21); Add(KeyCode.PageDown, 0x22);
            Add(KeyCode.End, 0x23); Add(KeyCode.Home, 0x24);
            Add(KeyCode.Insert, 0x2D); Add(KeyCode.Delete, 0x2E);
            Add(KeyCode.Print, 0x9A);
            Add(KeyCode.Numlock, 0x90); Add(KeyCode.ScrollLock, 0x91);

            // OEM punctuation / 标点
            Add(KeyCode.Slash, 0xBF); Add(KeyCode.Backslash, 0xDC);
            Add(KeyCode.BackQuote, 0xC0); Add(KeyCode.Minus, 0xBD);
            Add(KeyCode.Equals, 0xBB); Add(KeyCode.LeftBracket, 0xDB);
            Add(KeyCode.RightBracket, 0xDD); Add(KeyCode.Semicolon, 0xBA);
            Add(KeyCode.Quote, 0xDE); Add(KeyCode.Comma, 0xBC);
            Add(KeyCode.Period, 0xBE);

            // Modifiers; Korean layouts emit Hangul codes for right Ctrl/Alt, so map both.
            // 修饰键；韩文布局下右 Ctrl/Alt 产生 Hangul 键码，两侧都映射。
            Add(KeyCode.LeftWindows, 0x5B); Add(KeyCode.RightWindows, 0x5C);
            Add(KeyCode.Menu, 0x5D);
            Add(KeyCode.LeftShift, 0xA0); Add(KeyCode.RightShift, 0xA1);
            Add(KeyCode.LeftControl, 0xA2); Add(KeyCode.LeftAlt, 0xA4);
            Add(KeyCode.RightControl, 0xA3, 0x19);
            Add(KeyCode.RightAlt, 0xA5, 0x15);

            // Function row / 功能键
            Add(KeyCode.F1, 0x70); Add(KeyCode.F2, 0x71); Add(KeyCode.F3, 0x72);
            Add(KeyCode.F4, 0x73); Add(KeyCode.F5, 0x74); Add(KeyCode.F6, 0x75);
            Add(KeyCode.F7, 0x76); Add(KeyCode.F8, 0x77); Add(KeyCode.F9, 0x78);
            Add(KeyCode.F10, 0x79); Add(KeyCode.F11, 0x7A); Add(KeyCode.F12, 0x7B);
            Add(KeyCode.F13, 0x7C); Add(KeyCode.F14, 0x7D); Add(KeyCode.F15, 0x7E);
            Add(KeyCode.F16, 0x7F); Add(KeyCode.F17, 0x80); Add(KeyCode.F18, 0x81);
            Add(KeyCode.F19, 0x82); Add(KeyCode.F20, 0x83); Add(KeyCode.F21, 0x84);
            Add(KeyCode.F22, 0x85); Add(KeyCode.F23, 0x86); Add(KeyCode.F24, 0x87);

            // Keypad / 小键盘
            Add(KeyCode.Keypad0, 0x60); Add(KeyCode.Keypad1, 0x61);
            Add(KeyCode.Keypad2, 0x62); Add(KeyCode.Keypad3, 0x63);
            Add(KeyCode.Keypad4, 0x64); Add(KeyCode.Keypad5, 0x65);
            Add(KeyCode.Keypad6, 0x66); Add(KeyCode.Keypad7, 0x67);
            Add(KeyCode.Keypad8, 0x68); Add(KeyCode.Keypad9, 0x69);
            Add(KeyCode.KeypadMultiply, 0x6A); Add(KeyCode.KeypadPlus, 0x6B);
            Add(KeyCode.KeypadEnter, 0x6C); Add(KeyCode.KeypadMinus, 0x6D);
            Add(KeyCode.KeypadPeriod, 0x6E); Add(KeyCode.KeypadDivide, 0x6F);

            // Gamepad buttons (XInput virtual keys) / 手柄按键（XInput 虚拟键）
            Add(KeyCode.JoystickButton0, 0xC3); Add(KeyCode.JoystickButton1, 0xC4);
            Add(KeyCode.JoystickButton2, 0xC5); Add(KeyCode.JoystickButton3, 0xC6);
            Add(KeyCode.JoystickButton4, 0xC8); Add(KeyCode.JoystickButton5, 0xC7);
            Add(KeyCode.JoystickButton6, 0xC9); Add(KeyCode.JoystickButton7, 0xCA);
            Add(KeyCode.JoystickButton8, 0xCF); Add(KeyCode.JoystickButton9, 0xD0);
            Add(KeyCode.JoystickButton10, 0xD1); Add(KeyCode.JoystickButton11, 0xD2);
            Add(KeyCode.JoystickButton12, 0xCB); Add(KeyCode.JoystickButton13, 0xCC);
            Add(KeyCode.JoystickButton14, 0xCD); Add(KeyCode.JoystickButton15, 0xCE);

            var table = new Dictionary<KeyCode, int[]>(pairs.Count);
            foreach (var pair in pairs)
                table[pair.Key] = pair.Value;
            return table;
        }

        private static readonly Lazy<Dictionary<KeyCode, int[]>> VkTable =
            new Lazy<Dictionary<KeyCode, int[]>>(BuildVkTable);
    }
}
