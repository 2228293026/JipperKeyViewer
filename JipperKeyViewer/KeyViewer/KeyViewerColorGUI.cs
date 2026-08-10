// Settings GUI: Colors tab content / 设置界面:颜色 标签页内容
// Normal color settings, per-key colors, full-keyboard unified colors, KPS/Total colors, and the shared color picker (RGBA + hex). / 普通配色、每键独立颜色、全键盘统一配色、KPS/Total 配色,以及通用颜色选择器(RGBA + Hex)

using System.Collections.Generic;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    public partial class KeyViewer : MonoBehaviour
    {
        private void DrawColorSettings()
        {
            GUILayout.BeginVertical("box");
            string[] colorNames = {
                I18n.Tr("color_bg"), I18n.Tr("color_bg_clicked"), I18n.Tr("color_outline"), I18n.Tr("color_outline_clicked"),
                I18n.Tr("color_text"), I18n.Tr("color_text_clicked"),
                I18n.Tr("color_rain1"), I18n.Tr("color_rain2"), I18n.Tr("color_rain3"),
                I18n.Tr("ghost_rain_color1"), I18n.Tr("ghost_rain_color2"), I18n.Tr("ghost_rain_color3")
            };
            Color[] defaultColors = {
                Background, BackgroundClicked, Outline, OutlineClicked,
                Text, TextClicked,
                RainColor, RainColor2, RainColor3,
                GhostRainColorDefault, GhostRainColor2Default, GhostRainColor3Default
            };
            for (int i = 0; i < 12; i++)
            {
                if (i >= 6 && i < 9 && !Settings.Data.EnableRainEffect)
                    continue;
                if (i >= 9 && !Settings.Data.EnableGhostRain)
                    continue;
                ColorExpanded[i] = DrawFoldoutButton(colorNames[i], ColorExpanded[i]);
                if (ColorExpanded[i])
                {
                    GUILayout.BeginVertical("box");
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(15);
                    Color currentColor = GetColorByIndex(i);
                    Color newColor = DrawColorPicker(colorNames[i], currentColor, defaultColors[i]);
                    if (newColor != currentColor)
                    {
                        SetColorByIndex(i, newColor);
                        UpdateAllKeyColors();
                        SaveSettings();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                }
            }
            GUILayout.Space(5);
            DrawKpsTotalColors(MaxKeySlots, I18n.Tr("kps_colors"), ref kpsColorType);
            GUILayout.Space(3);
            DrawKpsTotalColors(MaxKeySlots + 1, I18n.Tr("total_colors"), ref totalColorType);
            GUILayout.EndVertical();
        }

        // ===== KPS & Total independent color state / KPS 与 Total 独立配色状态 =====
        int kpsColorType = -1;
        int totalColorType = -1;

        private static Color KpsTotalColor(int pi, int t) => pi == MaxKeySlots
            ? t switch { 0 => Settings.Data.KpsBackground, 1 => Settings.Data.KpsOutline, _ => Settings.Data.KpsText }
            : t switch { 0 => Settings.Data.TotalBackground, 1 => Settings.Data.TotalOutline, _ => Settings.Data.TotalText };

        private static void SetKpsTotalColor(int pi, int t, Color c)
        {
            if (pi == MaxKeySlots)
            {
                if (t == 0) Settings.Data.KpsBackground = c;
                else if (t == 1) Settings.Data.KpsOutline = c;
                else Settings.Data.KpsText = c;
            }
            else
            {
                if (t == 0) Settings.Data.TotalBackground = c;
                else if (t == 1) Settings.Data.TotalOutline = c;
                else Settings.Data.TotalText = c;
            }
        }

        private void DrawKpsTotalColors(int pi, string label, ref int expandedType)
        {
            expandedType = DrawFoldoutButton(label, expandedType);
            if (expandedType < 0) return;

            string[] typeNames = {
                I18n.Tr("color_bg"), I18n.Tr("color_outline"), I18n.Tr("color_text")
            };
            Color[] defaults = { Background, Outline, Text };

            for (int t = 0; t < 3; t++)
            {
                DrawFoldoutItemButton(typeNames[t], ref expandedType, t);
                if (expandedType != t) continue;

                GUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                Color cur = KpsTotalColor(pi, t);
                Color newColor = DrawColorPicker(typeNames[t], cur, defaults[t]);
                if (newColor != cur)
                {
                    SetKpsTotalColor(pi, t, newColor);
                    UpdateAllKeyColors();
                    SaveSettings();
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }

        // Per-frame counter naming the color picker's text fields (reset in DrawSettingsWindow) so focus
        // can be tracked per-field via control names on every IMGUI version. / 每帧自增的字段命名计数器
        // (在 DrawSettingsWindow 开头重置),用于跨版本稳定跟踪颜色输入框的焦点。
        private static int colorPickerFieldSeq;

        // In-progress text for the color picker text fields, keyed by control name. Persists across
        // frames so a focused field isn't reset to the model value while its value is being typed.
        // / 颜色输入框的进行中输入(按控件名缓存),跨帧保留,避免输入过程中被模型值重置。
        private readonly Dictionary<string, string> colorInputBuffer = new Dictionary<string, string>();

        private string TextInputField(string ctrlName, string modelText, params GUILayoutOption[] options)
        {
            GUI.SetNextControlName(ctrlName);
            bool focused = GUI.GetNameOfFocusedControl() == ctrlName;
            string display = focused && colorInputBuffer.TryGetValue(ctrlName, out string pending)
                ? pending : modelText;
            string txt = GUILayout.TextField(display, options);
            if (focused) colorInputBuffer[ctrlName] = txt;
            else colorInputBuffer.Remove(ctrlName);
            return txt;
        }

        /// <summary>Color to #RRGGBB, or #RRGGBBAA when alpha &lt; 255 / 颜色转 #RRGGBB,非不透明时输出 #RRGGBBAA</summary>
        private static string ColorToHex(Color c)
        {
            int r = Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f);
            int g = Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f);
            int b = Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f);
            int a = Mathf.RoundToInt(Mathf.Clamp01(c.a) * 255f);
            return a < 255 ? $"#{r:X2}{g:X2}{b:X2}{a:X2}" : $"#{r:X2}{g:X2}{b:X2}";
        }

        /// <summary>Parse #RRGGBB or #RRGGBBAA; 6-digit input sets alpha to opaque / 解析 #RRGGBB 或 #RRGGBBAA;6 位输入时 alpha 设为不透明</summary>
        private static bool TryParseHex(string input, out Color color)
        {
            color = Color.white;
            string s = string.IsNullOrEmpty(input) ? string.Empty : input.Trim().TrimStart('#');
            if (s.Length != 6 && s.Length != 8) return false;
            if (!uint.TryParse(s, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out uint v))
                return false;
            if (s.Length == 6)
                color = new Color(((v >> 16) & 0xFF) / 255f, ((v >> 8) & 0xFF) / 255f, (v & 0xFF) / 255f, 1f);
            else
                color = new Color(((v >> 24) & 0xFF) / 255f, ((v >> 16) & 0xFF) / 255f, ((v >> 8) & 0xFF) / 255f, (v & 0xFF) / 255f);
            return true;
        }

        private Color DrawColorPicker(string label, Color currentColor, Color defaultColor)
        {
            GUILayout.BeginVertical();
            GUILayout.Label(label);

            // Unique control names allocated up-front in draw order so focus tracking stays stable.
            // 先按绘制顺序分配唯一的控件名,保证焦点跟踪一致。
            string ctrlR = "cpi_" + (++colorPickerFieldSeq);
            string ctrlG = "cpi_" + (++colorPickerFieldSeq);
            string ctrlB = "cpi_" + (++colorPickerFieldSeq);
            string ctrlA = "cpi_" + (++colorPickerFieldSeq);
            string ctrlHex = "cpi_" + (++colorPickerFieldSeq);

            void DrawChannel(string ctrl, string name, ref float channel)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(name + ":", GUILayout.Width(20));
                channel = GUILayout.HorizontalSlider(channel, 0f, 1f, GUILayout.Width(150));
                // Text input keeps Unity's 0-1 scale; use the Hex field below for precise values.
                // 文本框保持 0-1;要精确取色时用下面的 Hex 输入。
                string txt = TextInputField(ctrl, channel.ToString("F2"), GUILayout.Width(40));
                if (float.TryParse(txt, out float val))
                    channel = Mathf.Clamp01(val);
                GUILayout.EndHorizontal();
            }

            DrawChannel(ctrlR, "R", ref currentColor.r);
            DrawChannel(ctrlG, "G", ref currentColor.g);
            DrawChannel(ctrlB, "B", ref currentColor.b);
            DrawChannel(ctrlA, "A", ref currentColor.a);

            // Direct #RRGGBB / #RRGGBBAA hex entry. / 直接输入 #RRGGBB 或 #RRGGBBAA
            GUILayout.BeginHorizontal();
            GUILayout.Label("Hex:", GUILayout.Width(20));
            string hex = TextInputField(ctrlHex, ColorToHex(currentColor), GUILayout.Width(120));
            if (TryParseHex(hex, out Color parsed))
                currentColor = parsed;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(I18n.Tr("preview") + ":", GUILayout.Width(40));
            Rect previewRect = GUILayoutUtility.GetRect(100, 20);
            GUIUtils.DrawRect(previewRect, currentColor);
            GUILayout.EndHorizontal();
            if (GUILayout.Button(I18n.Tr("reset_default")))
            {
                currentColor = defaultColor;
            }
            GUILayout.EndVertical();
            return currentColor;
        }

        private int perKeyColorSelected = -1;
        private int perKeyColorTypeIndex = -1;

        private void DrawPerKeyColorSettings()
        {
            GUILayout.BeginVertical("box");
            KeyCode[] keyCodes = GetKeyCode();
            KeyCode[] footKeyCodes = GetFootKeyCode();

            GUILayout.Label(I18n.Tr("row1_keys") + ":");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 8; i++) DrawPerKeyColorBtn(i, KeyToString(keyCodes[i]));
            GUILayout.EndHorizontal();

            byte[] backSequence = GetBackSequence();
            if (backSequence.Length > 0)
            {
                GUILayout.Label(I18n.Tr("row2_keys") + ":");
                GUILayout.BeginHorizontal();
                for (int b = 0; b < backSequence.Length && b < 8; b++)
                    DrawPerKeyColorBtn(backSequence[b], KeyToString(keyCodes[backSequence[b]]));
                GUILayout.EndHorizontal();
            }

            if (backSequence.Length >= 8)
            {
                GUILayout.Label(I18n.Tr("row3_keys") + ":");
                GUILayout.BeginHorizontal();
                for (int b = 8; b < backSequence.Length && backSequence[b] < keyCodes.Length; b++)
                    DrawPerKeyColorBtn(backSequence[b], KeyToString(keyCodes[backSequence[b]]));
                GUILayout.EndHorizontal();
            }

            if (footKeyCodes != null && footKeyCodes.Length > 0)
            {
                GUILayout.Label(I18n.Tr("foot_keys") + ":");
                int rows = footKeyCodes.Length <= 8 ? 1 : 2;
                for (int r = 0; r < rows; r++)
                {
                    GUILayout.BeginHorizontal();
                    int start = r * 8;
                    int end = Mathf.Min(start + 8, footKeyCodes.Length);
                    for (int f = start; f < end; f++)
                        DrawPerKeyColorBtn(FootKeyBase + f, KeyToString(footKeyCodes[f]));
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            DrawPerKeyColorBtn(MaxKeySlots, "KPS");
            DrawPerKeyColorBtn(MaxKeySlots + 1, "Total");
            GUILayout.EndHorizontal();

            if (perKeyColorSelected >= 0 && perKeyColorSelected < MaxKeySlots + 2)
                DrawPerKeyColorEditor(perKeyColorSelected);

            if (GUILayout.Button(I18n.Tr("per_key_color_reset")))
            { Settings.Data.InitPerKeyColors(); UpdateAllKeyColors(); SaveSettings(); }
            if (GUILayout.Button(I18n.Tr("auto_rainbow")))
                AutoAssignRainbowColors();

            GUILayout.EndVertical();
        }

        private void DrawPerKeyColorBtn(int idx, string label)
        {
            Color c = Settings.Data.PerKeyBackground[idx];
            var style = new GUIStyle(GUI.skin.button);
            style.normal.textColor = c.grayscale > 0.5f ? Color.black : Color.white;
            if (perKeyColorSelected == idx)
                GUI.backgroundColor = Color.Lerp(c, Color.white, 0.4f);
            else
                GUI.backgroundColor = c;
            bool pressed = GUILayout.Button(label, style);
            GUI.backgroundColor = Color.white;
            if (pressed)
            {
                if (perKeyColorSelected != idx) perKeyColorTypeIndex = -1;
                perKeyColorSelected = perKeyColorSelected == idx ? -1 : idx;
            }
        }

        private static string PerKeyLabel(int s) => s switch
        {
            MaxKeySlots => "KPS",
            MaxKeySlots + 1 => "Total",
            _ => KeyToString(GetKeyCodeForIndex(s))
        };

        private static int[] PerKeyTypeOrder(int s) => s >= MaxKeySlots
            ? new int[] { 0, 2, 4 }
            : s >= FootKeyBase ? new int[] { 0, 1, 2, 3, 4, 5 }
            : new int[] { 0, 1, 2, 3, 4, 5, 6, 7 };

        private bool DrawColorFoldout(int t, string name)
        {
            DrawFoldoutItemButton(name, ref perKeyColorTypeIndex, t);
            return perKeyColorTypeIndex == t;
        }

        private void DrawPerKeyColorEditor(int s)
        {
            GUILayout.Space(5);
            GUILayout.Label("Key " + s + " (" + PerKeyLabel(s) + ")");
            // Foot keys skip rain color types (6,7); PerKeyTypeOrder excludes them
            // 脚键不提供雨滴配色(6,7);PerKeyTypeOrder 已排除
            string rainKey = s < 8 ? "color_rain1" : s < 16 ? "color_rain2" : s < FootKeyBase ? "color_rain3" : "";

            string[] typeNames = {
                I18n.Tr("color_bg"), I18n.Tr("color_bg_clicked"),
                I18n.Tr("color_outline"), I18n.Tr("color_outline_clicked"),
                I18n.Tr("color_text"), I18n.Tr("color_text_clicked"),
                I18n.Tr(rainKey), "Ghost " + I18n.Tr(rainKey)
            };
            Color[] values = {
                Settings.Data.PerKeyBackground[s], Settings.Data.PerKeyBackgroundClicked[s],
                Settings.Data.PerKeyOutline[s], Settings.Data.PerKeyOutlineClicked[s],
                Settings.Data.PerKeyText[s], Settings.Data.PerKeyTextClicked[s],
                Settings.Data.PerKeyRainColor[s], Settings.Data.PerKeyGhostRainColor[s]
            };
            Color[] defaults = {
                Background, BackgroundClicked, Outline, OutlineClicked, Text, TextClicked,
                RainColor, GhostRainColorDefault
            };

            int[] typeOrder = PerKeyTypeOrder(s);
            for (int ti = 0; ti < typeOrder.Length; ti++)
            {
                int t = typeOrder[ti];
                if (!DrawColorFoldout(t, typeNames[t])) continue;

                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                GUILayout.BeginVertical("box");
                Color newColor = DrawColorPicker(typeNames[t], values[t], defaults[t]);
                if (newColor != values[t])
                {
                    SetPerKeyColor(s, t, newColor);
                    UpdateAllKeyColors();
                    SaveSettings();
                }
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }

            if (s < MaxKeySlots && Settings.Data.Count != null && s < Settings.Data.Count.Length)
                DrawPerKeyCountReset(s);
        }

        private static void SetPerKeyColor(int s, int t, Color color)
        {
            switch (t)
            {
                case 0: Settings.Data.PerKeyBackground[s] = color; break;
                case 1: Settings.Data.PerKeyBackgroundClicked[s] = color; break;
                case 2: Settings.Data.PerKeyOutline[s] = color; break;
                case 3: Settings.Data.PerKeyOutlineClicked[s] = color; break;
                case 4: Settings.Data.PerKeyText[s] = color; break;
                case 5: Settings.Data.PerKeyTextClicked[s] = color; break;
                case 6: Settings.Data.PerKeyRainColor[s] = color; break;
                case 7: Settings.Data.PerKeyGhostRainColor[s] = color; break;
            }
        }

        private void DrawPerKeyCountReset(int s)
        {
            GUILayout.Space(5);
            var redStyle = new GUIStyle(GUI.skin.button) { normal = { textColor = Color.red } };
            if (GUILayout.Button(I18n.Tr("reset_counts") + " (" + Settings.Data.Count[s] + ")", redStyle))
            {
                Settings.Data.Count[s] = 0;
                if (keyPressTimes != null && s < keyPressTimes.Length && keyPressTimes[s] != null)
                    keyPressTimes[s].Clear();
                if (lastPerKeyKps != null && s < lastPerKeyKps.Length)
                    lastPerKeyKps[s] = 0;
                if (Keys != null && s < Keys.Length && Keys[s]?.value != null)
                    Keys[s].value.text = "0";
                SaveSettings();
            }
        }

        private static KeyCode GetKeyCodeForIndex(int idx)
        {
            KeyCode[] main = GetKeyCode();
            if (main != null && idx < main.Length) return main[idx];
            KeyCode[] foot = GetFootKeyCode();
            int fi = idx - FootKeyBase;
            if (foot != null && fi >= 0 && fi < foot.Length) return foot[fi];
            return KeyCode.None;
        }

        private void DrawColorSection()
        {
            bool colorsExpanded = DrawFoldoutButton(I18n.Tr("colors"), ColorExpanded != null);
            if (colorsExpanded && ColorExpanded == null) ColorExpanded = new bool[12];
            if (!colorsExpanded) ColorExpanded = null;
            if (ColorExpanded == null) return;

            if (KeyViewer.IsFullKeyboard)
            {
                DrawFullKeyboardColorSection();
                return;
            }
            bool pk = GUILayout.Toggle(Settings.Data.EnablePerKeyColors, I18n.Tr("per_key_colors"));
            if (pk != Settings.Data.EnablePerKeyColors)
            {
                Settings.Data.EnablePerKeyColors = pk;
                ResetKeyViewer();
                UpdateAllKeyColors();
                SaveSettings();
            }
            if (Settings.Data.EnablePerKeyColors)
                DrawPerKeyColorSettings();
            else
                DrawColorSettings();
        }

        // ======================== Full Keyboard (108K) color section ========================
        private void DrawFullKeyboardColorSection()
        {
            GUILayout.BeginVertical("box");

            bool unified = GUILayout.Toggle(Settings.Data.EnableFullKeyboardUnifiedColor, I18n.Tr("fk_unified_color"));
            if (unified != Settings.Data.EnableFullKeyboardUnifiedColor)
            {
                Settings.Data.EnableFullKeyboardUnifiedColor = unified;
                UpdateAllKeyColors();
                SaveSettings();
            }

            string[] names = {
                I18n.Tr("color_bg"), I18n.Tr("color_bg_clicked"), I18n.Tr("color_outline"),
                I18n.Tr("color_outline_clicked"), I18n.Tr("color_text"), I18n.Tr("color_text_clicked")
            };
            Color[] defaults = {
                Background, BackgroundClicked, Outline, OutlineClicked, Text, TextClicked
            };
            for (int i = 0; i < 6; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                Color cur = GetFullKeyboardColor(i);
                Color newColor = DrawColorPicker(names[i], cur, defaults[i]);
                if (newColor != cur)
                {
                    SetFullKeyboardColor(i, newColor);
                    UpdateAllKeyColors();
                    SaveSettings();
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5);
            if (Settings.Data.FullKeyboardShowKpsTotal)
            {
                // KPS/Total colors only apply when unified color is OFF; with unified ON they follow
                // the FullKeyboard color set above, so hide these controls to avoid dead settings.
                // 仅当统一色关闭时 KPS/Total 单独配色才生效;开启时它们跟随上面的 FullKeyboard 配色,故隐藏入口避免无效设置。
                if (!Settings.Data.EnableFullKeyboardUnifiedColor)
                {
                    DrawKpsTotalColors(MaxKeySlots, I18n.Tr("kps_colors"), ref kpsColorType);
                    GUILayout.Space(3);
                    DrawKpsTotalColors(MaxKeySlots + 1, I18n.Tr("total_colors"), ref totalColorType);
                }
            }

            GUILayout.EndVertical();
        }

        private static Color GetFullKeyboardColor(int dim) => dim switch
        {
            0 => Settings.Data.FullKeyboardBackground,
            1 => Settings.Data.FullKeyboardBackgroundClicked,
            2 => Settings.Data.FullKeyboardOutline,
            3 => Settings.Data.FullKeyboardOutlineClicked,
            4 => Settings.Data.FullKeyboardText,
            _ => Settings.Data.FullKeyboardTextClicked
        };

        private static void SetFullKeyboardColor(int dim, Color c)
        {
            switch (dim)
            {
                case 0: Settings.Data.FullKeyboardBackground = c; break;
                case 1: Settings.Data.FullKeyboardBackgroundClicked = c; break;
                case 2: Settings.Data.FullKeyboardOutline = c; break;
                case 3: Settings.Data.FullKeyboardOutlineClicked = c; break;
                case 4: Settings.Data.FullKeyboardText = c; break;
                default: Settings.Data.FullKeyboardTextClicked = c; break;
            }
        }
    }
}