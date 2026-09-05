// Central key-state source. On Windows while the game window is focused, we poll the
// OS-level async key state (Win32 GetAsyncKeyState) instead of UnityEngine.Input: the OS
// state table reflects hardware AND injected presses (replay/macro mods drive user32-level
// input), which Unity's message-queue Input never reports — this is the same approach the
// community verified working against replay mods. Non-Windows or unfocused windows fall
// back to Unity Input. Every mod key poll funnels through this file.
// / 中心化按键状态源。Windows 且窗口聚焦时轮询操作系统级异步按键状态（Win32
// GetAsyncKeyState），而非 UnityEngine.Input：系统状态表同时反映硬件与注入按压（回放/
// 连点类 Mod 走 user32 层输入），而基于消息队列的 Unity Input 看不到它们——该方案已被
// 社区对回放 Mod 验证可行。非 Windows 或失焦时回落 Unity Input。所有按键轮询统一走本文件。

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    internal static class KeySource
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private static readonly bool IsWindows =
            Environment.OSVersion.Platform == PlatformID.Win32NT;

        public static bool GetKey(KeyCode code)
        {
            if (IsWindows && Application.isFocused)
            {
                int[] vks;
                if (VkTable.Value.TryGetValue(code, out vks))
                {
                    foreach (int vk in vks)
                        if ((GetAsyncKeyState(vk) & 0x8000) != 0)
                            return true;
                    return false;
                }
                // unmapped key → legacy poll / 未映射键回落轮询
            }
            return Input.GetKey(code);
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
