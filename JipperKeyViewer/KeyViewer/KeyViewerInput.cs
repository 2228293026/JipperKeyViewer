// Key detection and rebinding logic / 按键检测和重新绑定逻辑
// Handles listening for new key presses during rebinding and converting KeyCodes to display strings / 处理重绑定期间监听新按键，以及将 KeyCode 转换为显示字符串

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
            SaveSettings();
        }

        /// <summary>
        /// Convert a Unity KeyCode to a short display-friendly string / 将 Unity KeyCode 转换为简短友好的显示字符串
        /// Handles special keys: Alpha→number, Left/Right→L/R, Shift→⇧, Control→Ctrl, arrows, etc. / 处理特殊键：Alpha→数字，Left/Right→L/R，Shift→⇧，Control→Ctrl，方向键等
        /// </summary>
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

        /// <summary>Calculate IMGUI text field width based on content length / 根据内容长度计算 IMGUI 文本框宽度</summary>
        private static GUILayoutOption FloatFieldWidth(string text) => GUILayout.Width(Mathf.Max(30, text.Length * 9));
    }
}
