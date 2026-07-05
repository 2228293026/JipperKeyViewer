// Settings GUI window drawn inside UnityModManager / 在 UnityModManager 内绘制的设置 GUI 窗口
// All user-facing configuration UI: language, fonts, position, layout, colors, key rebinding, text editing / 所有面向用户的配置 UI：语言、字体、位置、布局、颜色、按键重绑定、文本编辑

using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>
    /// Settings window rendered via UnityModManager.OnGUI / 通过 UnityModManager.OnGUI 渲染的设置窗口
    /// Uses IMGUI (GUILayout) for immediate-mode UI / 使用 IMGUI (GUILayout) 即时模式 UI
    /// </summary>
    public partial class KeyViewer : MonoBehaviour
    {
        // ===== Profile UI state / 配置选择器 UI 状态 =====
        bool profileExpanded;
        bool profileIsRenaming;
        string profileRenameBuffer = "";
        string profileSaveAsBuffer = "";

        private static float FloatSliderField(GUIContent label, float value, float min, float max, string format = "F2")
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(100));
            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(120));
            string text = GUILayout.TextField(value.ToString(format), FloatFieldWidth(value.ToString(format)));
            if (float.TryParse(text, out float parsed))
                value = Mathf.Clamp(parsed, min, float.MaxValue);
            GUILayout.EndHorizontal();
            return value;
        }

        private static float FloatSliderField(string label, float value, float min, float max, string format = "F2")
            => FloatSliderField(new GUIContent(label), value, min, max, format);

        private static bool DrawFoldoutButton(string label, bool expanded)
        {
            if (GUILayout.Button((expanded ? "◢ " : "▶ ") + label, GUI.skin.label))
                return !expanded;
            return expanded;
        }

        private static int DrawFoldoutButton(string label, int expandedType, int expandValue = 0)
        {
            if (GUILayout.Button((expandedType >= 0 ? "◢ " : "▶ ") + label, GUI.skin.label))
                return expandedType >= 0 ? -1 : expandValue;
            return expandedType;
        }

        private static void DrawFoldoutItemButton(string label, ref int state, int itemIndex)
        {
            if (GUILayout.Button((state == itemIndex ? "◢ " : "▶ ") + label, GUI.skin.label))
                state = state == itemIndex ? -1 : itemIndex;
        }

        /// <summary>
        /// Draw the main settings window / 绘制主设置窗口
        /// Contains: language toggle, enable/disable, font selection, placement, custom positioning, layout, size, rain, key change, text change, colors / 包含：语言切换、启用/禁用、字体选择、位置、自定义定位、布局、大小、雨滴、按键更改、文本更改、颜色
        /// </summary>
        public void DrawSettingsWindow()
        {
            GUILayout.BeginVertical();
            DrawProfileSection();
            DrawLanguageSection();
            DrawCountResetSection();
            DrawFontSection();
            DrawFolderButtons();
            GUILayout.Space(10);
            DrawCustomPositionSection();
            DrawLayoutSection();
            DrawDisplaySection();
            GUILayout.Space(10);
            DrawRainSection();
            GUILayout.Space(10);
            DrawBindingSection();
            GUILayout.Space(5);
            DrawColorSection();
            GUILayout.EndVertical();
        }

        private void DrawProfileSection()
        {
            GUILayout.BeginVertical("box");
            DrawProfileFoldout();
            if (profileExpanded)
            {
                SyncProfilesWithDisk();
                DrawProfileList();
                GUILayout.Space(3);
                GUILayout.BeginHorizontal();
                DrawProfileSaveAs();
                DrawProfileRenameButton();
                DrawProfileDeleteButton();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawProfileFoldout()
        {
            string label = I18n.Tr("profile") + ": " + Settings.CurrentProfile;
            if (GUILayout.Button((profileExpanded ? "\u25BC " : "\u25B6 ") + label, GUILayout.MinWidth(200)))
                profileExpanded = !profileExpanded;
        }

        private void DrawProfileList()
        {
            if (Settings.ProfileNames == null) return;
            for (int i = 0; i < Settings.ProfileNames.Length; i++)
            {
                string p = Settings.ProfileNames[i];
                bool selected = p == Settings.CurrentProfile;
                if (GUILayout.Button((selected ? "\u2713 " : "  ") + p, GUILayout.MinWidth(200)))
                {
                    if (!selected) SwitchProfile(p);
                    profileExpanded = false;
                }
            }
        }

        private void DrawProfileSaveAs()
        {
            profileSaveAsBuffer = GUILayout.TextField(profileSaveAsBuffer, GUILayout.Width(120));
            if (GUILayout.Button(I18n.Tr("save_as"), GUILayout.MinWidth(60)))
            {
                string name = SanitizeFileName(profileSaveAsBuffer.Trim());
                if (string.IsNullOrEmpty(name)) return;
                if (Settings.ProfileNames != null)
                    foreach (var p in Settings.ProfileNames)
                        if (SanitizeFileName(p) == name) return;
                var list = new List<string>(Settings.ProfileNames ?? new string[0]) { name };
                Settings.ProfileNames = list.ToArray();
                Settings.CurrentProfile = name;
                SaveCurrentProfile();
                SaveMetaOnly();
                profileSaveAsBuffer = "";
                profileExpanded = false;
            }
        }

        private void DrawProfileRenameButton()
        {
            if (!profileIsRenaming)
            {
                if (GUILayout.Button(I18n.Tr("rename"), GUILayout.MinWidth(60)))
                {
                    profileIsRenaming = true;
                    profileRenameBuffer = Settings.CurrentProfile;
                }
                return;
            }
            profileRenameBuffer = GUILayout.TextField(profileRenameBuffer, GUILayout.Width(100));
            if (GUILayout.Button("\u2713", GUILayout.Width(24)))
            {
                string newName = SanitizeFileName(profileRenameBuffer.Trim());
                if (!string.IsNullOrEmpty(newName) && newName != SanitizeFileName(Settings.CurrentProfile))
                {
                    bool dup = Settings.ProfileNames != null
                        && Settings.ProfileNames.Any(p => SanitizeFileName(p) == newName);
                    if (!dup)
                        RenameProfile(Settings.CurrentProfile, newName);
                }
                profileIsRenaming = false;
                profileExpanded = false;
            }
            if (GUILayout.Button("\u2717", GUILayout.Width(24)))
                profileIsRenaming = false;
        }

        private void DrawProfileDeleteButton()
        {
            bool canDelete = Settings.ProfileNames != null && Settings.ProfileNames.Length > 1;
            GUI.enabled = canDelete;
            if (GUILayout.Button(I18n.Tr("delete"), GUILayout.MinWidth(60)))
            {
                DeleteProfile(Settings.CurrentProfile);
                profileExpanded = false;
            }
            GUI.enabled = true;
        }

        /// <summary>
        /// Draw the key rebinding section / 绘制按键重绑定区域
        /// Shows all keys for the current layout as clickable buttons / 将当前布局的所有按键显示为可点击的按钮
        /// </summary>
        private void DrawKeyChangeSection()
        {
            GUILayout.BeginVertical("box");
            KeyCode[] keyCodes = GetKeyCode();
            DrawMainKeyRows(I18n.Tr("row1_keys"), I18n.Tr("row2_keys"), I18n.Tr("row3_keys"),
                keyCodes, (i, _) => { SelectedKey = i; changeState = 0; });
            DrawFootKeyRows(I18n.Tr("foot_keys_list"), 20,
                (i, _) => { SelectedKey = i; changeState = 0; });
            if (SelectedKey != -1 && changeState == 0)
                GUILayout.Label("<b>" + I18n.Tr("press_new_key") + "</b>");
            GUILayout.EndVertical();
        }

        private void DrawMainKeyRows(string row1Label, string row2Label, string row3Label,
            KeyCode[] keyCodes, System.Action<int, KeyCode> onKeyClick, System.Func<int, KeyCode, string> labelFunc = null)
        {
            labelFunc ??= (i, kc) => KeyToString(kc);
            GUILayout.Label(row1Label + ":");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 8; i++)
                if (GUILayout.Button(labelFunc(i, keyCodes[i])))
                    onKeyClick(i, keyCodes[i]);
            GUILayout.EndHorizontal();

            byte[] backSequence = GetBackSequence();
            if (backSequence.Length > 0)
            {
                GUILayout.Label(row2Label + ":");
                GUILayout.BeginHorizontal();
                for (int i = 0; i < backSequence.Length && i < 8; i++)
                    if (GUILayout.Button(labelFunc(backSequence[i], keyCodes[backSequence[i]])))
                        onKeyClick(backSequence[i], keyCodes[backSequence[i]]);
                GUILayout.EndHorizontal();
            }

            if (backSequence.Length > 8)
            {
                GUILayout.Label(row3Label + ":");
                GUILayout.BeginHorizontal();
                for (int i = 8; i < backSequence.Length && backSequence[i] < keyCodes.Length; i++)
                    if (GUILayout.Button(labelFunc(backSequence[i], keyCodes[backSequence[i]])))
                        onKeyClick(backSequence[i], keyCodes[backSequence[i]]);
                GUILayout.EndHorizontal();
            }
        }

        private void DrawFootKeyRows(string label, int baseIndex, System.Action<int, KeyCode> onKeyClick,
            System.Func<int, KeyCode, string> labelFunc = null)
        {
            labelFunc ??= (i, kc) => KeyToString(kc);
            KeyCode[] footKeyCodes = GetFootKeyCode();
            if (footKeyCodes == null || footKeyCodes.Length == 0) return;
            GUILayout.Label(label + ":");
            if (footKeyCodes.Length <= 8)
            {
                GUILayout.BeginHorizontal();
                for (int i = 0; i < footKeyCodes.Length; i++)
                    if (GUILayout.Button(labelFunc(baseIndex + i, footKeyCodes[i])))
                        onKeyClick(baseIndex + i, footKeyCodes[i]);
                GUILayout.EndHorizontal();
            }
            else
            {
                int remaining = footKeyCodes.Length - 8;
                GUILayout.BeginHorizontal();
                for (int i = 0; i < 8; i++)
                    if (GUILayout.Button(labelFunc(baseIndex + i, footKeyCodes[i])))
                        onKeyClick(baseIndex + i, footKeyCodes[i]);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                for (int s = 0; s < 8 - remaining; s++)
                    GUILayout.FlexibleSpace();
                for (int i = 8; i < footKeyCodes.Length; i++)
                    if (GUILayout.Button(labelFunc(baseIndex + i, footKeyCodes[i])))
                        onKeyClick(baseIndex + i, footKeyCodes[i]);
                for (int s = 0; s < 8 - remaining; s++)
                    GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// Draw the ghost key rebinding section / 绘制鬼键重绑定区域
        /// Shows ghost key slots — click unbound to bind, click bound to clear / 显示鬼键槽位 — 点击未绑定的进入绑定，点击已绑定的清除
        /// </summary>
        private void DrawGhostKeyChangeSection()
        {
            GUILayout.BeginVertical("box");
            KeyCode[] ghostKeyCodes = GetGhostKeyCode();

            GUILayout.Label(I18n.Tr("row1_keys") + ":");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 8; i++)
                DrawGhostKeyButton(i, ghostKeyCodes);
            GUILayout.EndHorizontal();

            byte[] backSequence = GetBackSequence();
            if (backSequence.Length > 0)
            {
                GUILayout.Label(I18n.Tr("row2_keys") + ":");
                GUILayout.BeginHorizontal();
                for (int i = 0; i < backSequence.Length && i < 8; i++)
                    DrawGhostKeyButton(backSequence[i], ghostKeyCodes);
                GUILayout.EndHorizontal();
            }

            if (backSequence.Length > 8)
            {
                GUILayout.Label(I18n.Tr("row3_keys") + ":");
                GUILayout.BeginHorizontal();
                for (int i = 8; i < backSequence.Length; i++)
                    DrawGhostKeyButton(backSequence[i], ghostKeyCodes);
                GUILayout.EndHorizontal();
            }

            if (SelectedKey != -1 && changeState == 2)
                GUILayout.Label("<b>" + I18n.Tr("press_new_key") + "</b>");
            GUILayout.EndVertical();
        }

        private void DrawGhostKeyButton(int i, KeyCode[] ghostKeyCodes)
        {
            bool isBound = ghostKeyCodes[i] != KeyCode.None;
            string label = isBound ? KeyToString(ghostKeyCodes[i]) : "-";
            bool selected = i == SelectedKey && changeState == 2;
            if (GUILayout.Button(selected ? "<b>" + label + "</b>" : label))
            {
                if (isBound)
                {
                    ghostKeyCodes[i] = KeyCode.None;
                    SelectedKey = -1;
                    SaveSettings();
                }
                else
                {
                    SelectedKey = i;
                    changeState = 2;
                }
            }
        }

        /// <summary>
        /// Draw the custom text editing section / 绘制自定义文本编辑区域
        /// Allows typing custom labels for each key / 允许为每个按键输入自定义标签
        /// </summary>
        private void DrawTextChangeSection()
        {
            GUILayout.BeginVertical("box");
            KeyCode[] keyCodes = GetKeyCode();
            string[] keyTexts = GetKeyText();
            KeyCode[] footKeyCodes = GetFootKeyCode();
            string[] footKeyTexts = GetFootKeyText();

            DrawMainKeyRows(I18n.Tr("row1_text"), I18n.Tr("row2_text"), I18n.Tr("row3_text"),
                keyCodes, (i, _) => { SelectedKey = i; changeState = 1; },
                (i, kc) => GetKeyTextLabel(keyTexts, keyCodes, i));
            DrawFootKeyRows(I18n.Tr("foot_keys_text"), 20,
                (i, _) => { SelectedKey = i; changeState = 1; },
                (i, kc) => GetFootKeyTextLabel(footKeyTexts, footKeyCodes, i - 20));

            if (SelectedKey != -1 && changeState == 1)
                DrawTextEditArea(keyTexts, keyCodes, footKeyTexts, footKeyCodes);
            GUILayout.EndVertical();
        }

        private static string GetKeyTextLabel(string[] keyTexts, KeyCode[] keyCodes, int i) =>
            keyTexts != null && i < keyTexts.Length && !string.IsNullOrEmpty(keyTexts[i])
                ? keyTexts[i] : KeyToString(i < keyCodes.Length ? keyCodes[i] : KeyCode.None);

        private static string GetFootKeyTextLabel(string[] footKeyTexts, KeyCode[] footKeyCodes, int fi) =>
            footKeyTexts != null && fi < footKeyTexts.Length && !string.IsNullOrEmpty(footKeyTexts[fi])
                ? footKeyTexts[fi] : KeyToString(fi < footKeyCodes.Length ? footKeyCodes[fi] : KeyCode.None);

        private void DrawTextEditArea(string[] keyTexts, KeyCode[] keyCodes, string[] footKeyTexts, KeyCode[] footKeyCodes)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(I18n.Tr("input_text") + ":");
            if (SelectedKey < 20)
                DrawMainKeyTextField(keyTexts, keyCodes);
            else
                DrawFootKeyTextField(footKeyTexts, footKeyCodes);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(I18n.Tr("reset")))
            {
                if (SelectedKey < 20)
                {
                    keyTexts[SelectedKey] = null;
                    if (Keys != null && SelectedKey < Keys.Length && Keys[SelectedKey] != null)
                        Keys[SelectedKey].text.text = KeyToString(keyCodes[SelectedKey]);
                }
                else
                {
                    int footIndex = SelectedKey - 20;
                    footKeyTexts[footIndex] = null;
                    if (Keys != null && SelectedKey < Keys.Length && Keys[SelectedKey] != null)
                        Keys[SelectedKey].text.text = KeyToString(footKeyCodes[footIndex]);
                }
                SelectedKey = -1;
                SaveSettings();
            }
            if (GUILayout.Button(I18n.Tr("save_btn")))
            {
                SelectedKey = -1;
                SaveSettings();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawMainKeyTextField(string[] keyTexts, KeyCode[] keyCodes)
        {
            string currentText = !string.IsNullOrEmpty(keyTexts[SelectedKey])
                ? keyTexts[SelectedKey] : KeyToString(keyCodes[SelectedKey]);
            string newText = GUILayout.TextField(currentText, GUILayout.Width(150));
            if (keyTexts[SelectedKey] != newText)
            {
                if (Keys != null && SelectedKey < Keys.Length && Keys[SelectedKey] != null)
                    Keys[SelectedKey].text.text = newText;
                keyTexts[SelectedKey] = string.IsNullOrEmpty(newText) || newText == KeyToString(keyCodes[SelectedKey]) ? null : newText;
            }
        }

        private void DrawFootKeyTextField(string[] footKeyTexts, KeyCode[] footKeyCodes)
        {
            int footIndex = SelectedKey - 20;
            string currentText = footKeyTexts != null && !string.IsNullOrEmpty(footKeyTexts[footIndex])
                ? footKeyTexts[footIndex] : KeyToString(footKeyCodes[footIndex]);
            string newText = GUILayout.TextField(currentText, GUILayout.Width(150));
            if (footKeyTexts[footIndex] != newText)
            {
                if (Keys != null && SelectedKey < Keys.Length && Keys[SelectedKey] != null)
                    Keys[SelectedKey].text.text = newText;
                footKeyTexts[footIndex] = string.IsNullOrEmpty(newText) || newText == KeyToString(footKeyCodes[footIndex]) ? null : newText;
            }
        }

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
            DrawKpsTotalColors(36, I18n.Tr("kps_colors"), ref kpsColorType);
            GUILayout.Space(3);
            DrawKpsTotalColors(37, I18n.Tr("total_colors"), ref totalColorType);
            GUILayout.EndVertical();
        }

        // ===== KPS & Total independent color state =====
        int kpsColorType = -1;
        int totalColorType = -1;

        // ===== Per-row rain shadow/outline foldout state =====
        bool[] rainShadowColorExpanded = new bool[3];
        bool[] rainOutlineColorExpanded = new bool[3];
        bool[] ghostShadowColorExpanded = new bool[3];
        bool[] ghostOutlineColorExpanded = new bool[3];


        private static Color KpsTotalColor(int pi, int t) => pi == 36
            ? t switch { 0 => Settings.Data.KpsBackground, 1 => Settings.Data.KpsOutline, _ => Settings.Data.KpsText }
            : t switch { 0 => Settings.Data.TotalBackground, 1 => Settings.Data.TotalOutline, _ => Settings.Data.TotalText };

        private static void SetKpsTotalColor(int pi, int t, Color c)
        {
            if (pi == 36)
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
        private Color DrawColorPicker(string label, Color currentColor, Color defaultColor)
        {
            GUILayout.BeginVertical();
            GUILayout.Label(label);
            void DrawChannel(string name, ref float channel)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(name + ":", GUILayout.Width(20));
                channel = GUILayout.HorizontalSlider(channel, 0f, 1f, GUILayout.Width(150));
                string txt = GUILayout.TextField(channel.ToString("F2"), GUILayout.Width(40));
                if (float.TryParse(txt, out float val))
                    channel = Mathf.Clamp01(val);
                GUILayout.EndHorizontal();
            }
            DrawChannel("R", ref currentColor.r);
            DrawChannel("G", ref currentColor.g);
            DrawChannel("B", ref currentColor.b);
            DrawChannel("A", ref currentColor.a);
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

            if (Settings.Data.KeyViewerStyle == KeyviewerStyle.Key20)
            {
                GUILayout.Label(I18n.Tr("row3_keys") + ":");
                GUILayout.BeginHorizontal();
                for (int i = 16; i < 20 && i < keyCodes.Length; i++)
                    DrawPerKeyColorBtn(i, KeyToString(keyCodes[i]));
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
                        DrawPerKeyColorBtn(20 + f, KeyToString(footKeyCodes[f]));
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            DrawPerKeyColorBtn(36, "KPS");
            DrawPerKeyColorBtn(37, "Total");
            GUILayout.EndHorizontal();

            if (perKeyColorSelected >= 0 && perKeyColorSelected < 38)
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
            36 => "KPS",
            37 => "Total",
            _ => KeyToString(GetKeyCodeForIndex(s))
        };

        private static int[] PerKeyTypeOrder(int s) => s >= 36
            ? new int[] { 0, 2, 4 }
            : s >= 20 ? new int[] { 0, 1, 2, 3, 4, 5 }
            : new int[] { 0, 1, 2, 3, 4, 5, 6 };

        private bool DrawColorFoldout(int t, string name)
        {
            DrawFoldoutItemButton(name, ref perKeyColorTypeIndex, t);
            return perKeyColorTypeIndex == t;
        }

        private void DrawPerKeyColorEditor(int s)
        {
            GUILayout.Space(5);
            GUILayout.Label("Key " + s + " (" + PerKeyLabel(s) + ")");
            // Foot keys (s >= 20) intentionally skip rain color; PerKeyTypeOrder excludes type 6 for them
            string rainKey = s < 8 ? "color_rain1" : s < 16 ? "color_rain2" : s < 20 ? "color_rain3" : "";

            string[] typeNames = {
                I18n.Tr("color_bg"), I18n.Tr("color_bg_clicked"),
                I18n.Tr("color_outline"), I18n.Tr("color_outline_clicked"),
                I18n.Tr("color_text"), I18n.Tr("color_text_clicked"),
                I18n.Tr(rainKey) + " (" + s + ")"
            };
            Color[] values = {
                Settings.Data.PerKeyBackground[s], Settings.Data.PerKeyBackgroundClicked[s],
                Settings.Data.PerKeyOutline[s], Settings.Data.PerKeyOutlineClicked[s],
                Settings.Data.PerKeyText[s], Settings.Data.PerKeyTextClicked[s],
                Settings.Data.PerKeyRainColor[s]
            };
            Color[] defaults = {
                Background, BackgroundClicked, Outline, OutlineClicked, Text, TextClicked, RainColor
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

            if (s < 36 && Settings.Data.Count != null && s < Settings.Data.Count.Length)
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
            int fi = idx - 20;
            if (foot != null && fi >= 0 && fi < foot.Length) return foot[fi];
            return KeyCode.None;
        }

        private static readonly (int flag, string label)[] FontStyleFlagLabels =
        {
            (1, "B"), (2, "I"), (4, "U"), (8, "Lc"),
            (16, "Uc"), (32, "Sc"), (64, "St"), (128, "Sup"), (256, "Sub")
        };

        private string BuildFontStyleSummary()
        {
            int f = Settings.Data.FontStyleFlags;
            if (f == 0) return "Normal";
            var parts = new List<string>(4);
            foreach (var (flag, label) in FontStyleFlagLabels)
                if ((f & flag) != 0) parts.Add(label);
            return string.Join(" ", parts);
        }

        private void DrawLanguageSection()
        {
            GUILayout.BeginHorizontal();
            string[] langLabels = { "English", "中文", "한국어" };
            int langIdx = Settings.Language == "en" ? 0 : Settings.Language == "zh" ? 1 : 2;
            if (GUILayout.Button(I18n.Tr("language") + ": " + langLabels[langIdx]))
            {
                langIdx = (langIdx + 1) % 3;
                Settings.Language = langIdx == 0 ? "en" : langIdx == 1 ? "zh" : "ko";
                I18n.Lang = Settings.Language;
                SaveSettings();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawCountResetSection()
        {
            bool newEnabled = GUILayout.Toggle(Settings.Data.Enabled, (Settings.Data.Enabled ? "\u2713 " : "\u2717 ") + I18n.Tr("key_display_on"));
            if (newEnabled != Settings.Data.Enabled)
            {
                Settings.Data.Enabled = newEnabled;
                SaveSettings();
            }

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            var redTextStyle = new GUIStyle(GUI.skin.button) { normal = { textColor = Color.red } };
            if (GUILayout.Button(I18n.Tr("reset_counts"), redTextStyle, GUILayout.MinWidth(120)))
                ExecuteCountReset();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            bool newFormatting = GUILayout.Toggle(Settings.Data.EnableCountFormatting, I18n.Tr("count_formatting"));
            if (newFormatting != Settings.Data.EnableCountFormatting)
            {
                Settings.Data.EnableCountFormatting = newFormatting;
                SaveSettings();
                RefreshAllCountDisplay();
            }
            GUILayout.EndHorizontal();
        }

        private void ExecuteCountReset()
        {
            lastTotal = -1;
            Settings.Data.TotalCount = 0;
            for (int i = 0; i < Settings.Data.Count.Length; i++)
                Settings.Data.Count[i] = 0;
            ClearKpsTimers();
            if (Keys != null)
                for (int i = 0; i < Keys.Length; i++)
                    if (Keys[i]?.value != null)
                        Keys[i].value.text = "0";
            if (Kps?.value != null) Kps.value.text = "0";
            if (Total?.value != null) Total.value.text = "0";
            SaveSettings();
        }

        private void DrawFontSection()
        {
            GUILayout.Label(I18n.Tr("font_style") + ":");
            string curFont = fontList.Count > 0 ? fontList[Mathf.Clamp(Settings.Data.FontIndex, 0, fontList.Count - 1)].name : "None";
            if (GUILayout.Button((fontListExpanded ? "\u25BC " : "\u25B6 ") + curFont, GUILayout.MinWidth(200)))
                fontListExpanded = !fontListExpanded;
            if (fontListExpanded)
            {
                if (fontList.Count > 0)
                {
                    int newIdx = Settings.Data.FontIndex;
                    for (int i = 0; i < fontList.Count; i++)
                    {
                        bool selected = i == Settings.Data.FontIndex;
                        if (GUILayout.Button((selected ? "\u2713 " : "  ") + fontList[i].name, GUILayout.MinWidth(200)))
                            newIdx = i;
                    }
                    if (newIdx != Settings.Data.FontIndex)
                    {
                        Settings.Data.FontIndex = newIdx;
                        Settings.Data.FontName = fontList[newIdx].name;
                        fontRestored = false;
                        UpdateAllFonts();
                        SaveSettings();
                    }
                }
                else
                    GUILayout.Label("\u25B8 " + I18n.Tr("no_fonts_found"), GUILayout.MinWidth(200));

                GUILayout.Space(5);
                GUILayout.BeginVertical("box");
                GUILayout.Label(I18n.Tr("custom_font_tip"));
                GUILayout.Label($"CustomFont : {Path.Combine(Path.GetDirectoryName(Main.Mod?.Path) ?? ".", "CustomFont")}");
                GUILayout.EndVertical();
            }

            GUILayout.Space(3);
            string styleSummary = BuildFontStyleSummary();
            fontStyleExpanded = DrawFoldoutButton(I18n.Tr("font_style") + ": " + styleSummary, fontStyleExpanded);
            if (fontStyleExpanded)
            {
                string[] styleNames = { "Bold", "Italic", "Underline", "Lowercase", "Uppercase", "SmallCaps", "Strikethrough", "Superscript", "Subscript" };
                int[] styleFlags = { 1, 2, 4, 8, 16, 32, 64, 128, 256 };
                int[] styleGroups = { 0, 0, 0, 1, 1, 1, 0, 2, 2 };
                bool changed = false;
                for (int i = 0; i < styleFlags.Length; i++)
                {
                    bool active = (Settings.Data.FontStyleFlags & styleFlags[i]) != 0;
                    bool newActive = GUILayout.Toggle(active, styleNames[i]);
                    if (newActive != active)
                    {
                        if (newActive)
                        {
                            if (styleGroups[i] == 1)
                                Settings.Data.FontStyleFlags &= ~(8 | 16 | 32);
                            else if (styleGroups[i] == 2)
                                Settings.Data.FontStyleFlags &= ~(128 | 256);
                        }
                        Settings.Data.FontStyleFlags = newActive ? Settings.Data.FontStyleFlags | styleFlags[i] : Settings.Data.FontStyleFlags & ~styleFlags[i];
                        changed = true;
                    }
                }
                if (changed)
                {
                    UpdateAllFonts();
                    SaveSettings();
                }
            }
        }

        private void DrawFolderButtons()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(I18n.Tr("open_config_folder"), GUILayout.MinWidth(120)))
            {
                string dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    System.Diagnostics.Process.Start("explorer.exe", dir);
            }
            if (GUILayout.Button(I18n.Tr("open_font_folder"), GUILayout.MinWidth(120)))
            {
                string modPath = Path.GetDirectoryName(Main.Mod?.Path) ?? ".";
                string customFontDir = Path.Combine(modPath, "CustomFont");
                if (!Directory.Exists(customFontDir)) Directory.CreateDirectory(customFontDir);
                System.Diagnostics.Process.Start("explorer.exe", customFontDir);
            }
            GUILayout.EndHorizontal();

            bool newDownLocation = GUILayout.Toggle(Settings.Data.DownLocation, I18n.Tr("place_below"));
            if (newDownLocation != Settings.Data.DownLocation)
            {
                Settings.Data.DownLocation = newDownLocation;
                ResetKeyViewer();
                ResetFootKeyViewer();
                SaveSettings();
            }
        }

        private void DrawCustomPositionSection()
        {
            CustomPositionExpanded = DrawFoldoutButton(I18n.Tr("custom_pos"), CustomPositionExpanded);
            if (!CustomPositionExpanded) return;

            GUILayout.BeginVertical("box");
            bool newEnabled = GUILayout.Toggle(Settings.Data.CustomPositionEnabled,
                I18n.Tr("custom_pos") + " " + I18n.Tr("enable"));
            if (newEnabled != Settings.Data.CustomPositionEnabled)
            {
                Settings.Data.CustomPositionEnabled = newEnabled;
                SaveSettings();
                if (newEnabled)
                {
                    ResetKeyViewerPosition();
                    ResetFootKeyViewerPosition();
                }
                else
                {
                    ResetKeyViewer();
                    ResetFootKeyViewer();
                }
            }

            if (Settings.Data.CustomPositionEnabled)
            {
                GUILayout.Label(I18n.Tr("main_key_pos") + ":");
                Vector2 tempMainPos = Settings.Data.MainKeyViewerPosition;
                Vector2 tempFootPos = Settings.Data.FootKeyViewerPosition;
                bool positionChanged = false;

                float newMainX = FloatSliderField("X", tempMainPos.x, 0f, 1f);
                if (newMainX != tempMainPos.x) { tempMainPos.x = newMainX; positionChanged = true; }
                float newMainY = FloatSliderField("Y", tempMainPos.y, 0f, 1f);
                if (newMainY != tempMainPos.y) { tempMainPos.y = newMainY; positionChanged = true; }

                GUILayout.Label(I18n.Tr("foot_key_pos") + ":");
                float newFootX = FloatSliderField("X", tempFootPos.x, 0f, 1f);
                if (newFootX != tempFootPos.x) { tempFootPos.x = newFootX; positionChanged = true; }
                float newFootY = FloatSliderField("Y", tempFootPos.y, 0f, 1f);
                if (newFootY != tempFootPos.y) { tempFootPos.y = newFootY; positionChanged = true; }

                if (positionChanged)
                {
                    Settings.Data.MainKeyViewerPosition = tempMainPos;
                    Settings.Data.FootKeyViewerPosition = tempFootPos;
                    ResetKeyViewerPosition();
                    ResetFootKeyViewerPosition();
                    SaveSettings();
                }

                if (GUILayout.Button(I18n.Tr("reset_pos")))
                {
                    Settings.Data.MainKeyViewerPosition = new Vector2(0, 1);
                    Settings.Data.FootKeyViewerPosition = new Vector2(0.24f, 1f);
                    ResetKeyViewerPosition();
                    ResetFootKeyViewerPosition();
                    SaveSettings();
                }
            }
            GUILayout.EndVertical();
        }

        private void DrawLayoutSection()
        {
            GUILayout.Label(I18n.Tr("key_layout") + ":");
            KeyviewerStyle newStyle = (KeyviewerStyle)GUILayout.SelectionGrid((int)Settings.Data.KeyViewerStyle, KeyLayoutNames, 3);
            if (newStyle != Settings.Data.KeyViewerStyle)
            {
                Settings.Data.KeyViewerStyle = newStyle;
                ChangeKeyViewer();
                SaveSettings();
            }

            GUILayout.Label(I18n.Tr("foot_keys") + ":");
            FootKeyviewerStyle newFootStyle = (FootKeyviewerStyle)GUILayout.SelectionGrid((int)Settings.Data.FootKeyViewerStyle, FootKeyLayoutNames, 5);
            if (newFootStyle != Settings.Data.FootKeyViewerStyle)
            {
                Settings.Data.FootKeyViewerStyle = newFootStyle;
                ResetFootKeyViewer();
                SaveSettings();
            }

            float newSettingsSize = FloatSliderField(I18n.Tr("size"), Settings.Data.Size, 0.1f, 2f);
            if (newSettingsSize != Settings.Data.Size)
            {
                Settings.Data.Size = newSettingsSize;
                if (KeyViewerSizeObject != null)
                    KeyViewerSizeObject.transform.localScale = new Vector3(Settings.Data.Size, Settings.Data.Size, 1);
                SaveSettings();
            }
        }

        private void DrawDisplaySection()
        {
            bool newHideCount = GUILayout.Toggle(Settings.Data.HideMainKeyCount, I18n.Tr("hide_main_count"));
            if (newHideCount != Settings.Data.HideMainKeyCount)
            {
                Settings.Data.HideMainKeyCount = newHideCount;
                ResetKeyViewer();
                SaveSettings();
            }

            if (!Settings.Data.HideMainKeyCount)
            {
                bool newPerKeyKps = GUILayout.Toggle(Settings.Data.EnablePerKeyKps, I18n.Tr("per_key_kps"));
                if (newPerKeyKps != Settings.Data.EnablePerKeyKps)
                {
                    Settings.Data.EnablePerKeyKps = newPerKeyKps;
                    RefreshAllCountDisplay();
                    SaveSettings();
                }
            }

            bool newStreamer = GUILayout.Toggle(Settings.Data.StreamerMode, I18n.Tr("streamer_mode"));
            if (newStreamer != Settings.Data.StreamerMode)
            {
                Settings.Data.StreamerMode = newStreamer;
                if (Kps != null) Kps.gameObject.SetActive(!newStreamer);
                if (Total != null) Total.gameObject.SetActive(!newStreamer);
                SaveSettings();
            }
        }

        private void DrawRainSection()
        {
            RainExpanded = DrawFoldoutButton(I18n.Tr("rain_effect"), RainExpanded);
            if (!RainExpanded) return;

            bool newRainEffect = GUILayout.Toggle(Settings.Data.EnableRainEffect, I18n.Tr("rain_effect"));
            if (newRainEffect != Settings.Data.EnableRainEffect)
            {
                Settings.Data.EnableRainEffect = newRainEffect;
                if (!Settings.Data.EnableRainEffect)
                    rainSystem.ClearActiveDrops(Keys);
                SaveSettings();
            }

            if (!Settings.Data.EnableRainEffect) return;

            GUILayout.Label(I18n.Tr("rain_rows") + ":");
            GUILayout.BeginHorizontal();
            Settings.Data.EnableRainForRow1 = GUILayout.Toggle(Settings.Data.EnableRainForRow1, I18n.Tr("rain_row1"));
            Settings.Data.EnableRainForRow2 = GUILayout.Toggle(Settings.Data.EnableRainForRow2, I18n.Tr("rain_row2"));
            if (Settings.Data.KeyViewerStyle == KeyviewerStyle.Key20)
                Settings.Data.EnableRainForRow3 = GUILayout.Toggle(Settings.Data.EnableRainForRow3, I18n.Tr("rain_row3"));
            GUILayout.EndHorizontal();

            GUILayout.Label(I18n.Tr("rain_height") + ":");
            Settings.Data.RainHeightRow1 = FloatSliderField(I18n.Tr("rain_row1"), Settings.Data.RainHeightRow1, 1f, 2000f);
            Settings.Data.RainHeightRow2 = FloatSliderField(I18n.Tr("rain_row2"), Settings.Data.RainHeightRow2, 1f, 2000f);
            if (Settings.Data.KeyViewerStyle == KeyviewerStyle.Key20)
                Settings.Data.RainHeightRow3 = FloatSliderField(I18n.Tr("rain_row3"), Settings.Data.RainHeightRow3, 1f, 2000f);

            GUILayout.Label(I18n.Tr("rain_speed") + ":");
            Settings.Data.RainSpeedRow1 = FloatSliderField(I18n.Tr("rain_row1"), Settings.Data.RainSpeedRow1, 50f, 2000f, "F0");
            Settings.Data.RainSpeedRow2 = FloatSliderField(I18n.Tr("rain_row2"), Settings.Data.RainSpeedRow2, 50f, 2000f, "F0");
            if (Settings.Data.KeyViewerStyle == KeyviewerStyle.Key20)
                Settings.Data.RainSpeedRow3 = FloatSliderField(I18n.Tr("rain_row3"), Settings.Data.RainSpeedRow3, 50f, 2000f, "F0");

            GUILayout.Label(I18n.Tr("rain_width") + ":");
            Settings.Data.RainWidthRow1 = FloatSliderField(I18n.Tr("rain_width_row1"), Settings.Data.RainWidthRow1, 10f, 200f, "F0");
            Settings.Data.RainWidthRow2 = FloatSliderField(I18n.Tr("rain_width_row2"), Settings.Data.RainWidthRow2, 10f, 200f, "F0");
            if (Settings.Data.KeyViewerStyle == KeyviewerStyle.Key20)
                Settings.Data.RainWidthRow3 = FloatSliderField(I18n.Tr("rain_width_row3"), Settings.Data.RainWidthRow3, 10f, 200f, "F0");

            GUILayout.Label(I18n.Tr("rain_start_y") + ":");
            float newStartY1 = FloatSliderField(I18n.Tr("rain_row1"), Settings.Data.RainStartYRow1, -2000f, 1000f, "F0");
            if (newStartY1 != Settings.Data.RainStartYRow1) { Settings.Data.RainStartYRow1 = newStartY1; UpdateRainContainerPositions(); }
            float newStartY2 = FloatSliderField(I18n.Tr("rain_row2"), Settings.Data.RainStartYRow2, -2000f, 1000f, "F0");
            if (newStartY2 != Settings.Data.RainStartYRow2) { Settings.Data.RainStartYRow2 = newStartY2; UpdateRainContainerPositions(); }
            if (Settings.Data.KeyViewerStyle == KeyviewerStyle.Key20)
            {
                float newStartY3 = FloatSliderField(I18n.Tr("rain_row3"), Settings.Data.RainStartYRow3, -2000f, 1000f, "F0");
                if (newStartY3 != Settings.Data.RainStartYRow3) { Settings.Data.RainStartYRow3 = newStartY3; UpdateRainContainerPositions(); }
            }

            GUILayout.Space(5);
            bool newRainFade = GUILayout.Toggle(Settings.Data.EnableRainFade, I18n.Tr("rain_fade"));
            if (newRainFade != Settings.Data.EnableRainFade)
            {
                Settings.Data.EnableRainFade = newRainFade;
                if (!newRainFade && rainSystem != null && Keys != null)
                    rainSystem.ClearActiveDrops(Keys);
                SaveSettings();
            }
            if (Settings.Data.EnableRainFade)
            {
                float newFadeDur = FloatSliderField(I18n.Tr("fade_duration"), Settings.Data.RainFadeDuration, 0.03f, 5.0f);
                if (newFadeDur != Settings.Data.RainFadeDuration)
                {
                    Settings.Data.RainFadeDuration = newFadeDur;
                    SaveSettings();
                }
            }

            GUILayout.Space(5);
            bool newGradient = GUILayout.Toggle(Settings.Data.EnableRainGradient, I18n.Tr("rain_gradient"));
            if (newGradient != Settings.Data.EnableRainGradient)
            {
                Settings.Data.EnableRainGradient = newGradient;
                SaveSettings();
            }
            if (Settings.Data.EnableRainGradient)
            {
                float newFadePx = FloatSliderField(I18n.Tr("gradient_percent"), Settings.Data.RainFadePx, 1f, 200f, "F0");
                if (!Mathf.Approximately(newFadePx, Settings.Data.RainFadePx))
                {
                    Settings.Data.RainFadePx = newFadePx;
                    SaveSettings();
                }
            }

            GUILayout.Space(5);
            bool newGhostRain = GUILayout.Toggle(Settings.Data.EnableGhostRain, I18n.Tr("ghost_rain"));
            if (newGhostRain != Settings.Data.EnableGhostRain)
            {
                Settings.Data.EnableGhostRain = newGhostRain;
                if (!newGhostRain && rainSystem != null && Keys != null)
                    rainSystem.ClearActiveDrops(Keys);
                SaveSettings();
            }
            if (Settings.Data.EnableGhostRain)
            {
                DrawGhostRainShadowSection();
                DrawGhostRainOutlineSection();
                GUILayout.Label(I18n.Tr("ghost_rain") + " " + I18n.Tr("rain_start_y") + ":");
                float newGY1 = FloatSliderField(I18n.Tr("rain_row1"), Settings.Data.GhostRainStartYRow1, -2000f, 1000f, "F0");
                if (!Mathf.Approximately(newGY1, Settings.Data.GhostRainStartYRow1)) { Settings.Data.GhostRainStartYRow1 = newGY1; UpdateGhostRainStartY(); SaveSettings(); }
                float newGY2 = FloatSliderField(I18n.Tr("rain_row2"), Settings.Data.GhostRainStartYRow2, -2000f, 1000f, "F0");
                if (!Mathf.Approximately(newGY2, Settings.Data.GhostRainStartYRow2)) { Settings.Data.GhostRainStartYRow2 = newGY2; UpdateGhostRainStartY(); SaveSettings(); }
                if (Settings.Data.KeyViewerStyle == KeyviewerStyle.Key20)
                {
                    float newGY3 = FloatSliderField(I18n.Tr("rain_row3"), Settings.Data.GhostRainStartYRow3, -2000f, 1000f, "F0");
                    if (!Mathf.Approximately(newGY3, Settings.Data.GhostRainStartYRow3)) { Settings.Data.GhostRainStartYRow3 = newGY3; UpdateGhostRainStartY(); SaveSettings(); }
                }

                GUILayout.Label(I18n.Tr("ghost_rain_height") + ":");
                Settings.Data.GhostRainHeightRow1 = FloatSliderField(I18n.Tr("rain_row1"), Settings.Data.GhostRainHeightRow1, 1f, 2000f);
                Settings.Data.GhostRainHeightRow2 = FloatSliderField(I18n.Tr("rain_row2"), Settings.Data.GhostRainHeightRow2, 1f, 2000f);
                if (Settings.Data.KeyViewerStyle == KeyviewerStyle.Key20)
                    Settings.Data.GhostRainHeightRow3 = FloatSliderField(I18n.Tr("rain_row3"), Settings.Data.GhostRainHeightRow3, 1f, 2000f);

                GUILayout.Label(I18n.Tr("ghost_rain_speed") + ":");
                Settings.Data.GhostRainSpeedRow1 = FloatSliderField(I18n.Tr("rain_row1"), Settings.Data.GhostRainSpeedRow1, 50f, 2000f, "F0");
                Settings.Data.GhostRainSpeedRow2 = FloatSliderField(I18n.Tr("rain_row2"), Settings.Data.GhostRainSpeedRow2, 50f, 2000f, "F0");
                if (Settings.Data.KeyViewerStyle == KeyviewerStyle.Key20)
                    Settings.Data.GhostRainSpeedRow3 = FloatSliderField(I18n.Tr("rain_row3"), Settings.Data.GhostRainSpeedRow3, 50f, 2000f, "F0");

                GUILayout.Label(I18n.Tr("ghost_rain_width") + ":");
                Settings.Data.GhostRainWidthRow1 = FloatSliderField(I18n.Tr("rain_row1"), Settings.Data.GhostRainWidthRow1, 10f, 200f, "F0");
                Settings.Data.GhostRainWidthRow2 = FloatSliderField(I18n.Tr("rain_row2"), Settings.Data.GhostRainWidthRow2, 10f, 200f, "F0");
                if (Settings.Data.KeyViewerStyle == KeyviewerStyle.Key20)
                    Settings.Data.GhostRainWidthRow3 = FloatSliderField(I18n.Tr("rain_row3"), Settings.Data.GhostRainWidthRow3, 10f, 200f, "F0");
            }

            GUILayout.Space(8);
            DrawRainShadowSection();
            DrawRainOutlineSection();

            GUILayout.Space(5);
        }

        private void DrawGhostRainShadowSection()
        {
            GUILayout.Label(I18n.Tr("ghost_rain") + " " + I18n.Tr("rain_shadow"));
            for (int r = 0; r < 3; r++)
            {
                if (r == 2 && Settings.Data.KeyViewerStyle != KeyviewerStyle.Key20) break;
                string rowLabel = r == 0 ? I18n.Tr("rain_row1") : r == 1 ? I18n.Tr("rain_row2") : I18n.Tr("rain_row3");
                bool shadowEn = r == 0 ? Settings.Data.EnableGhostRainShadowRow1 : r == 1 ? Settings.Data.EnableGhostRainShadowRow2 : Settings.Data.EnableGhostRainShadowRow3;
                bool newShadowEn = GUILayout.Toggle(shadowEn, rowLabel);
                if (newShadowEn != shadowEn)
                {
                    if (r == 0) Settings.Data.EnableGhostRainShadowRow1 = newShadowEn;
                    else if (r == 1) Settings.Data.EnableGhostRainShadowRow2 = newShadowEn;
                    else Settings.Data.EnableGhostRainShadowRow3 = newShadowEn;
                    if (rainSystem != null && Keys != null) rainSystem.ClearActiveDrops(Keys);
                    SaveSettings();
                }
                if (!shadowEn) continue;
                GUILayout.BeginVertical("box");
                ghostShadowColorExpanded[r] = DrawFoldoutButton(I18n.Tr("rain_shadow_color"), ghostShadowColorExpanded[r]);
                if (ghostShadowColorExpanded[r])
                {
                    Color curCol = r == 0 ? Settings.Data.GhostRainShadowColorRow1 : r == 1 ? Settings.Data.GhostRainShadowColorRow2 : Settings.Data.GhostRainShadowColorRow3;
                    Color newCol = DrawColorPicker("", curCol, RainShadowColorDefault);
                    if (newCol != curCol)
                    {
                        if (r == 0) Settings.Data.GhostRainShadowColorRow1 = newCol;
                        else if (r == 1) Settings.Data.GhostRainShadowColorRow2 = newCol;
                        else Settings.Data.GhostRainShadowColorRow3 = newCol;
                        SaveSettings();
                    }
                }
                float curOffX = r == 0 ? Settings.Data.GhostRainShadowOffsetXRow1 : r == 1 ? Settings.Data.GhostRainShadowOffsetXRow2 : Settings.Data.GhostRainShadowOffsetXRow3;
                float curOffY = r == 0 ? Settings.Data.GhostRainShadowOffsetYRow1 : r == 1 ? Settings.Data.GhostRainShadowOffsetYRow2 : Settings.Data.GhostRainShadowOffsetYRow3;
                float newOffX = FloatSliderField("X " + I18n.Tr("rain_shadow_offset"), curOffX, -10f, 10f);
                if (newOffX != curOffX)
                {
                    if (r == 0) Settings.Data.GhostRainShadowOffsetXRow1 = newOffX;
                    else if (r == 1) Settings.Data.GhostRainShadowOffsetXRow2 = newOffX;
                    else Settings.Data.GhostRainShadowOffsetXRow3 = newOffX;
                    SaveSettings();
                }
                float newOffY = FloatSliderField("Y " + I18n.Tr("rain_shadow_offset"), curOffY, -10f, 10f);
                if (newOffY != curOffY)
                {
                    if (r == 0) Settings.Data.GhostRainShadowOffsetYRow1 = newOffY;
                    else if (r == 1) Settings.Data.GhostRainShadowOffsetYRow2 = newOffY;
                    else Settings.Data.GhostRainShadowOffsetYRow3 = newOffY;
                    SaveSettings();
                }
                GUILayout.EndVertical();
            }
        }

        private void DrawGhostRainOutlineSection()
        {
            GUILayout.Label(I18n.Tr("ghost_rain") + " " + I18n.Tr("rain_outline"));
            for (int r = 0; r < 3; r++)
            {
                if (r == 2 && Settings.Data.KeyViewerStyle != KeyviewerStyle.Key20) break;
                string rowLabel = r == 0 ? I18n.Tr("rain_row1") : r == 1 ? I18n.Tr("rain_row2") : I18n.Tr("rain_row3");
                bool outlineEn = r == 0 ? Settings.Data.EnableGhostRainOutlineRow1 : r == 1 ? Settings.Data.EnableGhostRainOutlineRow2 : Settings.Data.EnableGhostRainOutlineRow3;
                bool newOutlineEn = GUILayout.Toggle(outlineEn, rowLabel);
                if (newOutlineEn != outlineEn)
                {
                    if (r == 0) Settings.Data.EnableGhostRainOutlineRow1 = newOutlineEn;
                    else if (r == 1) Settings.Data.EnableGhostRainOutlineRow2 = newOutlineEn;
                    else Settings.Data.EnableGhostRainOutlineRow3 = newOutlineEn;
                    if (rainSystem != null && Keys != null) rainSystem.ClearActiveDrops(Keys);
                    SaveSettings();
                }
                if (!outlineEn) continue;
                GUILayout.BeginVertical("box");
                ghostOutlineColorExpanded[r] = DrawFoldoutButton(I18n.Tr("rain_outline_color"), ghostOutlineColorExpanded[r]);
                if (ghostOutlineColorExpanded[r])
                {
                    Color curCol = r == 0 ? Settings.Data.GhostRainOutlineColorRow1 : r == 1 ? Settings.Data.GhostRainOutlineColorRow2 : Settings.Data.GhostRainOutlineColorRow3;
                    Color newCol = DrawColorPicker("", curCol, RainOutlineColorDefault);
                    if (newCol != curCol)
                    {
                        if (r == 0) Settings.Data.GhostRainOutlineColorRow1 = newCol;
                        else if (r == 1) Settings.Data.GhostRainOutlineColorRow2 = newCol;
                        else Settings.Data.GhostRainOutlineColorRow3 = newCol;
                        SaveSettings();
                    }
                }
                float curW = r == 0 ? Settings.Data.GhostRainOutlineWidthRow1 : r == 1 ? Settings.Data.GhostRainOutlineWidthRow2 : Settings.Data.GhostRainOutlineWidthRow3;
                float newW = FloatSliderField(I18n.Tr("rain_outline_width"), curW, 0.5f, 10f, "F1");
                if (newW != curW)
                {
                    if (r == 0) Settings.Data.GhostRainOutlineWidthRow1 = newW;
                    else if (r == 1) Settings.Data.GhostRainOutlineWidthRow2 = newW;
                    else Settings.Data.GhostRainOutlineWidthRow3 = newW;
                    SaveSettings();
                }
                GUILayout.EndVertical();
            }
        }

        private void DrawRainShadowSection()
        {
            GUILayout.Label(I18n.Tr("rain_shadow"));
            for (int r = 0; r < 3; r++)
            {
                if (r == 2 && Settings.Data.KeyViewerStyle != KeyviewerStyle.Key20) break;
                string rowLabel = r == 0 ? I18n.Tr("rain_row1") : r == 1 ? I18n.Tr("rain_row2") : I18n.Tr("rain_row3");
                bool shadowEn = r == 0 ? Settings.Data.EnableRainShadowRow1 : r == 1 ? Settings.Data.EnableRainShadowRow2 : Settings.Data.EnableRainShadowRow3;
                bool newShadowEn = GUILayout.Toggle(shadowEn, rowLabel);
                if (newShadowEn != shadowEn)
                {
                    if (r == 0) Settings.Data.EnableRainShadowRow1 = newShadowEn;
                    else if (r == 1) Settings.Data.EnableRainShadowRow2 = newShadowEn;
                    else Settings.Data.EnableRainShadowRow3 = newShadowEn;
                    if (rainSystem != null && Keys != null) rainSystem.ClearActiveDrops(Keys);
                    SaveSettings();
                }
                if (!shadowEn) continue;
                GUILayout.BeginVertical("box");
                rainShadowColorExpanded[r] = DrawFoldoutButton(I18n.Tr("rain_shadow_color"), rainShadowColorExpanded[r]);
                if (rainShadowColorExpanded[r])
                {
                    Color curCol = r == 0 ? Settings.Data.RainShadowColorRow1 : r == 1 ? Settings.Data.RainShadowColorRow2 : Settings.Data.RainShadowColorRow3;
                    Color newCol = DrawColorPicker("", curCol, RainShadowColorDefault);
                    if (newCol != curCol)
                    {
                        if (r == 0) Settings.Data.RainShadowColorRow1 = newCol;
                        else if (r == 1) Settings.Data.RainShadowColorRow2 = newCol;
                        else Settings.Data.RainShadowColorRow3 = newCol;
                        SaveSettings();
                    }
                }
                float curOffX = r == 0 ? Settings.Data.RainShadowOffsetXRow1 : r == 1 ? Settings.Data.RainShadowOffsetXRow2 : Settings.Data.RainShadowOffsetXRow3;
                float curOffY = r == 0 ? Settings.Data.RainShadowOffsetYRow1 : r == 1 ? Settings.Data.RainShadowOffsetYRow2 : Settings.Data.RainShadowOffsetYRow3;
                float newOffX = FloatSliderField("X " + I18n.Tr("rain_shadow_offset"), curOffX, -10f, 10f);
                if (newOffX != curOffX)
                {
                    if (r == 0) Settings.Data.RainShadowOffsetXRow1 = newOffX;
                    else if (r == 1) Settings.Data.RainShadowOffsetXRow2 = newOffX;
                    else Settings.Data.RainShadowOffsetXRow3 = newOffX;
                    SaveSettings();
                }
                float newOffY = FloatSliderField("Y " + I18n.Tr("rain_shadow_offset"), curOffY, -10f, 10f);
                if (newOffY != curOffY)
                {
                    if (r == 0) Settings.Data.RainShadowOffsetYRow1 = newOffY;
                    else if (r == 1) Settings.Data.RainShadowOffsetYRow2 = newOffY;
                    else Settings.Data.RainShadowOffsetYRow3 = newOffY;
                    SaveSettings();
                }
                GUILayout.EndVertical();
            }
        }

        private void DrawRainOutlineSection()
        {
            GUILayout.Label(I18n.Tr("rain_outline"));
            for (int r = 0; r < 3; r++)
            {
                if (r == 2 && Settings.Data.KeyViewerStyle != KeyviewerStyle.Key20) break;
                string rowLabel = r == 0 ? I18n.Tr("rain_row1") : r == 1 ? I18n.Tr("rain_row2") : I18n.Tr("rain_row3");
                bool outlineEn = r == 0 ? Settings.Data.EnableRainOutlineRow1 : r == 1 ? Settings.Data.EnableRainOutlineRow2 : Settings.Data.EnableRainOutlineRow3;
                bool newOutlineEn = GUILayout.Toggle(outlineEn, rowLabel);
                if (newOutlineEn != outlineEn)
                {
                    if (r == 0) Settings.Data.EnableRainOutlineRow1 = newOutlineEn;
                    else if (r == 1) Settings.Data.EnableRainOutlineRow2 = newOutlineEn;
                    else Settings.Data.EnableRainOutlineRow3 = newOutlineEn;
                    if (rainSystem != null && Keys != null) rainSystem.ClearActiveDrops(Keys);
                    SaveSettings();
                }
                if (!outlineEn) continue;
                GUILayout.BeginVertical("box");
                rainOutlineColorExpanded[r] = DrawFoldoutButton(I18n.Tr("rain_outline_color"), rainOutlineColorExpanded[r]);
                if (rainOutlineColorExpanded[r])
                {
                    Color curCol = r == 0 ? Settings.Data.RainOutlineColorRow1 : r == 1 ? Settings.Data.RainOutlineColorRow2 : Settings.Data.RainOutlineColorRow3;
                    Color newCol = DrawColorPicker("", curCol, RainOutlineColorDefault);
                    if (newCol != curCol)
                    {
                        if (r == 0) Settings.Data.RainOutlineColorRow1 = newCol;
                        else if (r == 1) Settings.Data.RainOutlineColorRow2 = newCol;
                        else Settings.Data.RainOutlineColorRow3 = newCol;
                        SaveSettings();
                    }
                }
                float curW = r == 0 ? Settings.Data.RainOutlineWidthRow1 : r == 1 ? Settings.Data.RainOutlineWidthRow2 : Settings.Data.RainOutlineWidthRow3;
                float newW = FloatSliderField(I18n.Tr("rain_outline_width"), curW, 0.5f, 10f, "F1");
                if (newW != curW)
                {
                    if (r == 0) Settings.Data.RainOutlineWidthRow1 = newW;
                    else if (r == 1) Settings.Data.RainOutlineWidthRow2 = newW;
                    else Settings.Data.RainOutlineWidthRow3 = newW;
                    SaveSettings();
                }
                GUILayout.EndVertical();
            }
        }

        private void DrawBindingSection()
        {
            KeyChangeExpanded = DrawFoldoutButton(I18n.Tr("key_change"), KeyChangeExpanded);
            if (KeyChangeExpanded)
                DrawKeyChangeSection();

            if (Settings.Data.EnableRainEffect && Settings.Data.EnableGhostRain)
            {
                GhostRainChangeExpanded = DrawFoldoutButton(I18n.Tr("ghost_rain"), GhostRainChangeExpanded);
                if (GhostRainChangeExpanded)
                    DrawGhostKeyChangeSection();
            }

            TextChangeExpanded = DrawFoldoutButton(I18n.Tr("text_change"), TextChangeExpanded);
            if (TextChangeExpanded)
                DrawTextChangeSection();
        }

        private void DrawColorSection()
        {
            bool colorsExpanded = DrawFoldoutButton(I18n.Tr("colors"), ColorExpanded != null);
            if (colorsExpanded && ColorExpanded == null) ColorExpanded = new bool[12];
            if (!colorsExpanded) ColorExpanded = null;
            if (ColorExpanded == null) return;

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
    }
}
