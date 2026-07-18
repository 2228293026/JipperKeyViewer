// Key detection and rebinding logic / 按键检测和重新绑定逻辑
// Handles listening for new key presses during rebinding and converting KeyCodes to display strings / 处理重绑定期间监听新按键，以及将 KeyCode 转换为显示字符串

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>Pre-allocated char buffer for TMP text updates (no per-call ToString allocation) / 预分配的字符缓冲区，用于 TMP 文本更新（无每调用 ToString 分配）</summary>
    internal static class NumBuffer
    {
        private static readonly char[] Buffer = new char[32];

        /// <summary>Write integer into Buffer, return char segment via out params / 将整数写入 Buffer，通过 out 参数返回字符片段</summary>
        public static void Format(int count, bool thousands, out char[] buf, out int offset, out int length)
        {
            int pos = Buffer.Length;
            if (count == 0) { Buffer[--pos] = '0'; buf = Buffer; offset = pos; length = Buffer.Length - pos; return; }
            long val = count;
            if (val < 0) val = -val;
            int seg = 0;
            while (val > 0)
            {
                if (thousands && seg == 3) { Buffer[--pos] = ','; seg = 0; }
                Buffer[--pos] = (char)('0' + val % 10);
                val /= 10;
                seg++;
            }
            if (count < 0) Buffer[--pos] = '-';
            buf = Buffer; offset = pos; length = Buffer.Length - pos;
        }
    }

    /// <summary>
    /// Input processing: key rebinding and display string conversion / 输入处理：按键重绑定和显示字符串转换
    /// </summary>
    public partial class KeyViewer : MonoBehaviour
    {
        /// <summary>
        /// Listen for a key press when the user is rebinding a key / 当用户正在重绑定时监听按键按下
        /// Waits for any key down, then assigns it to the SelectedKey / 等待任意键按下，然后分配给 SelectedKey
        /// </summary>
        private void ProcessKeySelection()
        {
            if (SelectedKey == -1 || changeState == 1 || !Application.isFocused) return;
            if (!Input.anyKeyDown) return;

            foreach (KeyCode keyCode in AllKeyCodes)
            {
                if (Input.GetKeyDown(keyCode))
                {
                    SetupKey(keyCode);
                    return;
                }
            }
        }

        /// <summary>
        /// Assign a key code to the selected slot and update the display / 将按键代码分配给选中的槽位并更新显示
        /// </summary>
        private void SetupKey(KeyCode keyCode)
        {
            if (IsFullKeyboard) return;
            if (SelectedKey < 0) return; // -1 means no key is being rebound; never index arrays with it / -1 表示没有正在重绑定的键，禁止用作索引
            if (changeState == 2)
            {
                KeyCode[] ghostKeyCodes = GetGhostKeyCode();
                if (SelectedKey < ghostKeyCodes.Length)
                {
                    ghostKeyCodes[SelectedKey] = keyCode;
                    if (SelectedKey < ghostKeyStates.Length)
                        ghostKeyStates[SelectedKey] = false;
                }
                SelectedKey = -1;
                SaveSettings();
                return;
            }
            KeyCode[] keyCodes = GetKeyCode();
            KeyCode[] footKeyCodes = GetFootKeyCode();
            string[] keyTexts = GetKeyText();
            if (SelectedKey < FootKeyBase)
            {
                if (SelectedKey < keyCodes.Length)
                    keyCodes[SelectedKey] = keyCode;
            }
            else if (footKeyCodes != null && SelectedKey - FootKeyBase < footKeyCodes.Length)
            {
                footKeyCodes[SelectedKey - FootKeyBase] = keyCode;
            }
            else
            {
                SelectedKey = -1;
                return;
            }
            if (Keys != null && SelectedKey < Keys.Length && Keys[SelectedKey] != null)
            {
                string displayText;
                if (SelectedKey < FootKeyBase && SelectedKey < keyTexts.Length && !string.IsNullOrEmpty(keyTexts[SelectedKey]))
                    displayText = keyTexts[SelectedKey];
                else if (SelectedKey >= FootKeyBase)
                {
                    string[] footTexts = GetFootKeyText();
                    int footIndex = SelectedKey - FootKeyBase;
                    displayText = footTexts != null && footIndex < footTexts.Length && !string.IsNullOrEmpty(footTexts[footIndex])
                        ? footTexts[footIndex] : KeyToString(keyCode);
                }
                else
                    displayText = KeyToString(keyCode);
                Keys[SelectedKey].text.text = displayText;
            }
            SelectedKey = -1;
            SaveSettings();
        }

        static readonly Dictionary<KeyCode, string> KeyDisplayNames = new Dictionary<KeyCode, string>();

        /// <summary>
        /// Convert a Unity KeyCode to a short display-friendly string / 将 Unity KeyCode 转换为简短友好的显示字符串
        /// Uses a pre-built dictionary to avoid per-call allocations / 使用预建字典避免每次调用产生分配
        /// </summary>
        public static string KeyToString(KeyCode keyCode)
        {
            if (KeyDisplayNames.Count == 0 && AllKeyCodes != null)
                BuildKeyDisplayNames();
            return KeyDisplayNames.TryGetValue(keyCode, out var name) ? name : keyCode.ToString();
        }

        static void BuildKeyDisplayNames()
        {
            foreach (KeyCode k in AllKeyCodes)
            {
                string s = StripKeyCodePrefix(k.ToString());
                s = ReplaceKeyCodeSuffix(s);
                s = MapKeyCodeSymbol(s);
                KeyDisplayNames[k] = s;
            }
        }

        static string StripKeyCodePrefix(string s)
        {
            if (s.StartsWith("Alpha")) return s.Substring(5);
            if (s.StartsWith("Keypad")) return s.Substring(6);
            if (s.StartsWith("Left")) return 'L' + s.Substring(4);
            if (s.StartsWith("Right")) return 'R' + s.Substring(5);
            if (s.StartsWith("Mouse")) return "M" + s.Substring(5);
            return s;
        }

        static string ReplaceKeyCodeSuffix(string s)
        {
            if (s.EndsWith("Shift")) return s.Substring(0, s.Length - 5) + "\u21E7";
            if (s.EndsWith("Control")) return s.Substring(0, s.Length - 7) + "Ctrl";
            return s;
        }

        static string MapKeyCodeSymbol(string s)
        {
            return s switch
            {
                "Plus" => "+",
                "Minus" => "-",
                "Multiply" => "*",
                "Divide" => "/",
                "Enter" => "\u21B5",
                "Equals" => "=",
                "Period" => ".",
                "Return" => "\u21B5",
                "None" => " ",
                "Tab" => "\u21E5",
                "Backslash" => "\\",
                "Backspace" => "Back",
                "Slash" => "/",
                "LBracket" => "[",
                "RBracket" => "]",
                "Semicolon" => ";",
                "Comma" => ",",
                "Quote" => "'",
                "UpArrow" => "\u2191",
                "DownArrow" => "\u2193",
                "LArrow" => "\u2190",
                "RArrow" => "\u2192",
                "Space" => "\u2423",
                "BackQuote" => "`",
                "PageDown" => "Pg\u2193",
                "PageUp" => "Pg\u2191",
                "CapsLock" => "\u21EA",
                "Insert" => "Ins",
                _ => s
            };
        }

        /// <summary>Calculate IMGUI text field width based on content length / 根据内容长度计算 IMGUI 文本框宽度</summary>
        private static GUILayoutOption FloatFieldWidth(string text) => GUILayout.Width(Mathf.Max(30, text.Length * 9));

        // ======================== Input Processing (hot path) / 输入处理（热路径） ========================

        /// <summary>
        /// Check each main key and foot key for state changes (press/release) every frame / 每帧检查每个主键和脚键的状态变化（按下/释放）
        /// </summary>
        private void ProcessMainAndFootKeysInUpdate(long elapsedMilliseconds)
        {
            ProfileData d = Settings.Data;
            if (cachedKeyStyle != d.KeyViewerStyle)
            {
                cachedMainKeys = GetKeyCode();
                cachedGhostKeys = GetGhostKeyCode();
                cachedKeyStyle = d.KeyViewerStyle;
                ghostKeyStates = new bool[cachedGhostKeys.Length];
            }
            else if (cachedGhostKeys == null)
            {
                cachedGhostKeys = GetGhostKeyCode();
                ghostKeyStates = new bool[cachedGhostKeys.Length];
            }
            if (cachedFootStyle != d.FootKeyViewerStyle)
            {
                cachedFootKeys = GetFootKeyCode();
                cachedFootStyle = d.FootKeyViewerStyle;
            }
            ProcessKeyGroup(cachedMainKeys, 0, elapsedMilliseconds);
            // Full keyboard uses indices 0-104 for main keys; foot-key indices (24+) overlap real keys,
            // so never process the foot group here or it corrupts main-key press states.
            // 全键盘主键占用 0-104，脚键索引(24+)与真实键重叠，绝不能再处理脚键组，否则污染主键状态。
            if (!IsFullKeyboard && cachedFootKeys != null)
                ProcessKeyGroup(cachedFootKeys, FootKeyBase, elapsedMilliseconds);
            if (Total != null && Total.value != null && lastTotal != d.TotalCount)
            {
                lastTotal = d.TotalCount;
                NumBuffer.Format(lastTotal, d.EnableCountFormatting, out var buf, out int off, out int len);
                Total.value.SetText(buf, off, len);
            }
        }

        /// <summary>
        /// Process a group of keys for input state changes / 处理一组按键的输入状态变化
        /// Local-caches Settings references for hot-path performance / 局部缓存 Settings 引用以优化热路径性能
        /// </summary>
        private void ProcessKeyGroup(KeyCode[] keyCodes, int baseIndex, long elapsedMs)
        {
            ProfileData d = Settings.Data;
            int[] countArr = d.Count;
            bool rainEnabled = d.EnableRainEffect;
            bool enablePressAnim = d.EnablePressAnimation;
            float pressAnimScale = d.PressAnimationScale;
            bool enablePerKeyKps = d.EnablePerKeyKps;
            bool enableCountFmt = d.EnableCountFormatting;
            for (int i = 0; i < keyCodes.Length; i++)
            {
                int idx = baseIndex + i;
                if (idx >= Keys.Length) continue;
                Key key = Keys[idx];
                if (key == null) continue;
                bool current = Input.GetKey(keyCodes[i]);
                if (current != key.isPressed)
                {
                    UpdateKeyColors(idx, current, d);
                    key.isPressed = current;
                    if (enablePressAnim)
                    {
                        float target = current ? pressAnimScale : 1f;
                        if (key.currentAnim != null)
                            StopCoroutine(key.currentAnim);
                        key.currentAnim = StartCoroutine(AnimateKeyScale(key, target, 0.08f));
                    }
                    if (current)
                    {
                        // d.Count and keyPressTimes are sized for MaxKeySlots (40). Full-keyboard keys (idx 0-104)
                        // have no per-key count text, so only the shared TotalCount accumulates for them. / 全键盘主键无每键计数，仅累加 TotalCount
                        if (idx < countArr.Length)
                        {
                            countArr[idx]++;
                            if (key.value != null && !enablePerKeyKps)
                            {
                                NumBuffer.Format(countArr[idx], enableCountFmt, out var buf, out int off, out int len);
                                key.value.SetText(buf, off, len);
                            }
                        }
                        d.TotalCount++;
                        PressTimes.Enqueue(elapsedMs);
                        if (keyPressTimes != null && idx < keyPressTimes.Length)
                        {
                            keyPressTimes[idx].Enqueue(elapsedMs);
                            _hasKeyPressActivity = true;
                        }
                        if (rainEnabled) rainSystem.TriggerRainEffect(idx, key);
                    }
                    else
                    {
                        if (rainEnabled) rainSystem.ReleaseRainEffect(idx, key);
                    }
                }
            }
        }

        /// <summary>
        /// Calculate KPS by removing presses older than 1 second / 通过移除超过 1 秒的按下记录计算 KPS
        /// </summary>
        private void ProcessKpsInUpdate(long elapsedMilliseconds)
        {
            if (PressTimes == null) return;
            while (PressTimes.Count > 0 && elapsedMilliseconds - PressTimes.Peek() > 1000)
                PressTimes.Dequeue();
            int currentKps = PressTimes.Count;
            if (lastKps != currentKps)
            {
                lastKps = currentKps;
                if (Kps != null && Kps.value != null)
                {
                    NumBuffer.Format(currentKps, Settings.Data.EnableCountFormatting, out var buf, out int off, out int len);
                    Kps.value.SetText(buf, off, len);
                }
            }
        }

        /// <summary>
        /// Per-key KPS: clean timestamps older than 1s and update display / 每键 KPS：清理超过 1 秒的时间戳并更新显示
        /// </summary>
        private void ProcessPerKeyKpsInUpdate(long elapsedMilliseconds)
        {
            if (!_hasKeyPressActivity) return;
            if (!Settings.Data.EnablePerKeyKps || keyPressTimes == null || Keys == null) return;
            for (int i = 0; i < Keys.Length && i < keyPressTimes.Length; i++)
            {
                var q = keyPressTimes[i];
                while (q.Count > 0 && elapsedMilliseconds - q.Peek() > 1000)
                    q.Dequeue();
                int kps = q.Count;
                if (lastPerKeyKps != null && i < lastPerKeyKps.Length && lastPerKeyKps[i] != kps)
                {
                    lastPerKeyKps[i] = kps;
                    if (Keys[i] != null && Keys[i].value != null)
                    {
                        NumBuffer.Format(kps, Settings.Data.EnableCountFormatting, out var buf, out int off, out int len);
                        Keys[i].value.SetText(buf, off, len);
                    }
                }
            }
            bool anyActive = false;
            foreach (var q in keyPressTimes)
                if (q.Count > 0) { anyActive = true; break; }
            _hasKeyPressActivity = anyActive;
        }

        /// <summary>
        /// Process ghost key inputs — secondary keys that only trigger rain, no display/count / 处理鬼键输入 — 仅触发雨滴的副按键，无显示/计数
        /// ghostKeyStates is guaranteed non-null and same length as cachedGhostKeys (initialized in ProcessMainAndFootKeysInUpdate before this runs) / ghostKeyStates 保证非空且长度与 cachedGhostKeys 相同（在此方法之前由 ProcessMainAndFootKeysInUpdate 初始化）
        /// </summary>
        private void ProcessGhostKeysInUpdate()
        {
            if (cachedGhostKeys == null) return;
            ProfileData d = Settings.Data;
            bool rainEnabled = d.EnableRainEffect;
            bool ghostRainEnabled = d.EnableGhostRain;
            if (!rainEnabled || !ghostRainEnabled) return;

            KeyCode[] ghosts = cachedGhostKeys;
            for (int i = 0; i < ghosts.Length; i++)
            {
                if (ghosts[i] == KeyCode.None) continue;

                bool current = Input.GetKey(ghosts[i]);
                if (current != ghostKeyStates[i])
                {
                    ghostKeyStates[i] = current;
                    if (current)
                        rainSystem.TriggerGhostRain(i, Keys[i]);
                    else
                        rainSystem.ReleaseGhostRain(i, Keys[i]);
                }
            }
        }

        /// <summary>
        /// Update key visual colors based on press state / 根据按下状态更新按键视觉颜色
        /// </summary>
        private void UpdateKeyColors(int i, bool pressed, ProfileData d = null)
        {
            if (IsFullKeyboard) {
                var d2 = Settings.Data;
                bool u = d2.EnableFullKeyboardUnifiedColor;
                Key k = Keys[i];
                k.background.color = pressed ? (u ? d2.FullKeyboardBackgroundClicked : d2.BackgroundClicked) : (u ? d2.FullKeyboardBackground : d2.Background);
                k.outline.color = pressed ? (u ? d2.FullKeyboardOutlineClicked : d2.OutlineClicked) : (u ? d2.FullKeyboardOutline : d2.Outline);
                k.text.color = pressed ? (u ? d2.FullKeyboardTextClicked : d2.TextClicked) : (u ? d2.FullKeyboardText : d2.Text);
                if (k.value != null) k.value.color = k.text.color;
                return;
            }
            if (Keys == null || i >= Keys.Length) return;
            Key key = Keys[i];
            if (key == null) return;
            if (d == null) d = Settings.Data;
            if (d.EnablePerKeyColors && i < MaxKeySlots)
            {
                key.background.color = pressed ? d.PerKeyBackgroundClicked[i] : d.PerKeyBackground[i];
                key.outline.color = pressed ? d.PerKeyOutlineClicked[i] : d.PerKeyOutline[i];
                key.text.color = pressed ? d.PerKeyTextClicked[i] : d.PerKeyText[i];
            }
            else
            {
                key.background.color = pressed ? d.BackgroundClicked : d.Background;
                key.outline.color = pressed ? d.OutlineClicked : d.Outline;
                key.text.color = pressed ? d.TextClicked : d.Text;
            }
            if (key.value != null) key.value.color = key.text.color;
        }

        /// <summary>
        /// Smoothly animate key visuals scale (center-pivot wrapper).
        /// When EnablePressAnimationOnRain is on, scales full key transform with pivot compensation instead.
        /// 平滑缩放按键：默认只缩放 Visuals 包裹层（雨滴不动），开启雨滴动画时缩放整个按键+位置补偿
        /// </summary>
        private IEnumerator AnimateKeyScale(Key key, float target, float duration)
        {
            bool affectRain = Settings.Data.EnablePressAnimationOnRain;
            Transform animTarget;
            Vector2 origPos = Vector2.zero;
            float width = 0f;
            if (affectRain)
            {
                // Scale full key including rain — pivot (0, 0.5) needs compensation / 缩放整个按键包含雨滴
                RectTransform rt = key.transform as RectTransform;
                animTarget = rt;
                origPos = rt.anchoredPosition;
                width = rt.sizeDelta.x;
            }
            else
            {
                // Scale visuals wrapper only — center-pivot, no compensation needed / 仅缩放视觉层
                animTarget = key.visuals;
            }
            float startS = animTarget.localScale.x;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (animTarget == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float p = Mathf.Min(1f, elapsed / duration);
                float s = Mathf.Lerp(startS, target, p);
                animTarget.localScale = new Vector3(s, s, 1);
                if (affectRain)
                    (animTarget as RectTransform).anchoredPosition = origPos + new Vector2(width * (startS - s) * 0.5f, 0);
                yield return null;
            }
            if (animTarget == null) yield break;
            animTarget.localScale = new Vector3(target, target, 1);
            if (affectRain)
                (animTarget as RectTransform).anchoredPosition = origPos + new Vector2(width * (startS - target) * 0.5f, 0);
        }
    }
}
