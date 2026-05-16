// Settings data model and serialization helpers / 设置数据模型和序列化辅助类
// All user-configurable options are stored here and persisted as JSON / 所有用户可配置选项都存储在这里并序列化为 JSON

using TMPro;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>
    /// Serializable settings data model for the mod / Mod 的可序列化设置数据模型
    /// Includes key bindings, layout configuration, colors, rain effect parameters, and positioning / 包含按键绑定、布局配置、颜色、雨滴效果参数和位置
    /// </summary>
    [System.Serializable]
    public class KeyViewerSettings
    {
        /// <summary>Settings file version for migration / 设置文件版本号，用于迁移</summary>
        public int Version = 2;
        /// <summary>Selected main key layout / 选中的主按键布局</summary>
        public KeyviewerStyle KeyViewerStyle = KeyviewerStyle.Key16;
        /// <summary>Selected foot key layout / 选中的脚键布局</summary>
        public FootKeyviewerStyle FootKeyViewerStyle = FootKeyviewerStyle.Key4;

        // Main key bindings for each layout / 每种布局的主按键绑定
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

        // Foot key bindings (size 2 through 16) / 脚键绑定（2 到 16）
        public KeyCode[] footkey2 = { KeyCode.F8, KeyCode.F3 };
        public KeyCode[] footkey4 = { KeyCode.F8, KeyCode.F3, KeyCode.F7, KeyCode.F2 };
        public KeyCode[] footkey6 = { KeyCode.F8, KeyCode.F3, KeyCode.F7, KeyCode.F2, KeyCode.F6, KeyCode.F1 };
        public KeyCode[] footkey8 = { KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1 };
        public KeyCode[] footkey10 = { KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1, KeyCode.F9, KeyCode.F10 };
        public KeyCode[] footkey12 = { KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12 };
        public KeyCode[] footkey14 = { KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12, KeyCode.F13, KeyCode.F14 };
        public KeyCode[] footkey16 = { KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12, KeyCode.F13, KeyCode.F14, KeyCode.F15, KeyCode.F16 };

        // Foot key custom text labels / 脚键自定义文本标签
        public string[] footkey2Text = new string[2];
        public string[] footkey4Text = new string[4];
        public string[] footkey6Text = new string[6];
        public string[] footkey8Text = new string[8];
        public string[] footkey10Text = new string[10];
        public string[] footkey12Text = new string[12];
        public string[] footkey14Text = new string[14];
        public string[] footkey16Text = new string[16];

        /// <summary>Per-key press counter (index 0-35) / 每个按键的按下计数（索引 0-35）</summary>
        public int[] Count = new int[36];
        /// <summary>Total key press count / 总按键次数</summary>
        public int TotalCount;

        /// <summary>Whether to place keys below the default position / 是否将按键放在默认位置下方</summary>
        public bool DownLocation;
        /// <summary>Overall scale multiplier / 整体缩放倍率</summary>
        public float Size = 1f;
        /// <summary>Whether the mod overlay is enabled / 是否启用 Mod 覆盖层</summary>
        public bool Enabled = true;

        // Color settings / 颜色设置
        public Color Background = KeyViewer.Background;
        public Color BackgroundClicked = KeyViewer.BackgroundClicked;
        public Color Outline = KeyViewer.Outline;
        public Color OutlineClicked = KeyViewer.OutlineClicked;
        public Color Text = KeyViewer.Text;
        public Color TextClicked = KeyViewer.TextClicked;
        public Color RainColor = KeyViewer.RainColor;
        public Color RainColor2 = KeyViewer.RainColor2;
        public Color RainColor3 = KeyViewer.RainColor3;

        /// <summary>Master rain effect toggle / 雨滴效果总开关</summary>
        public bool EnableRainEffect = true;
        /// <summary>Rain fade-out on key release toggle / 雨滴松开淡出开关</summary>
        public bool EnableRainFade = false;
        /// <summary>Per-row rain toggles / 每排雨滴独立开关</summary>
        public bool EnableRainForRow1 = true;
        public bool EnableRainForRow2 = true;
        public bool EnableRainForRow3 = true;
        /// <summary>Per-row rain speed / 每排雨滴独立速度</summary>
        public float RainSpeedRow1 = 100f;
        public float RainSpeedRow2 = 100f;
        public float RainSpeedRow3 = 100f;
        /// <summary>Per-row rain height / 每排雨滴独立高度</summary>
        public float RainHeightRow1 = 275f;
        public float RainHeightRow2 = 275f;
        public float RainHeightRow3 = 275f;

        // Custom position (normalized 0-1, X=0 left X=1 right, Y=0 top Y=1 bottom) / 自定义位置（归一化 0-1）
        public Vector2 MainKeyViewerPosition = new Vector2(0, 1);
        public Vector2 FootKeyViewerPosition = new Vector2(0.24f, 1f);
        public bool CustomPositionEnabled = false;

        /// <summary>Selected font index in fontList / 字体列表中的选中字体索引</summary>
        public int FontIndex = 1;
        /// <summary>Font name for persistence across scene loads / 用于跨场景持久化的字体名称</summary>
        public string FontName = "";
        /// <summary>Language code / 语言代码</summary>
        public string Language = "en";

        /// <summary>Format large counts with thousands separator / 大数字千分位格式化</summary>
        public bool EnableCountFormatting = false;
        /// <summary>Hide press count on main keys / 隐藏主按键计数</summary>
        public bool HideMainKeyCount = false;

        /// <summary>
        /// Ensure all arrays are initialized (prevents null refs on load with legacy data) / 确保所有数组已初始化（防止加载旧数据时出现空引用）
        /// </summary>
        public KeyViewerSettings()
        {
            key8Text = key8Text ?? new string[8];
            key10Text = key10Text ?? new string[10];
            key12Text = key12Text ?? new string[12];
            key16Text = key16Text ?? new string[16];
            key20Text = key20Text ?? new string[20];
            footkey2Text = footkey2Text ?? new string[2];
            footkey4Text = footkey4Text ?? new string[4];
            footkey6Text = footkey6Text ?? new string[6];
            footkey8Text = footkey8Text ?? new string[8];
            footkey10Text = footkey10Text ?? new string[10];
            footkey12Text = footkey12Text ?? new string[12];
            footkey14Text = footkey14Text ?? new string[14];
            footkey16Text = footkey16Text ?? new string[16];
            Count = Count ?? new int[36];
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
