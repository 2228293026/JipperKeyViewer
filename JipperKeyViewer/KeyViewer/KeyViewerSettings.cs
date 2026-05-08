using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    [System.Serializable]
    public class KeyViewerSettings
    {
        public int Version = 2;
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
        public KeyCode[] footkey2 = { KeyCode.F8, KeyCode.F3 };
        public KeyCode[] footkey4 = { KeyCode.F8, KeyCode.F3, KeyCode.F7, KeyCode.F2 };
        public KeyCode[] footkey6 = { KeyCode.F8, KeyCode.F3, KeyCode.F7, KeyCode.F2, KeyCode.F6, KeyCode.F1 };
        public KeyCode[] footkey8 = { KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1 };
        public KeyCode[] footkey10 = { KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1, KeyCode.F9, KeyCode.F10 };
        public KeyCode[] footkey12 = { KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12 };
        public KeyCode[] footkey14 = { KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12, KeyCode.F13, KeyCode.F14 };
        public KeyCode[] footkey16 = { KeyCode.F8, KeyCode.F4, KeyCode.F7, KeyCode.F3, KeyCode.F6, KeyCode.F2, KeyCode.F5, KeyCode.F1, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12, KeyCode.F13, KeyCode.F14, KeyCode.F15, KeyCode.F16 };
        public int[] Count = new int[36];
        public int TotalCount;
        public bool DownLocation;
        public float Size = 1f;
        public bool Enabled = true;
        // 颜色设置
        public Color Background = KeyViewer.Background;
        public Color BackgroundClicked = KeyViewer.BackgroundClicked;
        public Color Outline = KeyViewer.Outline;
        public Color OutlineClicked = KeyViewer.OutlineClicked;
        public Color Text = KeyViewer.Text;
        public Color TextClicked = KeyViewer.TextClicked;
        public Color RainColor = KeyViewer.RainColor;
        public Color RainColor2 = KeyViewer.RainColor2;
        public Color RainColor3 = KeyViewer.RainColor3;
        public bool EnableRainEffect = true;
        // 每一排的独立雨滴启用开关
        public bool EnableRainForRow1 = true;
        public bool EnableRainForRow2 = true;
        public bool EnableRainForRow3 = true;
        // 每一排的独立雨滴速度
        public float RainSpeedRow1 = 100f;
        public float RainSpeedRow2 = 100f;
        public float RainSpeedRow3 = 100f;

        public float RainHeightRow1 = 275f;
        public float RainHeightRow2 = 275f;
        public float RainHeightRow3 = 275f;

        public Vector2 MainKeyViewerPosition = new Vector2(0, 1);
        public Vector2 FootKeyViewerPosition = new Vector2(0.24f, 1f);
        public bool CustomPositionEnabled = false;
        public int FontIndex = 1;
        public string FontName = "";
        public string Language = "en";
        // 添加构造函数来确保数组正确初始化
        public KeyViewerSettings()
        {
            // 确保所有文本数组不为null
            key8Text = key8Text ?? new string[8];
            key10Text = key10Text ?? new string[10];
            key12Text = key12Text ?? new string[12];
            key16Text = key16Text ?? new string[16];
            key20Text = key20Text ?? new string[20];
            Count = Count ?? new int[36];
        }
    }
    public class FontEntry
    {
        public string name;
        public TMP_FontAsset font;
        public FontEntry(string name, TMP_FontAsset font) { this.name = name; this.font = font; }
    }

    public static class GUIUtils
    {
        private static Texture2D _staticRectTexture;
        private static GUIStyle _staticRectStyle;
        public static void DrawRect(Rect position, Color color)
        {
            if (_staticRectTexture == null)
            {
                _staticRectTexture = new Texture2D(1, 1);
            }
            if (_staticRectStyle == null)
            {
                _staticRectStyle = new GUIStyle();
            }
            _staticRectTexture.SetPixel(0, 0, color);
            _staticRectTexture.Apply();
            _staticRectStyle.normal.background = _staticRectTexture;
            GUI.Box(position, GUIContent.none, _staticRectStyle);
        }
    }
}
