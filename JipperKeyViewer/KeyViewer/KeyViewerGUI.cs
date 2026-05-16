// Settings GUI window drawn inside UnityModManager / 在 UnityModManager 内绘制的设置 GUI 窗口
// All user-facing configuration UI: language, fonts, position, layout, colors, key rebinding, text editing / 所有面向用户的配置 UI：语言、字体、位置、布局、颜色、按键重绑定、文本编辑

using System.IO;
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
        /// <summary>
        /// Draw the main settings window / 绘制主设置窗口
        /// Contains: language toggle, enable/disable, font selection, placement, custom positioning, layout, size, rain, key change, text change, colors / 包含：语言切换、启用/禁用、字体选择、位置、自定义定位、布局、大小、雨滴、按键更改、文本更改、颜色
        /// </summary>
        public void DrawSettingsWindow()
        {
            GUILayout.BeginVertical();
            // Language toggle / 语言切换
            GUILayout.BeginHorizontal();
            string[] langLabels = { "English", "中文", "한국어" };
            int langIdx = Settings.Language == "en" ? 0 : Settings.Language == "zh" ? 1 : 2;
            string langNow = I18n.Tr("language") + ": " + langLabels[langIdx];
            if (GUILayout.Button(langNow))
            {
                langIdx = (langIdx + 1) % 3;
                Settings.Language = langIdx == 0 ? "en" : langIdx == 1 ? "zh" : "ko";
                I18n.Lang = Settings.Language;
                SaveSettings();
            }
            GUILayout.EndHorizontal();

            // Enable/disable toggle / 启用/禁用开关
            bool newEnabled = GUILayout.Toggle(Settings.Enabled, (Settings.Enabled ? "\u2713 " : "\u2717 ") + I18n.Tr("key_display_on"));
            if (newEnabled != Settings.Enabled)
            {
                Settings.Enabled = newEnabled;
                SaveSettings();
            }

            // Count reset / 计数重置
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            var redTextStyle = new GUIStyle(GUI.skin.button) { normal = { textColor = Color.red } };
            if (GUILayout.Button(I18n.Tr("reset_counts"), redTextStyle, GUILayout.MinWidth(120)))
            {
                lastTotal = -1;
                lastKps = -1;
                Settings.TotalCount = 0;
                for (int i = 0; i < Settings.Count.Length; i++)
                    Settings.Count[i] = 0;
                if (PressTimes != null) PressTimes.Clear();
                if (Keys != null)
                {
                    for (int i = 0; i < Keys.Length; i++)
                    {
                        if (Keys[i] != null && Keys[i].value != null)
                            Keys[i].value.text = "0";
                    }
                }
                if (Kps != null && Kps.value != null) Kps.value.text = "0";
                if (Total != null && Total.value != null) Total.value.text = "0";
                SaveSettings();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            bool newFormatting = GUILayout.Toggle(Settings.EnableCountFormatting, I18n.Tr("count_formatting"));
            if (newFormatting != Settings.EnableCountFormatting)
            {
                Settings.EnableCountFormatting = newFormatting;
                SaveSettings();
                RefreshAllCountDisplay();
            }
            GUILayout.EndHorizontal();

            // Font selection dropdown / 字体选择下拉菜单
            GUILayout.Label(I18n.Tr("font_style") + ":");
            string curFont = fontList.Count > 0 ? fontList[Settings.FontIndex].name : "None";
            if (GUILayout.Button((fontListExpanded ? "\u25BC " : "\u25B6 ") + curFont, GUILayout.MinWidth(200)))
                fontListExpanded = !fontListExpanded;
            if (fontListExpanded)
            {
                if (fontList.Count > 1)
                {
                    int newIdx = Settings.FontIndex;
                    for (int i = 0; i < fontList.Count; i++)
                    {
                        bool selected = i == Settings.FontIndex;
                        string label = (selected ? "\u2713 " : "  ") + fontList[i].name;
                        if (GUILayout.Button(label, GUILayout.MinWidth(200)))
                            newIdx = i;
                    }
                    if (newIdx != Settings.FontIndex)
                    {
                        Settings.FontIndex = newIdx;
                        Settings.FontName = fontList[newIdx].name;
                        fontRestored = false;
                        UpdateAllFonts();
                        SaveSettings();
                    }
                }
                else
                {
                    GUILayout.Label("\u25B8 " + I18n.Tr("no_fonts_found"), GUILayout.MinWidth(200));
                }

                GUILayout.Space(5);
                GUILayout.BeginVertical("box");
                GUILayout.Label(I18n.Tr("custom_font_tip"));
                GUILayout.Label($"CustomFont : {Path.Combine(Path.GetDirectoryName(Main.Mod?.Path) ?? ".", "CustomFont")}");
                GUILayout.EndVertical();
            }

            // DownLocation toggle (place below) / 下移位置开关
            bool newDownLocation = GUILayout.Toggle(Settings.DownLocation, I18n.Tr("place_below"));
            if (newDownLocation != Settings.DownLocation)
            {
                Settings.DownLocation = newDownLocation;
                ResetKeyViewer();
                ResetFootKeyViewer();
                SaveSettings();
            }

            GUILayout.Space(10);

            // Custom position toggle / 自定义位置开关
            bool newCustomPosition = GUILayout.Toggle(Settings.CustomPositionEnabled,
                (Settings.CustomPositionEnabled ? "\u25E2 " : "\u25B6 ") + I18n.Tr("custom_pos"));
            if (newCustomPosition != Settings.CustomPositionEnabled)
            {
                Settings.CustomPositionEnabled = newCustomPosition;
                SaveSettings();
                if (Settings.CustomPositionEnabled)
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

            // Custom position sliders (normalized 0-1) / 自定义位置滑块（归一化 0-1）
            if (Settings.CustomPositionEnabled)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label(I18n.Tr("main_key_pos") + ":");
                Vector2 tempMainPos = Settings.MainKeyViewerPosition;
                Vector2 tempFootPos = Settings.FootKeyViewerPosition;
                bool positionChanged = false;

                GUILayout.BeginHorizontal();
                GUILayout.Label("X:", GUILayout.Width(20));
                float newMainX = GUILayout.HorizontalSlider(tempMainPos.x, 0f, 1f, GUILayout.Width(120));
                if (newMainX != tempMainPos.x)
                {
                    tempMainPos.x = newMainX;
                    positionChanged = true;
                }
                string mainXText = GUILayout.TextField(tempMainPos.x.ToString("F2"), FloatFieldWidth(tempMainPos.x.ToString("F2")));
                if (float.TryParse(mainXText, out float parsedMainX) && parsedMainX != tempMainPos.x)
                {
                    tempMainPos.x = Mathf.Clamp01(parsedMainX);
                    positionChanged = true;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Y:", GUILayout.Width(20));
                float newMainY = GUILayout.HorizontalSlider(tempMainPos.y, 0f, 1f, GUILayout.Width(120));
                if (newMainY != tempMainPos.y)
                {
                    tempMainPos.y = newMainY;
                    positionChanged = true;
                }
                string mainYText = GUILayout.TextField(tempMainPos.y.ToString("F2"), FloatFieldWidth(tempMainPos.y.ToString("F2")));
                if (float.TryParse(mainYText, out float parsedMainY) && parsedMainY != tempMainPos.y)
                {
                    tempMainPos.y = Mathf.Clamp01(parsedMainY);
                    positionChanged = true;
                }
                GUILayout.EndHorizontal();

                GUILayout.Label(I18n.Tr("foot_key_pos") + ":");

                GUILayout.BeginHorizontal();
                GUILayout.Label("X:", GUILayout.Width(20));
                float newFootX = GUILayout.HorizontalSlider(tempFootPos.x, 0f, 1f, GUILayout.Width(120));
                if (newFootX != tempFootPos.x)
                {
                    tempFootPos.x = newFootX;
                    positionChanged = true;
                }
                string footXText = GUILayout.TextField(tempFootPos.x.ToString("F2"), FloatFieldWidth(tempFootPos.x.ToString("F2")));
                if (float.TryParse(footXText, out float parsedFootX) && parsedFootX != tempFootPos.x)
                {
                    tempFootPos.x = Mathf.Clamp01(parsedFootX);
                    positionChanged = true;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Y:", GUILayout.Width(20));
                float newFootY = GUILayout.HorizontalSlider(tempFootPos.y, 0f, 1f, GUILayout.Width(120));
                if (newFootY != tempFootPos.y)
                {
                    tempFootPos.y = newFootY;
                    positionChanged = true;
                }
                string footYText = GUILayout.TextField(tempFootPos.y.ToString("F2"), FloatFieldWidth(tempFootPos.y.ToString("F2")));
                if (float.TryParse(footYText, out float parsedFootY) && parsedFootY != tempFootPos.y)
                {
                    tempFootPos.y = Mathf.Clamp01(parsedFootY);
                    positionChanged = true;
                }
                GUILayout.EndHorizontal();

                if (positionChanged)
                {
                    Settings.MainKeyViewerPosition = tempMainPos;
                    Settings.FootKeyViewerPosition = tempFootPos;
                    ResetKeyViewerPosition();
                    ResetFootKeyViewerPosition();
                    SaveSettings();
                }

                if (GUILayout.Button(I18n.Tr("reset_pos")))
                {
                    Settings.MainKeyViewerPosition = new Vector2(0, 1);
                    Settings.FootKeyViewerPosition = new Vector2(0.24f, 1f);
                    ResetKeyViewerPosition();
                    ResetFootKeyViewerPosition();
                    SaveSettings();
                }
                GUILayout.EndVertical();
            }

            // Key layout selection grid / 按键布局选择网格
            GUILayout.Label(I18n.Tr("key_layout") + ":");
            KeyviewerStyle newStyle = (KeyviewerStyle)GUILayout.SelectionGrid((int)Settings.KeyViewerStyle,
                KeyLayoutNames, 3);
            if (newStyle != Settings.KeyViewerStyle)
            {
                Settings.KeyViewerStyle = newStyle;
                ChangeKeyViewer();
                SaveSettings();
            }

            // Foot key layout selection grid / 脚键布局选择网格
            GUILayout.Label(I18n.Tr("foot_keys") + ":");
            FootKeyviewerStyle newFootStyle = (FootKeyviewerStyle)GUILayout.SelectionGrid((int)Settings.FootKeyViewerStyle,
                FootKeyLayoutNames, 5);
            if (newFootStyle != Settings.FootKeyViewerStyle)
            {
                Settings.FootKeyViewerStyle = newFootStyle;
                ResetFootKeyViewer();
                SaveSettings();
            }

            // Size slider / 大小滑块
            GUILayout.BeginHorizontal();
            GUILayout.Label(I18n.Tr("size") + ":");
            float newSettingsSize = GUILayout.HorizontalSlider(Settings.Size, 0.1f, 2f, GUILayout.Width(120));
            string sizeText = GUILayout.TextField(newSettingsSize.ToString("F2"), FloatFieldWidth(newSettingsSize.ToString("F2")));
            if (float.TryParse(sizeText, out float parsedSize))
            {
                newSettingsSize = Mathf.Clamp(parsedSize, 0.1f, 2f);
            }
            if (newSettingsSize != Settings.Size)
            {
                Settings.Size = newSettingsSize;
                if (KeyViewerSizeObject != null)
                    KeyViewerSizeObject.transform.localScale = new Vector3(Settings.Size, Settings.Size, 1);
                SaveSettings();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Hide main key count toggle / 隐藏主按键计数开关
            bool newHideCount = GUILayout.Toggle(Settings.HideMainKeyCount, I18n.Tr("hide_main_count"));
            if (newHideCount != Settings.HideMainKeyCount)
            {
                Settings.HideMainKeyCount = newHideCount;
                ResetKeyViewer();
                SaveSettings();
            }

            GUILayout.Space(10);

            // Rain effect master toggle / 雨滴效果总开关
            bool newRainEffect = GUILayout.Toggle(Settings.EnableRainEffect, I18n.Tr("rain_effect"));
            if (newRainEffect != Settings.EnableRainEffect)
            {
                Settings.EnableRainEffect = newRainEffect;
                if (!Settings.EnableRainEffect)
                    rainSystem.ClearActiveDrops(Keys);
                SaveSettings();
            }

            // Per-row rain settings / 每排雨滴设置
            if (Settings.EnableRainEffect)
            {
                GUILayout.Label(I18n.Tr("rain_rows") + ":");
                GUILayout.BeginHorizontal();
                Settings.EnableRainForRow1 = GUILayout.Toggle(Settings.EnableRainForRow1, I18n.Tr("rain_row1"));
                Settings.EnableRainForRow2 = GUILayout.Toggle(Settings.EnableRainForRow2, I18n.Tr("rain_row2"));
                if (Settings.KeyViewerStyle == KeyviewerStyle.Key20)
                    Settings.EnableRainForRow3 = GUILayout.Toggle(Settings.EnableRainForRow3, I18n.Tr("rain_row3"));
                GUILayout.EndHorizontal();

                // Per-row rain height / 每排雨滴高度
                GUILayout.Label(I18n.Tr("rain_height") + ":");
                GUILayout.BeginHorizontal();
                GUILayout.Label(I18n.Tr("rain_row1") + ":");
                Settings.RainHeightRow1 = GUILayout.HorizontalSlider(Settings.RainHeightRow1, 1f, 1000f, GUILayout.Width(120));
                string height1Text = GUILayout.TextField(Settings.RainHeightRow1.ToString("F2"), FloatFieldWidth(Settings.RainHeightRow1.ToString("F2")));
                if (float.TryParse(height1Text, out float newHeight1))
                    Settings.RainHeightRow1 = Mathf.Clamp(newHeight1, 1f, 1000f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(I18n.Tr("rain_row2") + ":");
                Settings.RainHeightRow2 = GUILayout.HorizontalSlider(Settings.RainHeightRow2, 1f, 1000f, GUILayout.Width(120));
                string height2Text = GUILayout.TextField(Settings.RainHeightRow2.ToString("F2"), FloatFieldWidth(Settings.RainHeightRow2.ToString("F2")));
                if (float.TryParse(height2Text, out float newHeight2))
                    Settings.RainHeightRow2 = Mathf.Clamp(newHeight2, 1f, 1000f);
                GUILayout.EndHorizontal();

                if (Settings.KeyViewerStyle == KeyviewerStyle.Key20)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(I18n.Tr("rain_row3") + ":");
                    Settings.RainHeightRow3 = GUILayout.HorizontalSlider(Settings.RainHeightRow3, 1f, 1000f, GUILayout.Width(120));
                    string height3Text = GUILayout.TextField(Settings.RainHeightRow3.ToString("F2"), FloatFieldWidth(Settings.RainHeightRow3.ToString("F2")));
                    if (float.TryParse(height3Text, out float newHeight3))
                        Settings.RainHeightRow3 = Mathf.Clamp(newHeight3, 1f, 1000f);
                    GUILayout.EndHorizontal();
                }

                // Per-row rain speed / 每排雨滴速度
                GUILayout.Label(I18n.Tr("rain_speed") + ":");
                GUILayout.BeginHorizontal();
                GUILayout.Label(I18n.Tr("rain_row1") + ":");
                Settings.RainSpeedRow1 = GUILayout.HorizontalSlider(Settings.RainSpeedRow1, 50f, 1000f, GUILayout.Width(120));
                string speed1Text = GUILayout.TextField(Settings.RainSpeedRow1.ToString("F0"), FloatFieldWidth(Settings.RainSpeedRow1.ToString("F0")));
                if (float.TryParse(speed1Text, out float newSpeed1))
                    Settings.RainSpeedRow1 = Mathf.Clamp(newSpeed1, 50f, 1000f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(I18n.Tr("rain_row2") + ":");
                Settings.RainSpeedRow2 = GUILayout.HorizontalSlider(Settings.RainSpeedRow2, 50f, 1000f, GUILayout.Width(120));
                string speed2Text = GUILayout.TextField(Settings.RainSpeedRow2.ToString("F0"), FloatFieldWidth(Settings.RainSpeedRow2.ToString("F0")));
                if (float.TryParse(speed2Text, out float newSpeed2))
                    Settings.RainSpeedRow2 = Mathf.Clamp(newSpeed2, 50f, 1000f);
                GUILayout.EndHorizontal();

                if (Settings.KeyViewerStyle == KeyviewerStyle.Key20)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(I18n.Tr("rain_row3") + ":");
                    Settings.RainSpeedRow3 = GUILayout.HorizontalSlider(Settings.RainSpeedRow3, 50f, 1000f, GUILayout.Width(120));
                    string speed3Text = GUILayout.TextField(Settings.RainSpeedRow3.ToString("F0"), FloatFieldWidth(Settings.RainSpeedRow3.ToString("F0")));
                    if (float.TryParse(speed3Text, out float newSpeed3))
                        Settings.RainSpeedRow3 = Mathf.Clamp(newSpeed3, 50f, 1000f);
                    GUILayout.EndHorizontal();
                }

                // Rain fade-out toggle / 雨滴松开淡出开关
                GUILayout.Space(5);
                bool newRainFade = GUILayout.Toggle(Settings.EnableRainFade, I18n.Tr("rain_fade"));
                if (newRainFade != Settings.EnableRainFade)
                {
                    Settings.EnableRainFade = newRainFade;
                    SaveSettings();
                }
            }

            GUILayout.Space(10);

            // Key rebinding section / 按键重绑定区域
            KeyChangeExpanded = GUILayout.Toggle(KeyChangeExpanded, (KeyChangeExpanded ? "\u25E2 " : "\u25B6 ") + I18n.Tr("key_change"));
            if (KeyChangeExpanded)
                DrawKeyChangeSection();

            // Custom text labels section / 自定义文本标签区域
            TextChangeExpanded = GUILayout.Toggle(TextChangeExpanded, (TextChangeExpanded ? "\u25E2 " : "\u25B6 ") + I18n.Tr("text_change"));
            if (TextChangeExpanded)
                DrawTextChangeSection();

            // Color settings section / 颜色设置区域
            bool colorsExpanded = GUILayout.Toggle(ColorExpanded != null, (ColorExpanded != null ? "\u25E2 " : "\u25B6 ") + I18n.Tr("colors"));
            if (colorsExpanded && ColorExpanded == null) ColorExpanded = new bool[9];
            if (!colorsExpanded) ColorExpanded = null;
            if (ColorExpanded != null)
            {
                bool pk = GUILayout.Toggle(Settings.EnablePerKeyColors, I18n.Tr("per_key_colors"));
                if (pk != Settings.EnablePerKeyColors)
                {
                    Settings.EnablePerKeyColors = pk;
                    ResetKeyViewer();
                    UpdateAllKeyColors();
                    SaveSettings();
                }
                if (Settings.EnablePerKeyColors)
                    DrawPerKeyColorSettings();
                else
                    DrawColorSettings();
            }

            GUILayout.EndVertical();
        }

        /// <summary>
        /// Draw the key rebinding section / 绘制按键重绑定区域
        /// Shows all keys for the current layout as clickable buttons / 将当前布局的所有按键显示为可点击的按钮
        /// </summary>
        private void DrawKeyChangeSection()
        {
            GUILayout.BeginVertical("box");
            KeyCode[] keyCodes = GetKeyCode();

            GUILayout.Label(I18n.Tr("row1_keys") + ":");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 8; i++)
            {
                if (GUILayout.Button(KeyToString(keyCodes[i])))
                {
                    SelectedKey = i;
                    TextChanged = false;
                }
            }
            GUILayout.EndHorizontal();

            byte[] backSequence = GetBackSequence();
            if (backSequence.Length > 0)
            {
                GUILayout.Label(I18n.Tr("row2_keys") + ":");
                GUILayout.BeginHorizontal();
                for (int i = 0; i < backSequence.Length && i < 8; i++)
                {
                    if (GUILayout.Button(KeyToString(keyCodes[backSequence[i]])))
                    {
                        SelectedKey = backSequence[i];
                        TextChanged = false;
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (Settings.KeyViewerStyle == KeyviewerStyle.Key20)
            {
                GUILayout.Label(I18n.Tr("row3_keys") + ":");
                GUILayout.BeginHorizontal();
                for (int i = 16; i < 20; i++)
                {
                    if (i < keyCodes.Length)
                    {
                        if (GUILayout.Button(KeyToString(keyCodes[i])))
                        {
                            SelectedKey = i;
                            TextChanged = false;
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }

            // Foot key section / 脚键区域
            KeyCode[] footKeyCodes = GetFootKeyCode();
            if (footKeyCodes != null && footKeyCodes.Length > 0)
            {
                GUILayout.Label(I18n.Tr("foot_keys_list") + ":");
                if (footKeyCodes.Length <= 8)
                {
                    GUILayout.BeginHorizontal();
                    for (int i = 0; i < footKeyCodes.Length; i++)
                    {
                        if (GUILayout.Button(KeyToString(footKeyCodes[i])))
                        {
                            SelectedKey = i + 20;
                            TextChanged = false;
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    for (int i = 0; i < 8; i++)
                    {
                        if (GUILayout.Button(KeyToString(footKeyCodes[i])))
                        {
                            SelectedKey = i + 20;
                            TextChanged = false;
                        }
                    }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
                    int remaining = footKeyCodes.Length - 8;
                    for (int s = 0; s < 8 - remaining; s++)
                        GUILayout.FlexibleSpace();
                    for (int i = 8; i < footKeyCodes.Length; i++)
                    {
                        if (GUILayout.Button(KeyToString(footKeyCodes[i])))
                        {
                            SelectedKey = i + 20;
                            TextChanged = false;
                        }
                    }
                    for (int s = 0; s < 8 - remaining; s++)
                        GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            }

            if (SelectedKey != -1 && !TextChanged)
                GUILayout.Label("<b>" + I18n.Tr("press_new_key") + "</b>");
            GUILayout.EndVertical();
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

            GUILayout.Label(I18n.Tr("row1_text") + ":");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 8; i++)
            {
                string buttonText = !string.IsNullOrEmpty(keyTexts[i]) ? keyTexts[i] : KeyToString(keyCodes[i]);
                if (GUILayout.Button(buttonText))
                {
                    SelectedKey = i;
                    TextChanged = true;
                }
            }
            GUILayout.EndHorizontal();

            byte[] backSequence = GetBackSequence();
            if (backSequence.Length > 0)
            {
                GUILayout.Label(I18n.Tr("row2_text") + ":");
                GUILayout.BeginHorizontal();
                for (int i = 0; i < backSequence.Length && i < 8; i++)
                {
                    int keyIndex = backSequence[i];
                    string buttonText = !string.IsNullOrEmpty(keyTexts[keyIndex]) ? keyTexts[keyIndex] : KeyToString(keyCodes[keyIndex]);
                    if (GUILayout.Button(buttonText))
                    {
                        SelectedKey = keyIndex;
                        TextChanged = true;
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (Settings.KeyViewerStyle == KeyviewerStyle.Key20)
            {
                GUILayout.Label(I18n.Tr("row3_text") + ":");
                GUILayout.BeginHorizontal();
                for (int i = 16; i < 20; i++)
                {
                    if (i < keyTexts.Length)
                    {
                        string buttonText = !string.IsNullOrEmpty(keyTexts[i]) ? keyTexts[i] : KeyToString(keyCodes[i]);
                        if (GUILayout.Button(buttonText))
                        {
                            SelectedKey = i;
                            TextChanged = true;
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }

            // Foot key text labels / 脚键文本标签
            KeyCode[] footKeyCodes = GetFootKeyCode();
            string[] footKeyTexts = GetFootKeyText();
            if (footKeyCodes != null && footKeyCodes.Length > 0)
            {
                GUILayout.Label(I18n.Tr("foot_keys_text") + ":");
                if (footKeyCodes.Length <= 8)
                {
                    GUILayout.BeginHorizontal();
                    for (int i = 0; i < footKeyCodes.Length; i++)
                    {
                        string buttonText = !string.IsNullOrEmpty(footKeyTexts[i]) ? footKeyTexts[i] : KeyToString(footKeyCodes[i]);
                        if (GUILayout.Button(buttonText))
                        {
                            SelectedKey = i + 20;
                            TextChanged = true;
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    for (int i = 0; i < 8; i++)
                    {
                        string buttonText = !string.IsNullOrEmpty(footKeyTexts[i]) ? footKeyTexts[i] : KeyToString(footKeyCodes[i]);
                        if (GUILayout.Button(buttonText))
                        {
                            SelectedKey = i + 20;
                            TextChanged = true;
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    int remaining = footKeyCodes.Length - 8;
                    for (int s = 0; s < 8 - remaining; s++)
                        GUILayout.FlexibleSpace();
                    for (int i = 8; i < footKeyCodes.Length; i++)
                    {
                        string buttonText = !string.IsNullOrEmpty(footKeyTexts[i]) ? footKeyTexts[i] : KeyToString(footKeyCodes[i]);
                        if (GUILayout.Button(buttonText))
                        {
                            SelectedKey = i + 20;
                            TextChanged = true;
                        }
                    }
                    for (int s = 0; s < 8 - remaining; s++)
                        GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            }

            if (SelectedKey != -1 && TextChanged)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(I18n.Tr("input_text") + ":");
                if (SelectedKey < 20)
                {
                    string currentText = !string.IsNullOrEmpty(keyTexts[SelectedKey]) ? keyTexts[SelectedKey] : KeyToString(keyCodes[SelectedKey]);
                    string newText = GUILayout.TextField(currentText, GUILayout.Width(150));
                    if (keyTexts[SelectedKey] != newText)
                    {
                        if (Keys != null && SelectedKey < Keys.Length && Keys[SelectedKey] != null)
                            Keys[SelectedKey].text.text = newText;
                        keyTexts[SelectedKey] = string.IsNullOrEmpty(newText) || newText == KeyToString(keyCodes[SelectedKey]) ? null : newText;
                    }
                }
                else
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
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Draw the color settings section / 绘制颜色设置区域
        /// RGB-A sliders with preview and reset buttons for each color / 每个颜色的 R/G/B/A 滑块、预览和重置按钮
        /// </summary>
        private void DrawColorSettings()
        {
            GUILayout.BeginVertical("box");
            string[] colorNames = {
                I18n.Tr("color_bg"), I18n.Tr("color_bg_clicked"), I18n.Tr("color_outline"), I18n.Tr("color_outline_clicked"),
                I18n.Tr("color_text"), I18n.Tr("color_text_clicked"),
                I18n.Tr("color_rain1"), I18n.Tr("color_rain2"), I18n.Tr("color_rain3")
            };
            Color[] defaultColors = {
                Background, BackgroundClicked, Outline, OutlineClicked,
                Text, TextClicked,
                RainColor, RainColor2, RainColor3
            };
            for (int i = 0; i < 9; i++)
            {
                if (i >= 6 && !Settings.EnableRainEffect)
                    continue;
                ColorExpanded[i] = GUILayout.Toggle(ColorExpanded[i], ColorExpanded[i] ? $"\u25E2 {colorNames[i]}" : $"\u25B6 {colorNames[i]}");
                if (ColorExpanded[i])
                {
                    GUILayout.BeginVertical("box");
                    Color currentColor = GetColorByIndex(i);
                    Color newColor = DrawColorPicker(colorNames[i], currentColor, defaultColors[i]);
                    if (newColor != currentColor)
                    {
                        SetColorByIndex(i, newColor);
                        UpdateAllKeyColors();
                        SaveSettings();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Draw an individual color picker with R/G/B/A sliders and preview / 绘制单个颜色选择器，包含 R/G/B/A 滑块和预览
        /// </summary>
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

            bool pressed;
            void KeyBtn(int idx, string label)
            {
                Color c = Settings.PerKeyBackground[idx];
                var style = new GUIStyle(GUI.skin.button);
                style.normal.textColor = c.grayscale > 0.5f ? Color.black : Color.white;
                if (perKeyColorSelected == idx)
                    GUI.backgroundColor = Color.Lerp(c, Color.white, 0.4f);
                else
                    GUI.backgroundColor = c;
                pressed = GUILayout.Button(label, style);
                GUI.backgroundColor = Color.white;
                if (pressed)
                {
                    if (perKeyColorSelected != idx) perKeyColorTypeIndex = -1;
                    perKeyColorSelected = perKeyColorSelected == idx ? -1 : idx;
                }
            }

            GUILayout.Label(I18n.Tr("row1_keys") + ":");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 8; i++) KeyBtn(i, KeyToString(keyCodes[i]));
            GUILayout.EndHorizontal();

            byte[] backSequence = GetBackSequence();
            if (backSequence.Length > 0)
            {
                GUILayout.Label(I18n.Tr("row2_keys") + ":");
                GUILayout.BeginHorizontal();
                for (int b = 0; b < backSequence.Length && b < 8; b++)
                    KeyBtn(backSequence[b], KeyToString(keyCodes[backSequence[b]]));
                GUILayout.EndHorizontal();
            }

            if (Settings.KeyViewerStyle == KeyviewerStyle.Key20)
            {
                GUILayout.Label(I18n.Tr("row3_keys") + ":");
                GUILayout.BeginHorizontal();
                for (int i = 16; i < 20 && i < keyCodes.Length; i++)
                    KeyBtn(i, KeyToString(keyCodes[i]));
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
                        KeyBtn(20 + f, KeyToString(footKeyCodes[f]));
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            KeyBtn(36, "KPS");
            KeyBtn(37, "Total");
            GUILayout.EndHorizontal();

            if (perKeyColorSelected >= 0 && perKeyColorSelected < 38)
            {
                GUILayout.Space(5);
                int s = perKeyColorSelected;
                string keyLabel = s == 36 ? "KPS" : s == 37 ? "Total" : KeyToString(GetKeyCodeForIndex(s));
                GUILayout.Label("Key " + s + " (" + keyLabel + ")");
                string rainKey = s < 8 ? "color_rain1" : s < 16 ? "color_rain2" : s < 20 ? "color_rain3" : "color_rain1";

                string[] typeNames = {
                    I18n.Tr("color_bg"), I18n.Tr("color_bg_clicked"),
                    I18n.Tr("color_outline"), I18n.Tr("color_outline_clicked"),
                    I18n.Tr("color_text"), I18n.Tr("color_text_clicked"),
                    I18n.Tr(rainKey) + " (" + s + ")"
                };
                Color[] values = {
                    Settings.PerKeyBackground[s], Settings.PerKeyBackgroundClicked[s],
                    Settings.PerKeyOutline[s], Settings.PerKeyOutlineClicked[s],
                    Settings.PerKeyText[s], Settings.PerKeyTextClicked[s],
                    Settings.PerKeyRainColor[s]
                };
                Color[] defaults = {
                    Background, BackgroundClicked, Outline, OutlineClicked, Text, TextClicked, RainColor
                };

                int[] typeOrder = s >= 36 ? new int[] { 0, 2, 4 }
                    : s >= 20 ? new int[] { 0, 1, 2, 3, 4, 5 }
                    : new int[] { 0, 1, 2, 3, 4, 5, 6 };
                int typeCount = typeOrder.Length;

                for (int ti = 0; ti < typeCount; ti++)
                {
                    int t = typeOrder[ti];
                    bool expanded = GUILayout.Toggle(perKeyColorTypeIndex == t,
                        (perKeyColorTypeIndex == t ? "\u25E2 " : "\u25B6 ") + typeNames[t]);
                    if (expanded != (perKeyColorTypeIndex == t))
                        perKeyColorTypeIndex = expanded ? t : -1;

                    if (perKeyColorTypeIndex == t)
                    {
                        GUILayout.BeginVertical("box");
                        Color cur = values[t];
                        Color newColor = DrawColorPicker(typeNames[t], cur, defaults[t]);
                        if (newColor != cur)
                        {
                            switch (t)
                            {
                                case 0: Settings.PerKeyBackground[s] = newColor; break;
                                case 1: Settings.PerKeyBackgroundClicked[s] = newColor; break;
                                case 2: Settings.PerKeyOutline[s] = newColor; break;
                                case 3: Settings.PerKeyOutlineClicked[s] = newColor; break;
                                case 4: Settings.PerKeyText[s] = newColor; break;
                                case 5: Settings.PerKeyTextClicked[s] = newColor; break;
                                case 6: Settings.PerKeyRainColor[s] = newColor; break;
                            }
                            UpdateAllKeyColors();
                            SaveSettings();
                        }
                        GUILayout.EndVertical();
                    }
                }
            }

            if (GUILayout.Button(I18n.Tr("per_key_color_reset")))
            {
                Settings.InitPerKeyColors();
                UpdateAllKeyColors();
                SaveSettings();
            }

            if (GUILayout.Button(I18n.Tr("auto_rainbow")))
            {
                AutoAssignRainbowColors();
            }

            GUILayout.EndVertical();
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
    }
}
