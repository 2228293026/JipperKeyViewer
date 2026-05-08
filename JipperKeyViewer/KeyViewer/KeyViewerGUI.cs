using TMPro;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    public partial class KeyViewer : MonoBehaviour
    {
        public void DrawSettingsWindow()
        {
            ScanGameFonts();
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            string langNow = I18n.Tr("language") + ": " + (Settings.Language == "en" ? "English" : "\u4E2D\u6587");
            if (GUILayout.Button(langNow))
            {
                Settings.Language = Settings.Language == "en" ? "zh" : "en";
                I18n.Lang = Settings.Language;
                SaveSettings();
            }
            GUILayout.EndHorizontal();

            bool newEnabled = GUILayout.Toggle(Settings.Enabled, (Settings.Enabled ? "\u2713 " : "\u2717 ") + I18n.Tr("key_display_on"));
            if (newEnabled != Settings.Enabled)
            {
                Settings.Enabled = newEnabled;
                SaveSettings();
            }
            GUILayout.Label(I18n.Tr("font_style") + ":");
            string curFont = fontList.Count > 0 ? fontList[Settings.FontIndex].name : "\u65E0";
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
                GUILayout.EndVertical();
            }
            bool newDownLocation = GUILayout.Toggle(Settings.DownLocation, I18n.Tr("place_below"));
            if (newDownLocation != Settings.DownLocation)
            {
                Settings.DownLocation = newDownLocation;
                ResetKeyViewer();
                ResetFootKeyViewer();
                SaveSettings();
            }
            GUILayout.Space(10);
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
            GUILayout.Label(I18n.Tr("key_layout") + ":");
            KeyviewerStyle newStyle = (KeyviewerStyle)GUILayout.SelectionGrid((int)Settings.KeyViewerStyle,
                KeyLayoutNames, 3);
            if (newStyle != Settings.KeyViewerStyle)
            {
                Settings.KeyViewerStyle = newStyle;
                ChangeKeyViewer();
                SaveSettings();
            }
            GUILayout.Label(I18n.Tr("foot_keys") + ":");
            FootKeyviewerStyle newFootStyle = (FootKeyviewerStyle)GUILayout.SelectionGrid((int)Settings.FootKeyViewerStyle,
                FootKeyLayoutNames, 5);
            if (newFootStyle != Settings.FootKeyViewerStyle)
            {
                Settings.FootKeyViewerStyle = newFootStyle;
                ResetFootKeyViewer();
                SaveSettings();
            }
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
            GUILayout.Space(10);
            bool newRainEffect = GUILayout.Toggle(Settings.EnableRainEffect, I18n.Tr("rain_effect"));
            if (newRainEffect != Settings.EnableRainEffect)
            {
                Settings.EnableRainEffect = newRainEffect;
                if (!Settings.EnableRainEffect)
                {
                    ClearAllRainDrops();
                }
                SaveSettings();
            }

            if (Settings.EnableRainEffect)
            {
                GUILayout.Label(I18n.Tr("rain_rows") + ":");
                GUILayout.BeginHorizontal();
                Settings.EnableRainForRow1 = GUILayout.Toggle(Settings.EnableRainForRow1, I18n.Tr("rain_row1"));
                Settings.EnableRainForRow2 = GUILayout.Toggle(Settings.EnableRainForRow2, I18n.Tr("rain_row2"));
                if (Settings.KeyViewerStyle == KeyviewerStyle.Key20)
                {
                    Settings.EnableRainForRow3 = GUILayout.Toggle(Settings.EnableRainForRow3, I18n.Tr("rain_row3"));
                }
                GUILayout.EndHorizontal();
                GUILayout.Label(I18n.Tr("rain_height") + ":");

                GUILayout.BeginHorizontal();
                GUILayout.Label(I18n.Tr("rain_row1") + ":");
                Settings.RainHeightRow1 = GUILayout.HorizontalSlider(Settings.RainHeightRow1, 1f, 1000f, GUILayout.Width(120));
                string height1Text = GUILayout.TextField(Settings.RainHeightRow1.ToString("F2"), FloatFieldWidth(Settings.RainHeightRow1.ToString("F2")));
                if (float.TryParse(height1Text, out float newHeight1))
                {
                    Settings.RainHeightRow1 = newHeight1;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(I18n.Tr("rain_row2") + ":");
                Settings.RainHeightRow2 = GUILayout.HorizontalSlider(Settings.RainHeightRow2, 1f, 1000f, GUILayout.Width(120));
                string height2Text = GUILayout.TextField(Settings.RainHeightRow2.ToString("F2"), FloatFieldWidth(Settings.RainHeightRow2.ToString("F2")));
                if (float.TryParse(height2Text, out float newHeight2))
                {
                    Settings.RainHeightRow2 = newHeight2;
                }
                GUILayout.EndHorizontal();

                if (Settings.KeyViewerStyle == KeyviewerStyle.Key20)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(I18n.Tr("rain_row3") + ":");
                    Settings.RainHeightRow3 = GUILayout.HorizontalSlider(Settings.RainHeightRow3, 1f, 1000f, GUILayout.Width(120));
                    string height3Text = GUILayout.TextField(Settings.RainHeightRow3.ToString("F2"), FloatFieldWidth(Settings.RainHeightRow3.ToString("F2")));
                    if (float.TryParse(height3Text, out float newHeight3))
                    {
                        Settings.RainHeightRow3 = newHeight3;
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.Label(I18n.Tr("rain_speed") + ":");
                GUILayout.BeginHorizontal();
                GUILayout.Label(I18n.Tr("rain_row1") + ":");
                Settings.RainSpeedRow1 = GUILayout.HorizontalSlider(Settings.RainSpeedRow1, 50f, 1000f, GUILayout.Width(120));
                string speed1Text = GUILayout.TextField(Settings.RainSpeedRow1.ToString("F0"), FloatFieldWidth(Settings.RainSpeedRow1.ToString("F0")));
                if (float.TryParse(speed1Text, out float newSpeed1))
                {
                    Settings.RainSpeedRow1 = Mathf.Clamp(newSpeed1, 50f, 1000f);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(I18n.Tr("rain_row2") + ":");
                Settings.RainSpeedRow2 = GUILayout.HorizontalSlider(Settings.RainSpeedRow2, 50f, 1000f, GUILayout.Width(120));
                string speed2Text = GUILayout.TextField(Settings.RainSpeedRow2.ToString("F0"), FloatFieldWidth(Settings.RainSpeedRow2.ToString("F0")));
                if (float.TryParse(speed2Text, out float newSpeed2))
                {
                    Settings.RainSpeedRow2 = Mathf.Clamp(newSpeed2, 50f, 1000f);
                }
                GUILayout.EndHorizontal();

                if (Settings.KeyViewerStyle == KeyviewerStyle.Key20)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(I18n.Tr("rain_row3") + ":");
                    Settings.RainSpeedRow3 = GUILayout.HorizontalSlider(Settings.RainSpeedRow3, 50f, 1000f, GUILayout.Width(120));
                    string speed3Text = GUILayout.TextField(Settings.RainSpeedRow3.ToString("F0"), FloatFieldWidth(Settings.RainSpeedRow3.ToString("F0")));
                    if (float.TryParse(speed3Text, out float newSpeed3))
                    {
                        Settings.RainSpeedRow3 = Mathf.Clamp(newSpeed3, 50f, 1000f);
                    }
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(10);
            KeyChangeExpanded = GUILayout.Toggle(KeyChangeExpanded, (KeyChangeExpanded ? "\u25E2 " : "\u25B6 ") + I18n.Tr("key_change"));
            if (KeyChangeExpanded)
            {
                DrawKeyChangeSection();
            }
            TextChangeExpanded = GUILayout.Toggle(TextChangeExpanded, (TextChangeExpanded ? "\u25E2 " : "\u25B6 ") + I18n.Tr("text_change"));
            if (TextChangeExpanded)
            {
                DrawTextChangeSection();
            }
            bool colorsExpanded = GUILayout.Toggle(ColorExpanded != null, (ColorExpanded != null ? "\u25E2 " : "\u25B6 ") + I18n.Tr("colors"));
            if (colorsExpanded && ColorExpanded == null) ColorExpanded = new bool[9];
            if (!colorsExpanded) ColorExpanded = null;
            if (ColorExpanded != null)
            {
                DrawColorSettings();
            }
            GUILayout.EndVertical();
        }

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
                    StartKeySelection();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(I18n.Tr("row2_keys") + ":");
            byte[] backSequence = GetBackSequence();
            GUILayout.BeginHorizontal();
            for (int i = 0; i < backSequence.Length && i < 8; i++)
            {
                if (GUILayout.Button(KeyToString(keyCodes[backSequence[i]])))
                {
                    SelectedKey = backSequence[i];
                    TextChanged = false;
                    StartKeySelection();
                }
            }
            GUILayout.EndHorizontal();
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
                            StartKeySelection();
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
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
                            StartKeySelection();
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
                            StartKeySelection();
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
                            StartKeySelection();
                        }
                    }
                    for (int s = 0; s < 8 - remaining; s++)
                        GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            }
            if (SelectedKey != -1 && !TextChanged)
            {
                GUILayout.Label("<b>" + I18n.Tr("press_new_key") + "</b>");
            }
            GUILayout.EndVertical();
        }

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
            GUILayout.Label(I18n.Tr("row2_text") + ":");
            byte[] backSequence = GetBackSequence();
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
            if (SelectedKey != -1 && TextChanged)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(I18n.Tr("input_text") + ":");
                string currentText = !string.IsNullOrEmpty(keyTexts[SelectedKey]) ? keyTexts[SelectedKey] : KeyToString(keyCodes[SelectedKey]);
                string newText = GUILayout.TextField(currentText, GUILayout.Width(150));
                if (keyTexts[SelectedKey] != newText)
                {
                    if (Keys != null && SelectedKey < Keys.Length && Keys[SelectedKey] != null)
                    {
                        Keys[SelectedKey].text.text = newText;
                    }
                    keyTexts[SelectedKey] = string.IsNullOrEmpty(newText) || newText == KeyToString(keyCodes[SelectedKey]) ? null : newText;
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(I18n.Tr("reset")))
                {
                    keyTexts[SelectedKey] = null;
                    if (Keys != null && SelectedKey < Keys.Length && Keys[SelectedKey] != null)
                    {
                        Keys[SelectedKey].text.text = KeyToString(keyCodes[SelectedKey]);
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

        private Color DrawColorPicker(string label, Color currentColor, Color defaultColor)
        {
            GUILayout.BeginVertical();
            GUILayout.Label(label);
            GUILayout.BeginHorizontal();
            GUILayout.Label("R:", GUILayout.Width(20));
            currentColor.r = GUILayout.HorizontalSlider(currentColor.r, 0f, 1f, GUILayout.Width(150));
            GUILayout.Label(currentColor.r.ToString("F2"), GUILayout.Width(30));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("G:", GUILayout.Width(20));
            currentColor.g = GUILayout.HorizontalSlider(currentColor.g, 0f, 1f, GUILayout.Width(150));
            GUILayout.Label(currentColor.g.ToString("F2"), GUILayout.Width(30));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("B:", GUILayout.Width(20));
            currentColor.b = GUILayout.HorizontalSlider(currentColor.b, 0f, 1f, GUILayout.Width(150));
            GUILayout.Label(currentColor.b.ToString("F2"), GUILayout.Width(30));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("A:", GUILayout.Width(20));
            currentColor.a = GUILayout.HorizontalSlider(currentColor.a, 0f, 1f, GUILayout.Width(150));
            GUILayout.Label(currentColor.a.ToString("F2"), GUILayout.Width(30));
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
    }
}
