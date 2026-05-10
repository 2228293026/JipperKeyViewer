using System.Runtime.InteropServices;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    public partial class KeyViewer : MonoBehaviour
    {
        private void StartKeySelection()
        {
            WinAPICool = 0;
            KeyPressed = new bool[256];
            for (int i = 0; i < 256; i++)
            {
                KeyPressed[i] = (UnsafeNativeMethods.GetAsyncKeyState(i) & 0x8000) != 0;
            }
        }

        private void ProcessKeySelection()
        {
            if (SelectedKey == -1 || TextChanged || !Application.isFocused) return;
            if (Input.anyKeyDown)
            {
                foreach (KeyCode keyCode in AllKeyCodes)
                {
                    if (Input.GetKeyDown(keyCode))
                    {
                        SetupKey(keyCode);
                        return;
                    }
                }
            }
            else
            {
                for (int i = 0; i < 256; i++)
                {
                    bool currentPressed = (UnsafeNativeMethods.GetAsyncKeyState(i) & 0x8000) != 0;
                    if (currentPressed == KeyPressed[i]) continue;
                    if (KeyPressed[i])
                    {
                        KeyPressed[i] = false;
                        WinAPICool = 0;
                        continue;
                    }
                    else if (WinAPICool++ >= 6)
                    {
                        KeyCode keyCode = (KeyCode)(i + 0x1000);
                        SetupKey(keyCode);
                        return;
                    }
                }
            }
        }

        private void SetupKey(KeyCode keyCode)
        {
            KeyCode[] keyCodes = GetKeyCode();
            KeyCode[] footKeyCodes = GetFootKeyCode();
            string[] keyTexts = GetKeyText();
            if (SelectedKey < 20)
            {
                keyCodes[SelectedKey] = keyCode;
            }
            else
            {
                footKeyCodes[SelectedKey - 20] = keyCode;
            }
            if (Keys != null && SelectedKey < Keys.Length && Keys[SelectedKey] != null)
            {
                string displayText = SelectedKey < 20 && !string.IsNullOrEmpty(keyTexts[SelectedKey])
                    ? keyTexts[SelectedKey] : KeyToString(keyCode);
                Keys[SelectedKey].text.text = displayText;
            }
            SelectedKey = -1;
            WinAPICool = 0;
            KeyPressed = null;
            SaveSettings();
        }

        public static string KeyToString(KeyCode keyCode)
        {
            string keyString = keyCode.ToString();
            if (keyString.StartsWith("Alpha")) keyString = keyString.Substring(5);
            if (keyString.StartsWith("Keypad")) keyString = keyString.Substring(6);
            if (keyString.StartsWith("Left")) keyString = 'L' + keyString.Substring(4);
            if (keyString.StartsWith("Right")) keyString = 'R' + keyString.Substring(5);
            if (keyString.EndsWith("Shift")) keyString = keyString.Substring(0, keyString.Length - 5) + "\u21E7";
            if (keyString.EndsWith("Control")) keyString = keyString.Substring(0, keyString.Length - 7) + "Ctrl";
            if (keyString.StartsWith("Mouse")) keyString = "M" + keyString.Substring(5);
            return keyString switch
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
                "LeftArrow" => "\u2190",
                "RightArrow" => "\u2192",
                "Space" => "\u2423",
                "BackQuote" => "`",
                "PageDown" => "Pg\u2193",
                "PageUp" => "Pg\u2191",
                "CapsLock" => "\u21EA",
                "Insert" => "Ins",
                _ => keyString
            };
        }

        private static GUILayoutOption FloatFieldWidth(string text) => GUILayout.Width(Mathf.Max(30, text.Length * 9));
    }
}
