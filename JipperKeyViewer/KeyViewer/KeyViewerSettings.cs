// Settings data model and serialization helpers / 设置数据模型和序列化辅助类
// All user-configurable options are stored here and persisted as JSON / 所有用户可配置选项都存储在这里并序列化为 JSON

using TMPro;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    [System.Serializable]
    public class ProfileData
    {
        public KeyviewerStyle KeyViewerStyle = KeyviewerStyle.Key16;
        public FootKeyviewerStyle FootKeyViewerStyle = FootKeyviewerStyle.Key4;

        public KeyCode[] key8 = {
            KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash
        };
        public string[] key8Text = new string[8];
        public KeyCode[] key10 = {
            KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash,
            KeyCode.Space, KeyCode.Comma
        };
        public string[] key10Text = new string[10];
        public KeyCode[] key12 = {
            KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash,
            KeyCode.Space, KeyCode.C, KeyCode.Comma, KeyCode.Period
        };
        public string[] key12Text = new string[12];
        public KeyCode[] key14 = {
            KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash,
            KeyCode.Space, KeyCode.C, KeyCode.Comma, KeyCode.Period, KeyCode.CapsLock, KeyCode.LeftShift
        };
        public string[] key14Text = new string[14];
        public KeyCode[] key16 = {
            KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash,
            KeyCode.Space, KeyCode.C, KeyCode.Comma, KeyCode.Period, KeyCode.CapsLock, KeyCode.LeftShift, KeyCode.Return, KeyCode.H
        };
        public string[] key16Text = new string[16];
        public KeyCode[] key20 = {
            KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash,
            KeyCode.Space, KeyCode.C, KeyCode.Comma, KeyCode.Period, KeyCode.CapsLock, KeyCode.LeftShift, KeyCode.Return, KeyCode.H,
            KeyCode.LeftControl, KeyCode.D, KeyCode.RightShift, KeyCode.Semicolon
        };
        public string[] key20Text = new string[20];
        public KeyCode[] key24 = {
            KeyCode.Tab, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.E, KeyCode.P, KeyCode.Equals, KeyCode.Backspace, KeyCode.Backslash,
            KeyCode.Space, KeyCode.C, KeyCode.Comma, KeyCode.Period, KeyCode.CapsLock, KeyCode.LeftShift, KeyCode.Return, KeyCode.H,
            KeyCode.LeftControl, KeyCode.D, KeyCode.RightShift, KeyCode.Q, KeyCode.Z, KeyCode.X, KeyCode.V, KeyCode.B
        };
        public string[] key24Text = new string[24];

        public KeyCode[] key108 = KeyViewer.BuildDefaultKey108();

        public KeyCode[] footkey2 = { KeyCode.F8, KeyCode.F3 };
        public KeyCode[] footkey4 = { KeyCode.F8, KeyCode.F3, KeyCode.F7, KeyCode.F2 };
        public KeyCode[] footkey6 = { KeyCode.F8, KeyCode.F3, KeyCode.F7, KeyCode.F2, KeyCode.F6, KeyCode.F1 };
        public KeyCode[] footkey8 = { KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1 };
        public KeyCode[] footkey10 = { KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1, KeyCode.F9, KeyCode.F10 };
        public KeyCode[] footkey12 = { KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12 };
        public KeyCode[] footkey14 = { KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12, KeyCode.F13, KeyCode.F14 };
        public KeyCode[] footkey16 = { KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12, KeyCode.F13, KeyCode.F14, KeyCode.F15, KeyCode.F16 };

        public string[] footkey2Text = new string[2];
        public string[] footkey4Text = new string[4];
        public string[] footkey6Text = new string[6];
        public string[] footkey8Text = new string[8];
        public string[] footkey10Text = new string[10];
        public string[] footkey12Text = new string[12];
        public string[] footkey14Text = new string[14];
        public string[] footkey16Text = new string[16];

        public KeyCode[] GhostKey8 = new KeyCode[8];
        public KeyCode[] GhostKey10 = new KeyCode[10];
        public KeyCode[] GhostKey12 = new KeyCode[12];
        public KeyCode[] GhostKey14 = new KeyCode[14];
        public KeyCode[] GhostKey16 = new KeyCode[16];
        public KeyCode[] GhostKey20 = new KeyCode[20];
        public KeyCode[] GhostKey24 = new KeyCode[24];

        public int[] Count = new int[KeyViewer.MaxKeySlots];
        public int TotalCount;

        public bool DownLocation;
        public float Size = 1f;
        public bool Enabled = true;

        public Color Background = KeyViewer.Background;
        public Color BackgroundClicked = KeyViewer.BackgroundClicked;
        public Color Outline = KeyViewer.Outline;
        public Color OutlineClicked = KeyViewer.OutlineClicked;
        public Color Text = KeyViewer.Text;
        public Color TextClicked = KeyViewer.TextClicked;
        public Color RainColor = KeyViewer.RainColor;
        public Color RainColor2 = KeyViewer.RainColor2;
        public Color RainColor3 = KeyViewer.RainColor3;

        public Color KpsBackground = KeyViewer.Background;
        public Color KpsOutline = KeyViewer.Outline;
        public Color KpsText = KeyViewer.Text;

        public Color TotalBackground = KeyViewer.Background;
        public Color TotalOutline = KeyViewer.Outline;
        public Color TotalText = KeyViewer.Text;

        public bool EnableRainEffect = true;
        public bool EnableRainFade = true;
        public bool EnableGhostRain = false;
        public float RainFadeDuration = 0.5f;
        public bool EnableRainGradient = false;
        public float RainFadePx = 40f;
        public bool EnableRainForRow1 = true;
        public bool EnableRainForRow2 = true;
        public bool EnableRainForRow3 = true;
        public float RainSpeedRow1 = 100f;
        public float RainSpeedRow2 = 100f;
        public float RainSpeedRow3 = 100f;
        public float RainHeightRow1 = 275f;
        public float RainHeightRow2 = 275f;
        public float RainHeightRow3 = 275f;
        public float RainWidthRow1 = 50f;
        public float RainWidthRow2 = 40f;
        public float RainWidthRow3 = 30f;
        public float RainStartYRow1 = -223f;
        public float RainStartYRow2 = -169f;
        public float RainStartYRow3 = -115f;
        public float GhostRainStartYRow1 = -223f;
        public float GhostRainStartYRow2 = -169f;
        public float GhostRainStartYRow3 = -115f;
        public float GhostRainSpeedRow1 = 100f;
        public float GhostRainSpeedRow2 = 100f;
        public float GhostRainSpeedRow3 = 100f;
        public float GhostRainHeightRow1 = 275f;
        public float GhostRainHeightRow2 = 275f;
        public float GhostRainHeightRow3 = 275f;
        public float GhostRainWidthRow1 = 50f;
        public float GhostRainWidthRow2 = 40f;
        public float GhostRainWidthRow3 = 30f;

        public Vector2 MainKeyViewerPosition = new Vector2(0, 1);
        public Vector2 FootKeyViewerPosition = new Vector2(0.24f, 1f);
        public bool CustomPositionEnabled = false;

        public int FontIndex = 1;
        public string FontName = "";
        public int FontStyleFlags = 0;

        public bool EnableCountFormatting = false;
        public bool HideMainKeyCount = false;
        public bool EnablePerKeyKps = false;
        public bool StreamerMode = false;
        public bool StandardKeyWidth = false;

        public float KeyFontSize = 20f;
        public bool EnablePressAnimation = false;
        public float PressAnimationScale = 0.95f;
        public bool EnablePressAnimationOnRain = false;

        public Color GhostRainColor = KeyViewer.GhostRainColorDefault;
        public Color GhostRainColor2 = KeyViewer.GhostRainColor2Default;
        public Color GhostRainColor3 = KeyViewer.GhostRainColor3Default;

        public bool EnableRainShadowRow1;
        public bool EnableRainShadowRow2;
        public bool EnableRainShadowRow3;
        public Color RainShadowColorRow1 = KeyViewer.RainShadowColorDefault;
        public Color RainShadowColorRow2 = KeyViewer.RainShadowColorDefault;
        public Color RainShadowColorRow3 = KeyViewer.RainShadowColorDefault;
        public float RainShadowOffsetXRow1 = 3f;
        public float RainShadowOffsetYRow1 = -3f;
        public float RainShadowOffsetXRow2 = 3f;
        public float RainShadowOffsetYRow2 = -3f;
        public float RainShadowOffsetXRow3 = 3f;
        public float RainShadowOffsetYRow3 = -3f;

        public bool EnableRainOutlineRow1;
        public bool EnableRainOutlineRow2;
        public bool EnableRainOutlineRow3;
        public Color RainOutlineColorRow1 = KeyViewer.RainOutlineColorDefault;
        public Color RainOutlineColorRow2 = KeyViewer.RainOutlineColorDefault;
        public Color RainOutlineColorRow3 = KeyViewer.RainOutlineColorDefault;
        public float RainOutlineWidthRow1 = 2f;
        public float RainOutlineWidthRow2 = 2f;
        public float RainOutlineWidthRow3 = 2f;

        public bool EnableGhostRainShadowRow1;
        public bool EnableGhostRainShadowRow2;
        public bool EnableGhostRainShadowRow3;
        public Color GhostRainShadowColorRow1 = KeyViewer.RainShadowColorDefault;
        public Color GhostRainShadowColorRow2 = KeyViewer.RainShadowColorDefault;
        public Color GhostRainShadowColorRow3 = KeyViewer.RainShadowColorDefault;
        public float GhostRainShadowOffsetXRow1 = 3f;
        public float GhostRainShadowOffsetYRow1 = -3f;
        public float GhostRainShadowOffsetXRow2 = 3f;
        public float GhostRainShadowOffsetYRow2 = -3f;
        public float GhostRainShadowOffsetXRow3 = 3f;
        public float GhostRainShadowOffsetYRow3 = -3f;

        public bool EnableGhostRainOutlineRow1;
        public bool EnableGhostRainOutlineRow2;
        public bool EnableGhostRainOutlineRow3;
        public Color GhostRainOutlineColorRow1 = KeyViewer.RainOutlineColorDefault;
        public Color GhostRainOutlineColorRow2 = KeyViewer.RainOutlineColorDefault;
        public Color GhostRainOutlineColorRow3 = KeyViewer.RainOutlineColorDefault;
        public float GhostRainOutlineWidthRow1 = 2f;
        public float GhostRainOutlineWidthRow2 = 2f;
        public float GhostRainOutlineWidthRow3 = 2f;

        public bool EnablePerKeyColors = false;

        // ===== Full 108-key keyboard unified color control / 全键盘统一配色控制 =====
        // Only background / outline / text (+ pressed variants); rain / ghost / KPS colors untouched / 仅背景/描边/文字，雨滴/鬼键/KPS 色不动
        public bool EnableFullKeyboardUnifiedColor = true;
        public Color FullKeyboardBackground = KeyViewer.Background;
        public Color FullKeyboardBackgroundClicked = KeyViewer.BackgroundClicked;
        public Color FullKeyboardOutline = KeyViewer.Outline;
        public Color FullKeyboardOutlineClicked = KeyViewer.OutlineClicked;
        public Color FullKeyboardText = KeyViewer.Text;
        public Color FullKeyboardTextClicked = KeyViewer.TextClicked;
        // Optional KPS / Total boxes in full-keyboard mode / 全键盘模式下可选的 KPS/Total 框
        public bool FullKeyboardShowKpsTotal = false;
        // Custom KPS / Total position (normalized 0-1, applied in full-keyboard mode) / KPS/Total 自定义位置（归一化 0-1，仅全键盘生效）
        public Vector2 FullKpsPosition = new Vector2(0.62f, 0.12f);
        public Vector2 FullTotalPosition = new Vector2(0.71f, 0.12f);
        // KPS / Total box size in full-keyboard mode (width & height in px) / 全键盘下 KPS/Total 框的尺寸（宽高，像素）
        public float FullKeyboardKpsTotalSize = 150f;
        // Center the KPS / Total text+value (stacked, centered) instead of left-label / right-number / 居中显示 KPS/Total（上下堆叠居中）而非左文本右数值（仅 slim 布局生效，含全键盘）
        public bool KpsTotalCentered = false;
        // Stack KPS/Total label and value vertically when centered is enabled / 居中模式下将标签和数值上下堆叠
        public bool KpsTotalStackedWhenCentered = false;
        public Color[] PerKeyBackground;
        public Color[] PerKeyBackgroundClicked;
        public Color[] PerKeyOutline;
        public Color[] PerKeyOutlineClicked;
        public Color[] PerKeyText;
        public Color[] PerKeyTextClicked;
        public Color[] PerKeyRainColor;
        public Color[] PerKeyGhostRainColor;

        public ProfileData()
        {
            key8Text = key8Text ?? new string[8];
            key10Text = key10Text ?? new string[10];
            key12Text = key12Text ?? new string[12];
            key14Text = key14Text ?? new string[14];
            key16Text = key16Text ?? new string[16];
            key20Text = key20Text ?? new string[20];
            key24Text = key24Text ?? new string[24];
            key108 = key108 ?? KeyViewer.BuildDefaultKey108();
            footkey2Text = footkey2Text ?? new string[2];
            footkey4Text = footkey4Text ?? new string[4];
            footkey6Text = footkey6Text ?? new string[6];
            footkey8Text = footkey8Text ?? new string[8];
            footkey10Text = footkey10Text ?? new string[10];
            footkey12Text = footkey12Text ?? new string[12];
            footkey14Text = footkey14Text ?? new string[14];
            footkey16Text = footkey16Text ?? new string[16];
            GhostKey8 = GhostKey8 ?? new KeyCode[8];
            GhostKey10 = GhostKey10 ?? new KeyCode[10];
            GhostKey12 = GhostKey12 ?? new KeyCode[12];
            GhostKey14 = GhostKey14 ?? new KeyCode[14];
            GhostKey16 = GhostKey16 ?? new KeyCode[16];
            GhostKey20 = GhostKey20 ?? new KeyCode[20];
            GhostKey24 = GhostKey24 ?? new KeyCode[24];
            Count = Count ?? new int[KeyViewer.MaxKeySlots];
            int n = KeyViewer.MaxKeySlots + 2;
            PerKeyBackground = SafeEnsure(PerKeyBackground, n, KeyViewer.Background);
            PerKeyBackgroundClicked = SafeEnsure(PerKeyBackgroundClicked, n, KeyViewer.BackgroundClicked);
            PerKeyOutline = SafeEnsure(PerKeyOutline, n, KeyViewer.Outline);
            PerKeyOutlineClicked = SafeEnsure(PerKeyOutlineClicked, n, KeyViewer.OutlineClicked);
            PerKeyText = SafeEnsure(PerKeyText, n, KeyViewer.Text);
            PerKeyTextClicked = SafeEnsure(PerKeyTextClicked, n, KeyViewer.TextClicked);
            PerKeyRainColor = SafeEnsure(PerKeyRainColor, n, KeyViewer.RainColor);
            PerKeyGhostRainColor = SafeEnsure(PerKeyGhostRainColor, n, KeyViewer.GhostRainColorDefault);
        }

        private static Color[] SafeEnsure(Color[] arr, int len, Color fill)
        {
            if (arr != null && arr.Length == len) return arr;
            Color[] r = new Color[len];
            for (int i = 0; i < len; i++)
                r[i] = (arr != null && i < arr.Length) ? arr[i] : fill;
            return r;
        }

        public void InitPerKeyColors()
        {
            int n = KeyViewer.MaxKeySlots + 2;
            int footBase = KeyViewer.FootKeyBase;

            var oldBg = PerKeyBackground;
            var oldBgClicked = PerKeyBackgroundClicked;
            var oldOutline = PerKeyOutline;
            var oldOutlineClicked = PerKeyOutlineClicked;
            var oldText = PerKeyText;
            var oldTextClicked = PerKeyTextClicked;
            var oldRain = PerKeyRainColor;
            var oldGhostRain = PerKeyGhostRainColor;

            PerKeyBackground = new Color[n];
            PerKeyBackgroundClicked = new Color[n];
            PerKeyOutline = new Color[n];
            PerKeyOutlineClicked = new Color[n];
            PerKeyText = new Color[n];
            PerKeyTextClicked = new Color[n];
            PerKeyRainColor = new Color[n];
            PerKeyGhostRainColor = new Color[n];

            for (int i = 0; i < n; i++)
            {
                PerKeyBackground[i] = oldBg != null && i < oldBg.Length ? oldBg[i] : KeyViewer.Background;
                PerKeyBackgroundClicked[i] = oldBgClicked != null && i < oldBgClicked.Length
                    ? oldBgClicked[i] : KeyViewer.BackgroundClicked;
                PerKeyOutline[i] = oldOutline != null && i < oldOutline.Length ? oldOutline[i] : KeyViewer.Outline;
                PerKeyOutlineClicked[i] = oldOutlineClicked != null && i < oldOutlineClicked.Length
                    ? oldOutlineClicked[i] : KeyViewer.OutlineClicked;
                PerKeyText[i] = oldText != null && i < oldText.Length ? oldText[i] : KeyViewer.Text;
                PerKeyTextClicked[i] = oldTextClicked != null && i < oldTextClicked.Length
                    ? oldTextClicked[i] : KeyViewer.TextClicked;
            }

            for (int i = 0; i < n; i++)
            {
                if (oldRain != null && i < oldRain.Length)
                {
                    PerKeyRainColor[i] = oldRain[i];
                }
                else
                {
                    if (i < 8) PerKeyRainColor[i] = KeyViewer.RainColor;
                    else if (i < 16) PerKeyRainColor[i] = KeyViewer.RainColor2;
                    else if (i < footBase) PerKeyRainColor[i] = KeyViewer.RainColor3;
                    else if (i < KeyViewer.MaxKeySlots) PerKeyRainColor[i] = KeyViewer.RainColor;
                    else PerKeyRainColor[i] = KeyViewer.RainColor;
                }
                PerKeyGhostRainColor[i] = oldGhostRain != null && i < oldGhostRain.Length
                    ? oldGhostRain[i] : KeyViewer.GhostRainColorDefault;
            }
        }
    }

    /// <summary>
    /// Serializable settings data model for the mod / Mod 的可序列化设置数据模型
    /// Includes key bindings, layout configuration, colors, rain effect parameters, and positioning / 包含按键绑定、布局配置、颜色、雨滴效果参数和位置
    /// </summary>
    [System.Serializable]
    public class KeyViewerSettings
    {
        public int Version = 5;
        public string CurrentProfile = "Default";
        public string[] ProfileNames = new[] { "Default" };
        public string Language = "en";
        /// <summary>Last active settings GUI tab (persisted in meta JSON) / 上次停留的设置界面标签页(存入 meta JSON)</summary>
        public int UiTab;
        public ProfileData Data = new ProfileData();

        public KeyViewerSettings()
        {
            CurrentProfile = CurrentProfile ?? "Default";
            ProfileNames = ProfileNames ?? new[] { "Default" };
        }
    }

    /// <summary>
    /// Font entry associating a display name with a TMP_FontAsset / 字体条目，将显示名称关联到 TMP_FontAsset
    /// </summary>
    public class FontEntry
    {
        public string name;
        public TMP_FontAsset font;
        public string sourceFontName;
        public FontEntry(string name, TMP_FontAsset font) { this.name = name; this.font = font; sourceFontName = name; }
    }

    /// <summary>
    /// Utility helpers for IMGUI drawing / IMGUI 绘制工具方法
    /// </summary>
    public static class GUIUtils
    {
        /// <summary>
        /// Draw a solid-color rectangle using GUI.DrawTexture / 使用 GUI.DrawTexture 绘制纯色矩形
        /// </summary>
        public static void DrawRect(Rect position, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(position, Texture2D.whiteTexture);
            GUI.color = prev;
        }
    }
}
