// Key detection and rebinding logic / 按键检测和重新绑定逻辑
// Handles listening for new key presses during rebinding and converting KeyCodes to display strings / 处理重绑定期间监听新按键，以及将 KeyCode 转换为显示字符串

using System.Collections.Generic;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
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
            if (SelectedKey == -1 || TextChanged || !Application.isFocused) return;
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
            KeyCode[] keyCodes = GetKeyCode();
            KeyCode[] footKeyCodes = GetFootKeyCode();
            string[] keyTexts = GetKeyText();
            if (SelectedKey < 20)
            {
                keyCodes[SelectedKey] = keyCode;
            }
            else if (footKeyCodes != null && SelectedKey - 20 < footKeyCodes.Length)
            {
                footKeyCodes[SelectedKey - 20] = keyCode;
            }
            else
            {
                SelectedKey = -1;
                return;
            }
            if (Keys != null && SelectedKey < Keys.Length && Keys[SelectedKey] != null)
            {
                string displayText;
                if (SelectedKey < 20 && !string.IsNullOrEmpty(keyTexts[SelectedKey]))
                    displayText = keyTexts[SelectedKey];
                else if (SelectedKey >= 20)
                {
                    string[] footTexts = GetFootKeyText();
                    int footIndex = SelectedKey - 20;
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
                string s = k.ToString();
                if (s.StartsWith("Alpha")) s = s.Substring(5);
                else if (s.StartsWith("Keypad")) s = s.Substring(6);
                else if (s.StartsWith("Left")) s = 'L' + s.Substring(4);
                else if (s.StartsWith("Right")) s = 'R' + s.Substring(5);
                else if (s.StartsWith("Mouse")) s = "M" + s.Substring(5);
                if (s.EndsWith("Shift")) s = s.Substring(0, s.Length - 5) + "\u21E7";
                else if (s.EndsWith("Control")) s = s.Substring(0, s.Length - 7) + "Ctrl";
                s = s switch
                {
                    "Plus" => "+", "Minus" => "-", "Multiply" => "*", "Divide" => "/",
                    "Enter" => "\u21B5", "Equals" => "=", "Period" => ".", "Return" => "\u21B5",
                    "None" => " ", "Tab" => "\u21E5", "Backslash" => "\\", "Backspace" => "Back",
                    "Slash" => "/", "LBracket" => "[", "RBracket" => "]", "Semicolon" => ";",
                    "Comma" => ",", "Quote" => "'", "UpArrow" => "\u2191", "DownArrow" => "\u2193",
                    "LeftArrow" => "\u2190", "RightArrow" => "\u2192", "Space" => "\u2423",
                    "BackQuote" => "`", "PageDown" => "Pg\u2193", "PageUp" => "Pg\u2191",
                    "CapsLock" => "\u21EA", "Insert" => "Ins",
                    _ => s
                };
                KeyDisplayNames[k] = s;
            }
        }

        /// <summary>Calculate IMGUI text field width based on content length / 根据内容长度计算 IMGUI 文本框宽度</summary>
        private static GUILayoutOption FloatFieldWidth(string text) => GUILayout.Width(Mathf.Max(30, text.Length * 9));
    }
}
