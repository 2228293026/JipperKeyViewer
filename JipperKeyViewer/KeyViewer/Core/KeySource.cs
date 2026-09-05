// Central key-state source with TGT (Spectre) replay compatibility. Unity's Input.GetKey
// reads the PHYSICAL keyboard, so key presses injected by replay mods are invisible to it —
// while the game's own replay pipeline reads the AsyncInput key masks (AsyncKeyCode /
// AsyncInputManager in Assembly-CSharp, fed from SkyHook), which reflect BOTH hardware and
// injected presses. When the AsyncInput hook is active we read those masks; otherwise we
// fall back to legacy Input polling. Every mod key poll funnels through this file.
// / 中心化按键状态源，兼容 TGT（Spectre）回放。Unity 的 Input.GetKey 读的是物理键盘——回放
// Mod 注入的按键对它不可见；而游戏自己的回放管线读取 AsyncInput 键掩码（Assembly-CSharp 里的
// AsyncKeyCode / AsyncInputManager，由 SkyHook 供数），硬件与注入按压都会反映其中。AsyncInput
// 钩子激活时读掩码，否则回落旧版 Input 轮询。本 Mod 的所有按键轮询统一走本文件。

using SkyHook;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    internal static class KeySource
    {
        /// <summary>AsyncInput hook active → replay/external key states are visible through
        /// the game's key masks. / AsyncInput 钩子激活 → 经由游戏键掩码可读到回放/外部按键。</summary>
        private static bool AsyncAvailable => AsyncInputManager.isActive;

        public static bool GetKey(KeyCode code)
        {
            if (AsyncAvailable)
            {
                KeyLabel? label = ToKeyLabel(code);
                if (label.HasValue)
                    return AsyncInput.GetKey(label.Value, false);
                return Input.GetKey(code); // unmapped key → legacy poll / 未映射键回落轮询
            }
            return Input.GetKey(code);
        }

        public static bool GetKeyDown(KeyCode code)
        {
            if (AsyncAvailable)
            {
                KeyLabel? label = ToKeyLabel(code);
                if (label.HasValue)
                    return AsyncInput.GetKeyDown(label.Value, false);
                return Input.GetKeyDown(code);
            }
            return Input.GetKeyDown(code);
        }

        /// <summary>Unity KeyCode → SkyHook KeyLabel (the exact mapping the game's replay
        /// pipeline consumes). Only keys a key-viewer can meaningfully display are mapped;
        /// null falls back to legacy polling at the call site. / Unity KeyCode → SkyHook
        /// KeyLabel（与游戏回放管线消费的映射一致）。仅映射按键显示器有意义的键；null 由调用
        /// 点回落旧版轮询。</summary>
        private static KeyLabel? ToKeyLabel(KeyCode code)
        {
            switch (code)
            {
                case KeyCode.Escape: return KeyLabel.Escape;
                case KeyCode.F1: return KeyLabel.F1;
                case KeyCode.F2: return KeyLabel.F2;
                case KeyCode.F3: return KeyLabel.F3;
                case KeyCode.F4: return KeyLabel.F4;
                case KeyCode.F5: return KeyLabel.F5;
                case KeyCode.F6: return KeyLabel.F6;
                case KeyCode.F7: return KeyLabel.F7;
                case KeyCode.F8: return KeyLabel.F8;
                case KeyCode.F9: return KeyLabel.F9;
                case KeyCode.F10: return KeyLabel.F10;
                case KeyCode.F11: return KeyLabel.F11;
                case KeyCode.F12: return KeyLabel.F12;
                case KeyCode.BackQuote: return KeyLabel.Grave;
                case KeyCode.Alpha1: return KeyLabel.Alpha1;
                case KeyCode.Alpha2: return KeyLabel.Alpha2;
                case KeyCode.Alpha3: return KeyLabel.Alpha3;
                case KeyCode.Alpha4: return KeyLabel.Alpha4;
                case KeyCode.Alpha5: return KeyLabel.Alpha5;
                case KeyCode.Alpha6: return KeyLabel.Alpha6;
                case KeyCode.Alpha7: return KeyLabel.Alpha7;
                case KeyCode.Alpha8: return KeyLabel.Alpha8;
                case KeyCode.Alpha9: return KeyLabel.Alpha9;
                case KeyCode.Alpha0: return KeyLabel.Alpha0;
                case KeyCode.Minus: return KeyLabel.Minus;
                case KeyCode.Equals: return KeyLabel.Equal;
                case KeyCode.Backspace: return KeyLabel.Backspace;
                case KeyCode.Tab: return KeyLabel.Tab;
                case KeyCode.Q: return KeyLabel.Q;
                case KeyCode.W: return KeyLabel.W;
                case KeyCode.E: return KeyLabel.E;
                case KeyCode.R: return KeyLabel.R;
                case KeyCode.T: return KeyLabel.T;
                case KeyCode.Y: return KeyLabel.Y;
                case KeyCode.U: return KeyLabel.U;
                case KeyCode.I: return KeyLabel.I;
                case KeyCode.O: return KeyLabel.O;
                case KeyCode.P: return KeyLabel.P;
                case KeyCode.LeftBracket: return KeyLabel.LeftBrace;
                case KeyCode.RightBracket: return KeyLabel.RightBrace;
                case KeyCode.Backslash: return KeyLabel.BackSlash;
                case KeyCode.CapsLock: return KeyLabel.CapsLock;
                case KeyCode.A: return KeyLabel.A;
                case KeyCode.S: return KeyLabel.S;
                case KeyCode.D: return KeyLabel.D;
                case KeyCode.F: return KeyLabel.F;
                case KeyCode.G: return KeyLabel.G;
                case KeyCode.H: return KeyLabel.H;
                case KeyCode.J: return KeyLabel.J;
                case KeyCode.K: return KeyLabel.K;
                case KeyCode.L: return KeyLabel.L;
                case KeyCode.Semicolon: return KeyLabel.Semicolon;
                case KeyCode.Quote: return KeyLabel.Apostrophe;
                case KeyCode.Return: return KeyLabel.Enter;
                case KeyCode.LeftShift: return KeyLabel.LShift;
                case KeyCode.RightShift: return KeyLabel.RShift;
                case KeyCode.Z: return KeyLabel.Z;
                case KeyCode.X: return KeyLabel.X;
                case KeyCode.C: return KeyLabel.C;
                case KeyCode.V: return KeyLabel.V;
                case KeyCode.B: return KeyLabel.B;
                case KeyCode.N: return KeyLabel.N;
                case KeyCode.M: return KeyLabel.M;
                case KeyCode.Comma: return KeyLabel.Comma;
                case KeyCode.Period: return KeyLabel.Dot;
                case KeyCode.Slash: return KeyLabel.Slash;
                case KeyCode.RightControl: return KeyLabel.RControl;
                case KeyCode.LeftControl: return KeyLabel.LControl;
                case KeyCode.LeftAlt: return KeyLabel.LAlt;
                case KeyCode.RightAlt: return KeyLabel.RAlt;
                case KeyCode.Space: return KeyLabel.Space;
                case KeyCode.UpArrow: return KeyLabel.ArrowUp;
                case KeyCode.DownArrow: return KeyLabel.ArrowDown;
                case KeyCode.LeftArrow: return KeyLabel.ArrowLeft;
                case KeyCode.RightArrow: return KeyLabel.ArrowRight;
                default: return null;
            }
        }
    }
}
