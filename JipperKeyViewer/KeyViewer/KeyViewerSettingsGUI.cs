// Settings GUI: General / Layout / Display tab content / 设置界面:常规 / 布局 / 显示 标签页内容
// Profile, language, count reset, font, folder buttons, custom position, layout, display, full-keyboard KPS/Total layout / 配置、语言、计数重置、字体、文件夹、自定义位置、布局、显示、全键盘 KPS/Total 布局
// Shared small draw helpers (FloatSliderField, foldout buttons) also live here / 通用小绘制工具(FloatSliderField、折叠按钮)也放在此文件

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
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
            if (GUILayout.Button((profileExpanded ? "◢ " : "▶ ") + label, GUILayout.MinWidth(200)))
                profileExpanded = !profileExpanded;
        }

        private void DrawProfileList()
        {
            if (Settings.ProfileNames == null) return;
            for (int i = 0; i < Settings.ProfileNames.Length; i++)
            {
                string p = Settings.ProfileNames[i];
                bool selected = p == Settings.CurrentProfile;
                if (GUILayout.Button((selected ? "✓ " : "  ") + p, GUILayout.MinWidth(200)))
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
            if (GUILayout.Button("✓", GUILayout.Width(24)))
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
            if (GUILayout.Button("✗", GUILayout.Width(24)))
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

        /// <summary>
        /// Count display options (the enable toggle and reset-count button moved to the header bar) / 计数显示选项(总开关与重置计数按钮已移到顶部常驻栏)
        /// </summary>
        private void DrawCountResetSection()
        {
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
            if (Kps != null) SetKpsTotalDisplay(Kps, "KPS", "0");
            if (Total != null) SetKpsTotalDisplay(Total, "Total", "0");
            SaveSettings();
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

        private void DrawFontSection()
        {
            GUILayout.Label(I18n.Tr("font_style") + ":");
            string curFont = fontList.Count > 0 ? fontList[Mathf.Clamp(Settings.Data.FontIndex, 0, fontList.Count - 1)].name : "None";
            if (GUILayout.Button((fontListExpanded ? "◢ " : "▶ ") + curFont, GUILayout.MinWidth(200)))
                fontListExpanded = !fontListExpanded;
            if (fontListExpanded)
            {
                if (fontList.Count > 0)
                {
                    int newIdx = Settings.Data.FontIndex;
                    for (int i = 0; i < fontList.Count; i++)
                    {
                        bool selected = i == Settings.Data.FontIndex;
                        if (GUILayout.Button((selected ? "✓ " : "  ") + fontList[i].name, GUILayout.MinWidth(200)))
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
                    GUILayout.Label("▶ " + I18n.Tr("no_fonts_found"), GUILayout.MinWidth(200));

                GUILayout.Space(5);
                GUILayout.BeginVertical("box");
                GUILayout.Label(I18n.Tr("custom_font_tip"));
                GUILayout.Label($"CustomFont : {Path.Combine(Loader.ModPath, "CustomFont")}");
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
                string modPath = Loader.ModPath;
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
                if (KeyViewer.IsFullKeyboard)
                {
                    GUILayout.Label(I18n.Tr("main_key_pos") + ":");
                    Vector2 tempMainPos = Settings.Data.MainKeyViewerPosition;
                    bool positionChanged = false;
                    float newMainX = FloatSliderField("X", tempMainPos.x, 0f, 1f);
                    if (newMainX != tempMainPos.x) { tempMainPos.x = newMainX; positionChanged = true; }
                    float newMainY = FloatSliderField("Y", tempMainPos.y, 0f, 1f);
                    if (newMainY != tempMainPos.y) { tempMainPos.y = newMainY; positionChanged = true; }
                    if (positionChanged)
                    {
                        Settings.Data.MainKeyViewerPosition = tempMainPos;
                        ResetKeyViewerPosition();
                        SaveSettings();
                    }
                    if (GUILayout.Button(I18n.Tr("reset_pos")))
                    {
                        Settings.Data.MainKeyViewerPosition = new Vector2(0, 1);
                        ResetKeyViewerPosition();
                        SaveSettings();
                    }
                }
                else
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

            if (!KeyViewer.IsFullKeyboard)
            {
                // Standard key width toggle: only show for layouts with mixed-width back rows
                // 标准按键宽度开关：仅在有宽窄键混排的布局显示
                bool hasNonStandardWidth = Settings.Data.KeyViewerStyle switch
                {
                    KeyviewerStyle.Key10 or KeyviewerStyle.Key12 or KeyviewerStyle.Key20 => true,
                    _ => false
                };
                if (hasNonStandardWidth)
                {
                    bool newStdWidth = GUILayout.Toggle(Settings.Data.StandardKeyWidth, I18n.Tr("standard_key_width"));
                    if (newStdWidth != Settings.Data.StandardKeyWidth)
                    {
                        Settings.Data.StandardKeyWidth = newStdWidth;
                        ChangeKeyViewer();
                        SaveSettings();
                    }
                }

                GUILayout.Label(I18n.Tr("foot_keys") + ":");
                FootKeyviewerStyle newFootStyle = (FootKeyviewerStyle)GUILayout.SelectionGrid((int)Settings.Data.FootKeyViewerStyle, FootKeyLayoutNames, 5);
                if (newFootStyle != Settings.Data.FootKeyViewerStyle)
                {
                    Settings.Data.FootKeyViewerStyle = newFootStyle;
                    ResetFootKeyViewer();
                    SaveSettings();
                }
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
            // The main-count / per-key-KPS toggles only apply to the normal (non-full-keyboard) layouts;
            // the full 108-key view shows key labels and has its own KPS/Total controls. / 主区域计数与每键KPS 开关仅对普通布局生效；全键盘显示键位字母、并有独立的 KPS/Total 控制，故全键盘下隐藏。
            if (!KeyViewer.IsFullKeyboard)
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
            }

            // Streamer mode (hides KPS/Total) only applies to the normal layouts; the full keyboard has
            // its own dedicated "Show KPS / Total" toggle, so don't show this redundant one there.
            // 主播模式（隐藏 KPS/Total）仅对普通布局生效；全键盘已有专属的「显示 KPS/Total」开关，故不再显示这个重复的。
            if (!KeyViewer.IsFullKeyboard)
            {
                bool newStreamer = GUILayout.Toggle(Settings.Data.StreamerMode, I18n.Tr("streamer_mode"));
                if (newStreamer != Settings.Data.StreamerMode)
                {
                    Settings.Data.StreamerMode = newStreamer;
                    if (Kps != null) Kps.gameObject.SetActive(!newStreamer);
                    if (Total != null) Total.gameObject.SetActive(!newStreamer);
                    SaveSettings();
                }
            }

            float newFontSize = FloatSliderField(I18n.Tr("key_font_size"), Settings.Data.KeyFontSize, 8f, 72f, "F0");
            if (newFontSize != Settings.Data.KeyFontSize)
            {
                Settings.Data.KeyFontSize = newFontSize;
                UpdateAllFonts();
                SaveSettings();
            }

            // Center KPS / Total text — only for flat (slim) KPS/Total designs (e.g. full keyboard, 8K/14K/16K/24K).
            // Hidden for stacked (non-slim) layouts like 12K/10K/20K standard. / 仅对扁平（slim）KPS/Total 生效（全键盘及 8K/14K/16K/24K 等），堆叠布局（12K/10K/20K 标准）隐藏。
            if (KeyViewer.KpsTotalIsSlim())
            {
                bool newCenterKt = GUILayout.Toggle(Settings.Data.KpsTotalCentered, I18n.Tr("fk_kps_total_centered"));
                if (newCenterKt != Settings.Data.KpsTotalCentered)
                {
                    Settings.Data.KpsTotalCentered = newCenterKt;
                    ChangeKeyViewer();
                    SaveSettings();
                }
                // Stack KPS/Total text vertically — only available when centered is enabled
                // KPS/Total 上下堆叠 — 仅在居中开启时可用
                if (Settings.Data.KpsTotalCentered)
                {
                    bool newStacked = GUILayout.Toggle(Settings.Data.KpsTotalStackedWhenCentered, I18n.Tr("kps_total_stacked"));
                    if (newStacked != Settings.Data.KpsTotalStackedWhenCentered)
                    {
                        Settings.Data.KpsTotalStackedWhenCentered = newStacked;
                        ChangeKeyViewer();
                        SaveSettings();
                    }
                }
            }

            bool newPressAnim = GUILayout.Toggle(Settings.Data.EnablePressAnimation, I18n.Tr("press_animation"));
            if (newPressAnim != Settings.Data.EnablePressAnimation)
            {
                Settings.Data.EnablePressAnimation = newPressAnim;
                SaveSettings();
            }

            if (Settings.Data.EnablePressAnimation)
            {
                float newScale = FloatSliderField(I18n.Tr("press_anim_scale"), Settings.Data.PressAnimationScale, 0.5f, 0.95f);
                if (newScale != Settings.Data.PressAnimationScale)
                {
                    Settings.Data.PressAnimationScale = newScale;
                    SaveSettings();
                }

                bool newRainAnim = GUILayout.Toggle(Settings.Data.EnablePressAnimationOnRain, I18n.Tr("press_anim_rain"));
                if (newRainAnim != Settings.Data.EnablePressAnimationOnRain)
                {
                    Settings.Data.EnablePressAnimationOnRain = newRainAnim;
                    SaveSettings();
                }
            }
        }

        // ===== Full-keyboard KPS / Total section foldout state =====
        bool KpsTotalExpanded = false;

        // ======================== Full Keyboard (108K) KPS / Total layout section ========================
        private void DrawFullKeyboardKpsTotalSection()
        {
            KpsTotalExpanded = DrawFoldoutButton(I18n.Tr("fk_kps_total"), KpsTotalExpanded);
            if (!KpsTotalExpanded) return;
            GUILayout.BeginVertical("box");
            // Show / hide toggle (single source of truth for visibility). / 显示开关（可见性的唯一控制入口）。
            bool showKt = GUILayout.Toggle(Settings.Data.FullKeyboardShowKpsTotal, I18n.Tr("fk_show_kps_total"));
            if (showKt != Settings.Data.FullKeyboardShowKpsTotal)
            {
                Settings.Data.FullKeyboardShowKpsTotal = showKt;
                ChangeKeyViewer();
                SaveSettings();
            }
            // KPS / Total box size (px) — layout property, kept here not in the color section. / KPS/Total 框尺寸（像素），属布局属性，置于此而非颜色栏目。
            float newKtSize = FloatSliderField(I18n.Tr("fk_kps_total_size"), Settings.Data.FullKeyboardKpsTotalSize, 40f, 400f, "F0");
            if (newKtSize != Settings.Data.FullKeyboardKpsTotalSize)
            {
                Settings.Data.FullKeyboardKpsTotalSize = newKtSize;
                ChangeKeyViewer();
                SaveSettings();
            }
            if (Settings.Data.FullKeyboardShowKpsTotal)
            {
                // KPS / Total custom position (normalized 0-1) / KPS/Total 自定义位置（归一化 0-1）
                GUILayout.Space(5);
                GUILayout.Label(I18n.Tr("fk_kps_pos") + ":");
                Vector2 kpsPos = Settings.Data.FullKpsPosition;
                float kpsX = FloatSliderField("X", kpsPos.x, 0f, 1f);
                float kpsY = FloatSliderField("Y", kpsPos.y, 0f, 1f);
                if (kpsX != kpsPos.x || kpsY != kpsPos.y)
                {
                    Settings.Data.FullKpsPosition = new Vector2(kpsX, kpsY);
                    ApplyFullKeyboardKpsTotalPosition();
                    SaveSettings();
                }
                GUILayout.Label(I18n.Tr("fk_total_pos") + ":");
                Vector2 totalPos = Settings.Data.FullTotalPosition;
                float totalX = FloatSliderField("X", totalPos.x, 0f, 1f);
                float totalY = FloatSliderField("Y", totalPos.y, 0f, 1f);
                if (totalX != totalPos.x || totalY != totalPos.y)
                {
                    Settings.Data.FullTotalPosition = new Vector2(totalX, totalY);
                    ApplyFullKeyboardKpsTotalPosition();
                    SaveSettings();
                }
            }
            GUILayout.EndVertical();
        }
    }
}