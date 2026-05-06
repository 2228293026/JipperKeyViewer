using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace JipperKeyViewer.KeyViewer
{
    public class KeyViewer : MonoBehaviour
    {
        public static KeyViewerSettings Settings;
        public static readonly Color Background = new(0.5607843f, 0.2352941f, 1, 0.1960784f);
        public static readonly Color BackgroundClicked = Color.white;
        public static readonly Color Outline = new(0.5529412f, 0.2431373f, 1);
        public static readonly Color OutlineClicked = Color.white;
        public static readonly Color Text = Color.white;
        public static readonly Color TextClicked = Color.black;
        public static readonly Color RainColor = new(0.5137255f, 0.1254902f, 0.858823538f);
        public static readonly Color RainColor2 = Color.white;
        public static readonly Color RainColor3 = Color.magenta;
        public static readonly byte[] BackSequence8 = Array.Empty<byte>();
        public static readonly byte[] BackSequence10 = new byte[] { 8, 9 };
        public static readonly byte[] BackSequence12 = new byte[] { 9, 8, 10, 11 };
        public static readonly byte[] BackSequence16 = new byte[] { 12, 13, 9, 8, 10, 11, 14, 15 };
        public static readonly byte[] BackSequence20 = new byte[] { 12, 13, 9, 8, 10, 11, 14, 15, 17, 16, 18, 19 };

        static readonly string[] KeyLayoutNames = { "12K", "16K", "20K", "10K", "8K" };
        static readonly string[] FootKeyLayoutNames = { "Off", "2K", "4K", "6K", "8K", "10K", "12K", "14K", "16K" };

        // Cache once: avoid Enum.GetValues allocation every frame
        static KeyViewer()
        {
            AllKeyCodes = (KeyCode[])Enum.GetValues(typeof(KeyCode));
        }

        GameObject KeyViewerObject;
        GameObject KeyViewerSizeObject; // 缓存大小对象，用于调整大小
        Canvas Canvas;
        Key[] Keys;
        Key Kps;
        int lastKps;
        int lastTotal;
        Key Total;
        Queue<long> PressTimes;
        Stopwatch Stopwatch;
        bool Save;
        long lastFrameMs;
        const long MAX_DELTA_MS = 50;
        bool KeyChangeExpanded;
        bool TextChangeExpanded;
        bool[] ColorExpanded;
        KeyviewerStyle currentKeyViewerStyle;
        bool[] KeyPressed; // 用于按键选择逻辑
        int SelectedKey = -1;
        int WinAPICool; // 用于按键选择逻辑
        bool TextChanged; // 用于按键选择逻辑

        static string ConfigPath
        {
            get
            {
                string modPath = Path.GetDirectoryName(Main.Mod?.Path);
                return Path.Combine(modPath ?? Application.persistentDataPath, "config", "settings.json");
            }
        }
        // 缓存加载的资源
        Sprite keyBackgroundSprite;
        Sprite keyOutlineSprite;
        TMP_FontAsset defaultFont;
        public static KeyViewer instance;
        private Stack<Rain> rainPool = new Stack<Rain>();
        private static readonly KeyCode[] AllKeyCodes;
        private KeyviewerStyle cachedKeyStyle = (KeyviewerStyle)(-1);
        private KeyCode[] cachedMainKeys;
        private FootKeyviewerStyle cachedFootStyle = (FootKeyviewerStyle)(-1);
        private KeyCode[] cachedFootKeys;
        private TMP_FontAsset mapleFont;
        private Dictionary<TMP_FontAsset, Material> shadowMaterials = new Dictionary<TMP_FontAsset, Material>();
        static readonly List<FontEntry> fontList = new List<FontEntry>();
        bool fontListExpanded;
        private bool wasEnabled;

        void Awake()
        {
            instance = this;
            LoadSettings();
            I18n.Load();
            I18n.Lang = Settings.Language;
            currentKeyViewerStyle = Settings.KeyViewerStyle;
            // 在Awake中尝试加载资源
            TryLoadResources();
            wasEnabled = Settings.Enabled; // 初始化状态跟踪
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        void Start()
        {
            EnableKeyViewer();
        }
        void TryRestoreFont()
        {
            if (string.IsNullOrEmpty(Settings.FontName)) return;
            if (fontList.Exists(e => e.name == Settings.FontName)) return; // 已恢复
            ScanGameFonts();
            int idx = fontList.FindIndex(e => e.name == Settings.FontName);
            if (idx >= 0)
            {
                Settings.FontIndex = idx;
                UpdateAllFonts();
                Debug.Log($"KeyViewer: 已恢复字体 {Settings.FontName}");
            }
        }
        void OnEnable()
        {
            if (Settings.Enabled)
                EnableKeyViewer();
            else
                DisableKeyViewer();
            if (Settings.CustomPositionEnabled)
            {
                ResetKeyViewerPosition();
                ResetFootKeyViewerPosition();
            }
        }
        void OnDisable()
        {
            DisableKeyViewer();
        }
        void OnDestroy()
        {
            SaveSettings();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            ClearAllRains();
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 在场景加载后第一时间清空所有雨线
            ClearAllRainDrops();
            Debug.Log($"Scene changed to {scene.name}, cleared all rain drops");
        }
        void Update()
        {
            TryRestoreFont();
            if (wasEnabled != Settings.Enabled)
            {
                if (Settings.Enabled)
                {
                    EnableKeyViewer();
                }
                else
                {
                    DisableKeyViewer();
                }
                wasEnabled = Settings.Enabled;
            }
            if (KeyViewerObject != null && Settings.Enabled) // 确保KeyViewer已启用
            {
                ProcessKeySelection();
                // 在主线程中处理按键状态、计数、KPS、保存设置
                ProcessMainAndFootKeysInUpdate();
                ProcessKeyRainQueues();
                ProcessKpsAndSaveInUpdate();

                if (Settings.EnableRainEffect)
                {
                    UpdateRainEffects();
                }
            }
        }
        // 获取当前时间（不受暂停影响）

        private void UpdateRainEffects()
        {
            if (!Settings.EnableRainEffect) return;

            if (Stopwatch == null)
            {
                Stopwatch = Stopwatch.StartNew();
                lastFrameMs = 0;
                return;
            }
            if (Keys == null || Keys.Length == 0) return;

            long now = Stopwatch.ElapsedMilliseconds;
            float deltaMs = lastFrameMs == 0 ? 0 : Mathf.Min(MAX_DELTA_MS, now - lastFrameMs);
            lastFrameMs = now;

            for (int i = 0; i < Keys.Length; i++)
            {
                Key key = Keys[i];
                if (key == null || key.rainList.Count == 0) continue;

                bool isKeyPressed = key.isPressed;
                // 每键只查一次行参数，避免雨滴内循环重复 if 链
                float rainSpeed = GetRainSpeedForKey(i);
                float rainHeight = GetRainTravelDistanceForKey(i);

                for (int j = 0; j < key.rainList.Count; j++)
                {
                    RawRain rain = key.rainList[j];
                    if (rain.removed) continue;

                    bool updateSize = isKeyPressed && j == key.rainList.Count - 1;

                    if (!rain.UpdateLocation(deltaMs, updateSize, rainSpeed, rainHeight))
                    {
                        rain.removed = true;
                        key.rainList.RemoveAt(j);
                        j--;
                    }
                }
            }
        }

        private void TriggerRainEffect(int keyIndex, Key key)
        {
            if (!Settings.EnableRainEffect || key == null || !IsRainEnabledForKey(keyIndex))
                return;

            // 立即生成第一个雨滴
            CreateRainDropForKey(keyIndex, key);
        }

        private void CreateRainDropForKey(int keyIndex, Key key)
        {
            if (key == null || key.rain == null) return;

            RawRain rawRain = new RawRain(key.rain.transform, key.color);
            key.rawRainQueue.Enqueue(rawRain);
            key.rainList.Add(rawRain);
        }



        // 新增：获取雨滴速度
        private float GetRainSpeedForKey(int keyIndex)
        {
            if (keyIndex < 8) return Settings.RainSpeedRow1;
            if (keyIndex < 16) return Settings.RainSpeedRow2;
            if (keyIndex < 20) return Settings.RainSpeedRow3;
            return 0f;
        }


        private float GetRainTravelDistanceForKey(int keyIndex)
        {
            // 根据按键所在的行返回对应的雨滴高度
            if (keyIndex < 8)
                return Settings.RainHeightRow1; // 第1排
            else if (keyIndex < 16)
                return Settings.RainHeightRow2; // 第2排
            else if (keyIndex < 20)
                return Settings.RainHeightRow3; // 第3排
            else
                return 0f;
        }

        private void ClearAllRainDrops()
        {
            if (Keys == null) return;
            foreach (var key in Keys)
            {
                if (key == null) continue;
                foreach (var rain in key.rainList)
                {
                    rain.removed = true;
                }
                key.rainList.Clear();
            }
        }

        #region 配置文件管理
        private void LoadSettings()
        {
            string directory = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    Settings = JsonUtility.FromJson<KeyViewerSettings>(json);
                    // 确保所有数组在加载后不为null
                    if (Settings != null)
                    {
                        Settings.key8Text = Settings.key8Text ?? new string[8];
                        Settings.key10Text = Settings.key10Text ?? new string[10];
                        Settings.key12Text = Settings.key12Text ?? new string[12];
                        Settings.key16Text = Settings.key16Text ?? new string[16];
                        Settings.key20Text = Settings.key20Text ?? new string[20];
                        Settings.Count = Settings.Count ?? new int[36];
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"加载配置文件失败: {e.Message}");
                    Settings = new KeyViewerSettings();
                }
            }
            else
            {
                Settings = new KeyViewerSettings();
                SaveSettings();
            }
        }
        public void SaveSettings()
        {
            try
            {
                string directory = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                string json = JsonUtility.ToJson(Settings, true);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"保存配置文件失败: {e.Message}");
            }
        }
        #endregion
        #region 设置界面
        public void DrawSettingsWindow()
        {
            ScanGameFonts();
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            string langNow = I18n.Tr("language") + ": " + (Settings.Language == "en" ? "English" : "中文");
            if (GUILayout.Button(langNow))
            {
                Settings.Language = Settings.Language == "en" ? "zh" : "en";
                I18n.Lang = Settings.Language;
                SaveSettings();
            }
            GUILayout.EndHorizontal();

            bool newEnabled = GUILayout.Toggle(Settings.Enabled, (Settings.Enabled ? "✓ " : "✗ ") + I18n.Tr("key_display_on"));
            if (newEnabled != Settings.Enabled)
            {
                Settings.Enabled = newEnabled;
                SaveSettings();
                // 状态变化会在Update中处理
            }
            GUILayout.Label(I18n.Tr("font_style") + ":");
            string curFont = fontList.Count > 0 ? fontList[Settings.FontIndex].name : "无";
            if (GUILayout.Button((fontListExpanded ? "▼ " : "▶ ") + curFont, GUILayout.MinWidth(200)))
                fontListExpanded = !fontListExpanded;
            if (fontListExpanded && fontList.Count > 1)
            {
                int newIdx = Settings.FontIndex;
                for (int i = 0; i < fontList.Count; i++)
                {
                    bool selected = i == Settings.FontIndex;
                    string label = (selected ? "✓ " : "  ") + fontList[i].name;
                    if (GUILayout.Button(label, GUILayout.MinWidth(200)))
                        newIdx = i;
                }
                if (newIdx != Settings.FontIndex)
                {
                    Settings.FontIndex = newIdx;
                    Settings.FontName = fontList[newIdx].name;
                    UpdateAllFonts();
                    SaveSettings();
                }
            }
            // 基本设置
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
                (Settings.CustomPositionEnabled ? "◢ " : "▶ ") + I18n.Tr("custom_pos"));
            if (newCustomPosition != Settings.CustomPositionEnabled)
            {
                Settings.CustomPositionEnabled = newCustomPosition;
                SaveSettings();
                if (Settings.CustomPositionEnabled)
                {
                    ResetKeyViewerPosition();
                    ResetFootKeyViewerPosition();
                }
            }
            if (Settings.CustomPositionEnabled)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label(I18n.Tr("main_key_pos") + ":");
                // 使用局部变量来避免冲突
                Vector2 tempMainPos = Settings.MainKeyViewerPosition;
                Vector2 tempFootPos = Settings.FootKeyViewerPosition;
                bool positionChanged = false;
                // 主按键X坐标
                GUILayout.BeginHorizontal();
                GUILayout.Label("X:", GUILayout.Width(20));
                float newMainX = GUILayout.HorizontalSlider(tempMainPos.x, -1000f, 1000f, GUILayout.Width(120));
                if (newMainX != tempMainPos.x)
                {
                    tempMainPos.x = newMainX;
                    positionChanged = true;
                }
                string mainXText = GUILayout.TextField(tempMainPos.x.ToString("F0"), FloatFieldWidth(tempMainPos.x.ToString("F0")));
                if (float.TryParse(mainXText, out float parsedMainX) && parsedMainX != tempMainPos.x)
                {
                    tempMainPos.x = Mathf.Clamp(parsedMainX, -1000f, 1000f);
                    positionChanged = true;
                }
                GUILayout.EndHorizontal();
                // 主按键Y坐标
                GUILayout.BeginHorizontal();
                GUILayout.Label("Y:", GUILayout.Width(20));
                float newMainY = GUILayout.HorizontalSlider(tempMainPos.y, -1000f, 1000f, GUILayout.Width(120));
                if (newMainY != tempMainPos.y)
                {
                    tempMainPos.y = newMainY;
                    positionChanged = true;
                }
                string mainYText = GUILayout.TextField(tempMainPos.y.ToString("F0"), FloatFieldWidth(tempMainPos.y.ToString("F0")));
                if (float.TryParse(mainYText, out float parsedMainY) && parsedMainY != tempMainPos.y)
                {
                    tempMainPos.y = Mathf.Clamp(parsedMainY, -1000f, 1000f);
                    positionChanged = true;
                }
                GUILayout.EndHorizontal();
                GUILayout.Label(I18n.Tr("foot_key_pos") + ":");
                // 脚键X坐标
                GUILayout.BeginHorizontal();
                GUILayout.Label("X:", GUILayout.Width(20));
                float newFootX = GUILayout.HorizontalSlider(tempFootPos.x, -1000f, 1000f, GUILayout.Width(120));
                if (newFootX != tempFootPos.x)
                {
                    tempFootPos.x = newFootX;
                    positionChanged = true;
                }
                string footXText = GUILayout.TextField(tempFootPos.x.ToString("F0"), FloatFieldWidth(tempFootPos.x.ToString("F0")));
                if (float.TryParse(footXText, out float parsedFootX) && parsedFootX != tempFootPos.x)
                {
                    tempFootPos.x = Mathf.Clamp(parsedFootX, -1000f, 1000f);
                    positionChanged = true;
                }
                GUILayout.EndHorizontal();
                // 脚键Y坐标
                GUILayout.BeginHorizontal();
                GUILayout.Label("Y:", GUILayout.Width(20));
                float newFootY = GUILayout.HorizontalSlider(tempFootPos.y, -1000f, 1000f, GUILayout.Width(120));
                if (newFootY != tempFootPos.y)
                {
                    tempFootPos.y = newFootY;
                    positionChanged = true;
                }
                string footYText = GUILayout.TextField(tempFootPos.y.ToString("F0"), FloatFieldWidth(tempFootPos.y.ToString("F0")));
                if (float.TryParse(footYText, out float parsedFootY) && parsedFootY != tempFootPos.y)
                {
                    tempFootPos.y = Mathf.Clamp(parsedFootY, -1000f, 1000f);
                    positionChanged = true;
                }
                GUILayout.EndHorizontal();
                // 应用位置变化
                if (positionChanged)
                {
                    Settings.MainKeyViewerPosition = tempMainPos;
                    Settings.FootKeyViewerPosition = tempFootPos;
                    ResetKeyViewerPosition();
                    ResetFootKeyViewerPosition();
                    SaveSettings();
                }
                // 重置按钮
                if (GUILayout.Button(I18n.Tr("reset_pos")))
                {
                    Settings.MainKeyViewerPosition = new Vector2(0, 0);
                    Settings.FootKeyViewerPosition = new Vector2(432, 15);
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
            // 脚键布局
            GUILayout.Label(I18n.Tr("foot_keys") + ":");
            FootKeyviewerStyle newFootStyle = (FootKeyviewerStyle)GUILayout.SelectionGrid((int)Settings.FootKeyViewerStyle,
                FootKeyLayoutNames, 5);
            if (newFootStyle != Settings.FootKeyViewerStyle)
            {
                Settings.FootKeyViewerStyle = newFootStyle;
                ResetFootKeyViewer();
                SaveSettings();
            }
            // 大小设置
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

                // 第1排雨线
                GUILayout.BeginHorizontal();
                GUILayout.Label(I18n.Tr("rain_row1") + ":");
                Settings.RainHeightRow1 = GUILayout.HorizontalSlider(Settings.RainHeightRow1, 1f, 1000f, GUILayout.Width(120));
                string height1Text = GUILayout.TextField(Settings.RainHeightRow1.ToString("F2"), FloatFieldWidth(Settings.RainHeightRow1.ToString("F2")));
                if (float.TryParse(height1Text, out float newHeight1))
                {
                    Settings.RainHeightRow1 = newHeight1;
                }
                GUILayout.EndHorizontal();

                // 第2排雨线
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
                    // 第3排雨线
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
                // 第1排速度
                GUILayout.BeginHorizontal();
                GUILayout.Label(I18n.Tr("rain_row1") + ":");
                Settings.RainSpeedRow1 = GUILayout.HorizontalSlider(Settings.RainSpeedRow1, 50f, 1000f, GUILayout.Width(120));
                string speed1Text = GUILayout.TextField(Settings.RainSpeedRow1.ToString("F0"), FloatFieldWidth(Settings.RainSpeedRow1.ToString("F0")));
                if (float.TryParse(speed1Text, out float newSpeed1))
                {
                    Settings.RainSpeedRow1 = Mathf.Clamp(newSpeed1, 50f, 1000f);
                }
                GUILayout.EndHorizontal();

                // 第2排速度
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
                    // 第3排速度
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
            // 按键更改
            KeyChangeExpanded = GUILayout.Toggle(KeyChangeExpanded, (KeyChangeExpanded ? "◢ " : "▶ ") + I18n.Tr("key_change"));
            if (KeyChangeExpanded)
            {
                DrawKeyChangeSection();
            }
            // 文本更改
            TextChangeExpanded = GUILayout.Toggle(TextChangeExpanded, (TextChangeExpanded ? "◢ " : "▶ ") + I18n.Tr("text_change"));
            if (TextChangeExpanded)
            {
                DrawTextChangeSection();
            }
            // 颜色设置
            bool colorsExpanded = GUILayout.Toggle(ColorExpanded != null, (ColorExpanded != null ? "◢ " : "▶ ") + I18n.Tr("colors"));
            if (colorsExpanded && ColorExpanded == null) ColorExpanded = new bool[9];
            if (!colorsExpanded) ColorExpanded = null;
            if (ColorExpanded != null)
            {
                DrawColorSettings();
            }
            GUILayout.EndVertical();
        }
        private void UpdateAllFonts()
        {
            TMP_FontAsset currentFont = GetCurrentFont();
            Material shadowMat = GetShadowMaterial(currentFont);
            void UpdateText(TMP_Text t)
            {
                if (t == null) return;
                t.font = currentFont;
                t.fontMaterial = shadowMat;
            }
            if (Keys != null)
            {
                foreach (Key key in Keys)
                {
                    if (key == null) continue;
                    UpdateText(key.text);
                    UpdateText(key.value);
                }
            }
            UpdateText(Kps?.text);
            UpdateText(Kps?.value);
            UpdateText(Total?.text);
            UpdateText(Total?.value);
        }

        Material GetShadowMaterial(TMP_FontAsset font)
        {
            if (shadowMaterials.TryGetValue(font, out var mat)) return mat;
            mat = new Material(font.material);
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.5f));
            mat.SetFloat("_UnderlayOffsetX", 1f);
            mat.SetFloat("_UnderlayOffsetY", -1f);
            mat.SetFloat("_UnderlaySoftness", 0f);
            shadowMaterials[font] = mat;
            return mat;
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
            // 第3排按键（针对20键布局）
            if (Settings.KeyViewerStyle == KeyviewerStyle.Key20)
            {
                GUILayout.Label(I18n.Tr("row3_keys") + ":");
                GUILayout.BeginHorizontal();
                // 第3排按键索引是16-19
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
            // 脚键
            KeyCode[] footKeyCodes = GetFootKeyCode();
            if (footKeyCodes != null && footKeyCodes.Length > 0) // 检查数组是否存在且非空
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
                    // 第1行: 前8个
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
                    // 第2行: 剩余居中
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
                    TextChanged = true; // 这里也设置为true，允许文本更改
                }
            }
            GUILayout.EndHorizontal();
            // 第3排按键文本（针对20键布局）
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
                RainColor, RainColor2, RainColor3 // 对应的默认颜色
            };
            for (int i = 0; i < 9; i++) // 修改为9个颜色
            {
                // 只有在启用雨滴效果时才显示雨滴颜色设置
                if (i >= 6 && !Settings.EnableRainEffect)
                    continue;
                ColorExpanded[i] = GUILayout.Toggle(ColorExpanded[i], ColorExpanded[i] ? $"◢ {colorNames[i]}" : $"▶ {colorNames[i]}");
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
            // 颜色预览
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
        #endregion
        #region 位置调整方法
        private void ResetKeyViewerPosition()
        {
            if (Keys == null || !Settings.CustomPositionEnabled) return;
            Vector2 basePosition = Settings.MainKeyViewerPosition;
            int remove = Settings.DownLocation ? 200 : 0;
            // 根据当前布局重新设置所有主按键的位置
            switch (Settings.KeyViewerStyle)
            {
                case KeyviewerStyle.Key12:
                    for (int i = 0; i < 8; i++)
                        SetKeyPosition(i, basePosition.x + 54 * i, basePosition.y + 279 - remove);
                    SetKeyPosition(8, basePosition.x + 81 + 54, basePosition.y + 225 - remove);
                    SetKeyPosition(9, basePosition.x + 81, basePosition.y + 225 - remove);
                    SetKeyPosition(10, basePosition.x + 54 * 4, basePosition.y + 225 - remove);
                    SetKeyPosition(11, basePosition.x + 54 * 4 + 81, basePosition.y + 225 - remove);
                    SetKeyPosition(-1, basePosition.x + 0, basePosition.y + 225 - remove); // KPS
                    SetKeyPosition(-2, basePosition.x + 81 + 54 * 5, basePosition.y + 225 - remove); // Total
                    break;
                case KeyviewerStyle.Key16:
                    for (int i = 0; i < 8; i++)
                        SetKeyPosition(i, basePosition.x + 54 * i, basePosition.y + 320 - remove);
                    for (int i = 0; i < 8; i++)
                    {
                        int j = BackSequence16[i];
                        SetKeyPosition(j, basePosition.x + 54 * i, basePosition.y + 266 - remove);
                    }
                    SetKeyPosition(-1, basePosition.x + 0, basePosition.y + 220 - remove); // KPS
                    SetKeyPosition(-2, basePosition.x + 216, basePosition.y + 220 - remove); // Total
                    break;
                case KeyviewerStyle.Key20:
                    for (int i = 0; i < 8; i++)
                        SetKeyPosition(i, basePosition.x + 54 * i, basePosition.y + 333 - remove);
                    for (int i = 0; i < 8; i++)
                    {
                        int j = BackSequence20[i];
                        SetKeyPosition(j, basePosition.x + 54 * i, basePosition.y + 279 - remove);
                    }
                    SetKeyPosition(16, basePosition.x + 81 + 54, basePosition.y + 225 - remove);
                    SetKeyPosition(17, basePosition.x + 81, basePosition.y + 225 - remove);
                    SetKeyPosition(18, basePosition.x + 54 * 4, basePosition.y + 225 - remove);
                    SetKeyPosition(19, basePosition.x + 54 * 4 + 81, basePosition.y + 225 - remove);
                    SetKeyPosition(-1, basePosition.x + 0, basePosition.y + 225 - remove); // KPS
                    SetKeyPosition(-2, basePosition.x + 81 + 54 * 5, basePosition.y + 225 - remove); // Total
                    break;
                case KeyviewerStyle.Key8:
                    for (int i = 0; i < 8; i++)
                        SetKeyPosition(i, basePosition.x + 54 * i, basePosition.y + 279 - remove);
                    SetKeyPosition(-1, basePosition.x + 0, basePosition.y + 233 - remove); // KPS
                    SetKeyPosition(-2, basePosition.x + 216, basePosition.y + 233 - remove); // Total
                    break;
                case KeyviewerStyle.Key10:
                    for (int i = 0; i < 8; i++)
                        SetKeyPosition(i, basePosition.x + 54 * i, basePosition.y + 279 - remove);
                    SetKeyPosition(8, basePosition.x + 81, basePosition.y + 225 - remove);
                    SetKeyPosition(9, basePosition.x + 54 * 4, basePosition.y + 225 - remove);
                    SetKeyPosition(-1, basePosition.x + 0, basePosition.y + 225 - remove); // KPS
                    SetKeyPosition(-2, basePosition.x + 81 + 54 * 5, basePosition.y + 225 - remove); // Total
                    break;
            }
        }
        private void ResetFootKeyViewerPosition()
        {
            if (Keys == null || !Settings.CustomPositionEnabled) return;
            Vector2 basePosition = Settings.FootKeyViewerPosition;
            // 重新设置所有脚键的位置
            int size = 0;
            switch (Settings.FootKeyViewerStyle)
            {
                case FootKeyviewerStyle.Key2: size = 2; break;
                case FootKeyviewerStyle.Key4: size = 4; break;
                case FootKeyviewerStyle.Key6: size = 6; break;
                case FootKeyviewerStyle.Key8: size = 8; break;
                case FootKeyviewerStyle.Key10: size = 10; break;
                case FootKeyviewerStyle.Key12: size = 12; break;
                case FootKeyviewerStyle.Key14: size = 14; break;
                case FootKeyviewerStyle.Key16: size = 16; break;
                default: return;
            }
            int firstRowCount = size <= 8 ? size : 8;
            float yBase = size > 8 ? basePosition.y + 34 : basePosition.y;
            for (int i = 20; i < 20 + size; i++)
            {
                int offset = i - 20;
                if (offset < firstRowCount)
                {
                    SetKeyPosition(i, basePosition.x + offset * 34, yBase);
                }
                else
                {
                    int col = offset - firstRowCount;
                    float x = basePosition.x + col * 34 + (firstRowCount - (size - firstRowCount)) * 17;
                    SetKeyPosition(i, x, yBase - 34);
                }
            }
        }
        private void SetKeyPosition(int keyIndex, float x, float y)
        {
            if (keyIndex == -1 && Kps != null)
            {
                Kps.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
            }
            else if (keyIndex == -2 && Total != null)
            {
                Total.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
            }
            else if (keyIndex >= 0 && keyIndex < Keys.Length && Keys[keyIndex] != null)
            {
                Keys[keyIndex].transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
            }
        }
        #endregion
        #region 颜色管理
        private Color GetColorByIndex(int index)
        {
            return index switch
            {
                0 => Settings.Background,
                1 => Settings.BackgroundClicked,
                2 => Settings.Outline,
                3 => Settings.OutlineClicked,
                4 => Settings.Text,
                5 => Settings.TextClicked,
                6 => Settings.RainColor,
                7 => Settings.RainColor2,
                8 => Settings.RainColor3,
                _ => Color.white
            };
        }
        private void SetColorByIndex(int index, Color color)
        {
            switch (index)
            {
                case 0: Settings.Background = color; break;
                case 1: Settings.BackgroundClicked = color; break;
                case 2: Settings.Outline = color; break;
                case 3: Settings.OutlineClicked = color; break;
                case 4: Settings.Text = color; break;
                case 5: Settings.TextClicked = color; break;
                case 6: Settings.RainColor = color; break;
                case 7: Settings.RainColor2 = color; break;
                case 8: Settings.RainColor3 = color; break;
            }
        }
        private void UpdateAllKeyColors()
        {
            if (Keys == null) return;
            KeyCode[] keyCodes = GetKeyCode();
            KeyCode[] footKeyCodes = GetFootKeyCode();
            for (int i = 0; i < keyCodes.Length && i < Keys.Length; i++)
            {
                if (Keys[i] != null)
                {
                    Keys[i].background.color = Settings.Background;
                    Keys[i].outline.color = Settings.Outline;
                    Keys[i].text.color = Settings.Text;
                    if (Keys[i].value != null) Keys[i].value.color = Settings.Text;
                }
            }
            if (footKeyCodes != null)
            {
                for (int i = 0; i < footKeyCodes.Length; i++)
                {
                    int index = i + 20;
                    if (index < Keys.Length && Keys[index] != null)
                    {
                        Keys[index].background.color = Settings.Background;
                        Keys[index].outline.color = Settings.Outline;
                        Keys[index].text.color = Settings.Text;
                        if (Keys[index].value != null) Keys[index].value.color = Settings.Text;
                    }
                }
            }
            if (Kps != null)
            {
                Kps.background.color = Settings.Background;
                Kps.outline.color = Settings.Outline;
                Kps.text.color = Settings.Text;
                if (Kps.value != null) Kps.value.color = Settings.Text;
            }
            if (Total != null)
            {
                Total.background.color = Settings.Background;
                Total.outline.color = Settings.Outline;
                Total.text.color = Settings.Text;
                if (Total.value != null) Total.value.color = Settings.Text;
            }
        }
        #endregion
        #region 按键监听和选择
        private void StartKeySelection()
        {
            WinAPICool = 0;
            KeyPressed = new bool[256];
            for (int i = 0; i < 256; i++)
            {
                // 仅在选择新按键时使用 WinAPI 获取当前状态，避免在普通监听中使用
                KeyPressed[i] = (GetAsyncKeyState(i) & 0x8000) != 0;
            }
        }
        private void ProcessKeySelection()
        {
            if (SelectedKey == -1 || TextChanged || !Application.isFocused) return;
            // 检查普通 KeyCode (Unity API)
            if (Input.anyKeyDown)
            {
                foreach (KeyCode keyCode in AllKeyCodes)
                {
                    if (Input.GetKeyDown(keyCode))
                    {
                        SetupKey(keyCode);
                        return; // 找到一个就返回
                    }
                }
            }
            // 检查非 KeyCode (如鼠标按钮) 使用 WinAPI
            else
            {
                for (int i = 0; i < 256; i++)
                {
                    bool currentPressed = (GetAsyncKeyState(i) & 0x8000) != 0;
                    if (currentPressed == KeyPressed[i]) continue;
                    if (KeyPressed[i]) // 之前按下，现在释放
                    {
                        KeyPressed[i] = false;
                        WinAPICool = 0;
                        continue;
                    }
                    else if (WinAPICool++ >= 6) // 之前释放，现在按下，并且冷却足够
                    {
                        KeyCode keyCode = (KeyCode)(i + 0x1000); // 与原代码逻辑一致
                        SetupKey(keyCode);
                        return; // 找到一个就返回
                    }
                }
            }
        }
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
            WinAPICool = 0;
            KeyPressed = null;
            SaveSettings();
        }
        #endregion
        #region KeyViewer 核心功能
        private void EnableKeyViewer()
        {
            if (KeyViewerObject != null || !Settings.Enabled) return;
            // 确保资源已加载 *每次调用 EnableKeyViewer 时都检查*
            if (!TryLoadResources())
            {
                Debug.LogError("KeyViewer: 无法加载必要的资源 (KeyBackground, KeyOutline, Maplestory-OTF-Bold SDF)。请检查 Assets/Resources/kv/ 和 Assets/Resources/Maplefont/ 文件夹。");
                return; // 如果资源加载失败，则不创建UI
            }
            KeyViewerObject = new GameObject("JipperResourcePack KeyViewer");
            Canvas = KeyViewerObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = Canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            Canvas.gameObject.AddComponent<GraphicRaycaster>();
            KeyViewerSizeObject = new GameObject("SizeObject");
            RectTransform rectTransform = KeyViewerSizeObject.AddComponent<RectTransform>();
            rectTransform.SetParent(KeyViewerObject.transform);
            rectTransform.localScale = new Vector3(Settings.Size, Settings.Size, 1); // 应用初始大小设置
            rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.offsetMin = rectTransform.offsetMax = Vector2.zero;
            Keys = new Key[36]; // 初始化数组 (20 main + 16 foot)
            switch (Settings.KeyViewerStyle)
            {
                case KeyviewerStyle.Key12:
                    Initialize12KeyViewer();
                    break;
                case KeyviewerStyle.Key16:
                    Initialize16KeyViewer();
                    break;
                case KeyviewerStyle.Key20:
                    Initialize20KeyViewer();
                    break;
                case KeyviewerStyle.Key8:
                    Initialize8KeyViewer();
                    break;
                case KeyviewerStyle.Key10:
                    Initialize10KeyViewer();
                    break;
            }
            switch (Settings.FootKeyViewerStyle)
            {
                case FootKeyviewerStyle.Key2:
                    InitializeFootKeyViewer(2);
                    break;
                case FootKeyviewerStyle.Key4:
                    InitializeFootKeyViewer(4);
                    break;
                case FootKeyviewerStyle.Key6:
                    InitializeFootKeyViewer(6);
                    break;
                case FootKeyviewerStyle.Key8:
                    InitializeFootKeyViewer(8);
                    break;
                case FootKeyviewerStyle.Key10:
                    InitializeFootKeyViewer(10);
                    break;
                case FootKeyviewerStyle.Key12:
                    InitializeFootKeyViewer(12);
                    break;
                case FootKeyviewerStyle.Key14:
                    InitializeFootKeyViewer(14);
                    break;
                case FootKeyviewerStyle.Key16:
                    InitializeFootKeyViewer(16);
                    break;
            }
            Object.DontDestroyOnLoad(KeyViewerObject);
            PressTimes = new Queue<long>();
            Stopwatch = Stopwatch.StartNew();
        }
        private void DisableKeyViewer()
        {
            if (KeyViewerObject == null) return;
            Object.Destroy(KeyViewerObject);
            KeyViewerObject = null;
            KeyViewerSizeObject = null;
            while (rainPool.Count > 0) Object.Destroy(rainPool.Pop().gameObject);
            Canvas = null;
            Keys = null;
            PressTimes = null;
            Stopwatch = null;
        }
        // 在 Update 中直接处理主按键和脚键
        private void ProcessMainAndFootKeysInUpdate()
        {
            long elapsedMilliseconds = Stopwatch.ElapsedMilliseconds;
            // Cache key code arrays — only refresh when layout changes
            if (cachedKeyStyle != Settings.KeyViewerStyle)
            {
                cachedMainKeys = GetKeyCode();
                cachedKeyStyle = Settings.KeyViewerStyle;
            }
            if (cachedFootStyle != Settings.FootKeyViewerStyle)
            {
                cachedFootKeys = GetFootKeyCode();
                cachedFootStyle = Settings.FootKeyViewerStyle;
            }
            ProcessKeyGroup(cachedMainKeys, 0, elapsedMilliseconds);
            if (cachedFootKeys != null)
                ProcessKeyGroup(cachedFootKeys, 20, elapsedMilliseconds);
            if (Total != null && Total.value != null && lastTotal != Settings.TotalCount)
            {
                lastTotal = Settings.TotalCount;
                Total.value.text = lastTotal.ToString();
            }
        }
        private void ProcessKeyRainQueues()
        {
            if (Keys == null) return;
            for (int i = 0; i < Keys.Length; i++)
            {
                if (Keys[i] != null)
                    Keys[i].ProcessRainQueue();
            }
        }
        private void ProcessKeyGroup(KeyCode[] keyCodes, int baseIndex, long elapsedMs)
        {
            for (int i = 0; i < keyCodes.Length; i++)
            {
                int idx = baseIndex + i;
                if (idx >= Keys.Length || Keys[idx] == null) continue;
                bool current = Input.GetKey(keyCodes[i]);
                if (current != Keys[idx].isPressed)
                {
                    UpdateKey(idx, current);
                    Keys[idx].isPressed = current;
                    if (current)
                    {
                        Settings.Count[idx]++;
                        Settings.TotalCount++;
                        if (Keys[idx].value != null)
                            Keys[idx].value.text = Settings.Count[idx].ToString();
                        PressTimes.Enqueue(elapsedMs);
                        Save = true;
                        if (Settings.EnableRainEffect)
                            TriggerRainEffect(idx, Keys[idx]);
                    }
                }
            }
        }
        private void ProcessKpsAndSaveInUpdate()
        {
            // 更新KPS
            if (PressTimes != null)
            {
                long elapsedMilliseconds = Stopwatch.ElapsedMilliseconds;
                while (PressTimes.Count > 0 && elapsedMilliseconds - PressTimes.Peek() > 1000)
                {
                    PressTimes.Dequeue();
                }
                int currentKps = PressTimes.Count;
                if (lastKps != currentKps)
                {
                    lastKps = currentKps;
                    if (Kps != null && Kps.value != null) Kps.value.text = currentKps.ToString();
                }
            }
            // 保存设置 (检查Save标志)
            if (Save && enabled) // 确保组件启用
            {
                SaveSettings();
                Save = false; // 重置标志
            }
        }
        // UpdateKey 现在只负责更新颜色，安全地在主线程调用
        private void UpdateKey(int i, bool pressed)
        {
            if (Keys == null || i >= Keys.Length || Keys[i] == null) return;
            Key key = Keys[i];
            // 直接设置颜色
            key.background.color = pressed ? Settings.BackgroundClicked : Settings.Background;
            key.outline.color = pressed ? Settings.OutlineClicked : Settings.Outline;
            key.text.color = pressed ? Settings.TextClicked : Settings.Text;
            if (key.value != null) key.value.color = key.text.color;
        }
        #endregion
        private void ClearAllRains()
        {
            if (Keys == null) return;
            foreach (var key in Keys)
            {
                if (key == null) continue;
                while (key.rawRainQueue.Count > 0)
                {
                    var rain = key.rawRainQueue.Dequeue();
                    if (rain != null && rain.transform != null && rain.transform.gameObject != null)
                        Destroy(rain.transform.gameObject);
                }
                foreach (var rain in key.rainList)
                    rain.removed = true;
                key.rainList.Clear();
            }
        }

        private bool IsRainEnabledForKey(int keyIndex)
        {
            if (keyIndex < 8) return Settings.EnableRainForRow1; // 第一排
            if (keyIndex < 16) return Settings.EnableRainForRow2; // 第二排
            if (keyIndex < 20) return Settings.EnableRainForRow3; // 第三排
            return false; // 脚键不下雨
        }

        public Rain GetRainFromPool(Transform parent)
        {
            Rain r;
            if (rainPool.Count > 0)
            {
                r = rainPool.Pop();
                r.Init(parent);
            }
            else
            {
                GameObject go = new GameObject("Rain");
                go.AddComponent<RectTransform>();
                r = go.AddComponent<Rain>();
                r.Init(parent);
            }
            return r;
        }

        public void ReturnRain(Rain r)
        {
            r.gameObject.SetActive(false);
            r.rawRain = null;
            r.transform.SetParent(null);
            rainPool.Push(r);
        }
        #region 按键布局初始化
        private void Initialize12KeyViewer()
        {
            int remove = Settings.DownLocation ? 200 : 0;
            for (int i = 0; i < 8; i++) Keys[i] = CreateKey(i, 54 * i, 279 - remove, 50, 0); // raining = 0, should have rainline
            Keys[8] = CreateKey(8, 81 + 54, 225 - remove, 77, 1); // raining = 1, should NOT have rainline
            Keys[9] = CreateKey(9, 81, 225 - remove, 50, 1); // raining = 1, should NOT have rainline
            Keys[10] = CreateKey(10, 54 * 4, 225 - remove, 77, 1); // raining = 1, should NOT have rainline
            Keys[11] = CreateKey(11, 54 * 4 + 81, 225 - remove, 50, 1); // raining = 1, should NOT have rainline
            Kps = CreateKey(-1, 0, 225 - remove, 77, -1); // raining = -1, should NOT have rainline
            Total = CreateKey(-2, 81 + 54 * 5, 225 - remove, 77, -1); // raining = -1, should NOT have rainline
        }
        private void Initialize16KeyViewer()
        {
            int remove = Settings.DownLocation ? 200 : 0;
            for (int i = 0; i < 8; i++) Keys[i] = CreateKey(i, 54 * i, 320 - remove, 50, 0); // raining = 0, should have rainline
            for (int i = 0; i < 8; i++)
            {
                int j = BackSequence16[i];
                Keys[j] = CreateKey(j, 54 * i, 266 - remove, 50, 1); // raining = 1, should NOT have rainline
            }
            Kps = CreateKey(-1, 0, 220 - remove, 212, -1, true); // raining = -1, should NOT have rainline
            Total = CreateKey(-2, 216, 220 - remove, 212, -1, true); // raining = -1, should NOT have rainline
        }
        private void Initialize20KeyViewer()
        {
            int remove = Settings.DownLocation ? 200 : 0;
            for (int i = 0; i < 8; i++) Keys[i] = CreateKey(i, 54 * i, 333 - remove, 50, 0); // raining = 0, should have rainline
            for (int i = 0; i < 8; i++)
            {
                int j = BackSequence20[i];
                Keys[j] = CreateKey(j, 54 * i, 279 - remove, 50, 1); // raining = 1, should NOT have rainline
            }
            Keys[16] = CreateKey(16, 81 + 54, 225 - remove, 77, 3); // raining = 3, should have rainline
            Keys[17] = CreateKey(17, 81, 225 - remove, 50, 3); // raining = 3, should have rainline
            Keys[18] = CreateKey(18, 54 * 4, 225 - remove, 77, 3); // raining = 3, should have rainline
            Keys[19] = CreateKey(19, 54 * 4 + 81, 225 - remove, 50, 3); // raining = 3, should have rainline
            Kps = CreateKey(-1, 0, 225 - remove, 77, -1); // raining = -1, should NOT have rainline
            Total = CreateKey(-2, 81 + 54 * 5, 225 - remove, 77, -1); // raining = -1, should NOT have rainline
        }
        private void Initialize8KeyViewer()
        {
            int remove = Settings.DownLocation ? 200 : 0;
            for (int i = 0; i < 8; i++) Keys[i] = CreateKey(i, 54 * i, 279 - remove, 50, 0);
            Kps = CreateKey(-1, 0, 233 - remove, 212, -1, true);
            Total = CreateKey(-2, 216, 233 - remove, 212, -1, true);
        }
        private void Initialize10KeyViewer()
        {
            int remove = Settings.DownLocation ? 200 : 0;
            for (int i = 0; i < 8; i++) Keys[i] = CreateKey(i, 54 * i, 279 - remove, 50, 0); // raining = 0, should have rainline
            Keys[8] = CreateKey(8, 81, 225 - remove, 129, 1); // raining = 1, should NOT have rainline
            Keys[9] = CreateKey(9, 54 * 4, 225 - remove, 129, 1); // raining = 1, should NOT have rainline
            Kps = CreateKey(-1, 0, 225 - remove, 77, -1); // raining = -1, should NOT have rainline
            Total = CreateKey(-2, 81 + 54 * 5, 225 - remove, 77, -1); // raining = -1, should NOT have rainline
        }
        private void InitializeFootKeyViewer(int size)
        {
            for (int i = 20; i < 20 + size; i++)
            {
                int col;
                int row;
                if (size <= 8)
                {
                    col = i - 20;
                    row = 0;
                }
                else
                {
                    if (i - 20 < 8)
                    {
                        col = i - 20;
                        row = 0;
                    }
                    else
                    {
                        col = (i - 20) - 8;
                        row = 1;
                    }
                }
                // 超过8键时整体上移一行，使底行对齐原位置
                int baseY = size > 8 ? 15 + 34 : 15;
                int x = 432 + col * 34;
                if (size > 8 && row == 1)
                    x += (8 - (size - 8)) * 17;
                int y = baseY - row * 34;
                Keys[i] = CreateKey(i, x, y, 30, -1, true, false);
            }
        }
        private Key CreateKey(int i, float x, float y, float sizeX, int raining, bool slim = false, bool count = true)
        {
            // 资源未加载时 fallback 到纯色块
            if (defaultFont == null)
                defaultFont = GetCurrentFont();
            GameObject obj = new("Key " + i);
            KeyViewerSettings settings = Settings;
            RectTransform transform = obj.AddComponent<RectTransform>();
            transform.SetParent(KeyViewerSizeObject.transform);
            transform.sizeDelta = new Vector2(sizeX, slim ? 30 : 50);
            transform.anchorMin = transform.anchorMax = Vector2.zero;
            transform.pivot = new Vector2(0, 0.5f);
            transform.anchoredPosition = new Vector2(x, y);
            transform.localScale = Vector3.one;
            Key key = obj.AddComponent<Key>();
            key.isPressed = false; // 初始化为未按下状态
            GameObject gameObject;
            Image image;
            TextMeshProUGUI text;
            // Background
            gameObject = new GameObject("Background");
            transform = gameObject.AddComponent<RectTransform>();
            transform.SetParent(obj.transform);
            transform.anchorMin = transform.anchorMax = transform.pivot = Vector2.zero;
            transform.anchoredPosition = Vector2.zero;
            transform.sizeDelta = new Vector2(sizeX * 2, (slim ? 30 : 50) * 2);
            transform.localScale = new Vector3(0.5f, 0.5f);
            image = gameObject.AddComponent<Image>();
            image.color = settings.Background;
            if (keyBackgroundSprite != null)
            {
                image.sprite = keyBackgroundSprite;
                image.type = Image.Type.Sliced;
            }
            image.raycastTarget = false;
            key.background = image;
            // Outline
            gameObject = new GameObject("Outline");
            transform = gameObject.AddComponent<RectTransform>();
            transform.SetParent(obj.transform);
            transform.anchorMin = transform.anchorMax = transform.pivot = Vector2.zero;
            transform.anchoredPosition = Vector2.zero;
            transform.sizeDelta = new Vector2(sizeX * 2, (slim ? 30 : 50) * 2);
            transform.localScale = new Vector3(0.5f, 0.5f);
            image = gameObject.AddComponent<Image>();
            image.color = settings.Outline;
            if (keyOutlineSprite != null)
            {
                image.sprite = keyOutlineSprite;
                image.type = Image.Type.Sliced;
            }
            image.raycastTarget = false;
            key.outline = image;
            // KeyText
            gameObject = new GameObject("KeyText");
            transform = gameObject.AddComponent<RectTransform>();
            transform.SetParent(obj.transform);
            if (slim)
            {
                transform.sizeDelta = new Vector2(sizeX / 2, 30);
                transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0, 0.5f);
                transform.anchoredPosition = new Vector2(count ? 10 : 7.5f, 0);
            }
            else
            {
                transform.sizeDelta = new Vector2(sizeX - 4, 32);
                transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 1);
                transform.anchoredPosition = new Vector2(0, 2);
            }
            transform.localScale = Vector3.one;
            text = gameObject.AddComponent<TextMeshProUGUI>();
            text.font = GetCurrentFont();
            text.fontMaterial = GetShadowMaterial(text.font);
            text.enableAutoSizing = true;
            text.fontSizeMin = 0;
            text.fontSizeMax = 20;
            text.alignment = slim ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
            text.color = settings.Text;
            text.raycastTarget = false;
            key.text = text;
            // CountText (if applicable)
            if (count) // 只有 count 为 true 时才创建计数文本
            {
                gameObject = new GameObject("CountText");
                transform = gameObject.AddComponent<RectTransform>();
                transform.SetParent(obj.transform);
                if (slim)
                {
                    transform.sizeDelta = new Vector2(sizeX / 2, 30);
                    transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(1, 0.5f);
                    transform.anchoredPosition = new Vector2(-10, 0);
                }
                else
                {
                    transform.sizeDelta = new Vector2(sizeX - 4, 16);
                    transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 0);
                    transform.anchoredPosition = new Vector2(0, 2);
                }
                transform.localScale = Vector3.one;
                text = gameObject.AddComponent<TextMeshProUGUI>();
                text.font = GetCurrentFont();
                text.fontMaterial = GetShadowMaterial(text.font);
                text.enableAutoSizing = true;
                text.fontSizeMin = 0;
                text.fontSizeMax = 20;
                text.raycastTarget = false;
                text.alignment = slim ? TextAlignmentOptions.Right : TextAlignmentOptions.Top;
                key.value = text;
            }
            // UpdateKeyText 在 CreateKey 末尾调用，以设置初始文本
            UpdateKeyText(key, i);
            // RainLine Logic: Only create RainLine if raining is 0, 2, or 3
            if (raining >= 0) // 只有当 raining >= 0 时才创建雨线 (0, 1, 2, 3)
            {
                // 确保 key.rain 存在
                if (key.rain == null)
                {
                    key.rain = new GameObject("RainLine");
                    transform = key.rain.AddComponent<RectTransform>();
                    transform.SetParent(obj.transform);
                    transform.sizeDelta = new Vector2(sizeX, 275);
                    transform.anchorMin = transform.anchorMax = transform.pivot = Vector2.zero;
                    transform.anchoredPosition = new Vector2(0, raining switch
                    {
                        0 => -223, // 第一排
                        3 => -115, // 第三排 (注意：这里的值可能需要根据实际布局调整)
                        _ => -169 // 其他情况 (默认或第二排)
                    });
                    transform.localScale = Vector3.one;
                }
                // 设置颜色索引
                key.color = (byte)raining;
            }
            else
            {
                // 不需要雨线
                key.color = 1; // 默认颜色索引
                               // 可以选择销毁或隐藏雨线对象，但通常保持引用即可
                               // Object.Destroy(key.rain); // 如果你想完全移除
                               // 或者
                key.rain?.SetActive(false); // 如果你想暂时隐藏
                key.rain = null;
            }
            return key; // 成功创建并返回 Key 组件
        }
        #endregion
        #region 工具方法
        private static void UpdateKeyText(Key key, int i)
        {
            if (key == null) return;
            if (i == -1) // KPS
            {
                key.text.text = "KPS";
                key.value.text = 0f.ToString();
                return;
            }
            if (i == -2) // Total
            {
                key.text.text = "Total";
                key.value.text = 0f.ToString();
                return;
            }
            // 主按键 (0-19)
            if (i < 20)
            {
                KeyCode[] keyCodes = GetKeyCode();
                string[] keyTexts = GetKeyText();
                // 防御性检查，确保数组存在且索引有效
                if (keyCodes != null && keyTexts != null && i < keyCodes.Length && i < keyTexts.Length)
                {
                    string displayText = !string.IsNullOrEmpty(keyTexts[i]) ? keyTexts[i] : KeyToString(keyCodes[i]);
                    key.text.text = displayText;
                    key.value.text = Settings.Count[i].ToString();
                }
            }
            else // 脚键 (20+)
            {
                KeyCode[] footKeyCodes = GetFootKeyCode();
                int footIndex = i - 20;
                if (footKeyCodes != null && footIndex >= 0 && footIndex < footKeyCodes.Length)
                {
                    key.text.text = KeyToString(footKeyCodes[footIndex]);
                }
            }
        }

        void ScanGameFonts()
        {
            var gameFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (gameFonts == null) return;
            foreach (var f in gameFonts)
            {
                // 防止重复添加
                if (!fontList.Exists(e => e.font == f))
                    fontList.Add(new FontEntry(f.name, f));
            }
            Debug.Log($"KeyViewer: 扫描到 {fontList.Count} 个字体");
        }

        private bool TryLoadResources()
        {
            if (keyBackgroundSprite != null) return true;

            fontList.Clear();

            string modPath = Path.GetDirectoryName(Main.Mod?.Path);
            string bundlePath = Path.Combine(modPath ?? ".", "assets", "keyviewer_resources");

            var bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle != null)
            {
                keyBackgroundSprite = bundle.LoadAsset<Sprite>("KeyBackground");
                keyOutlineSprite = bundle.LoadAsset<Sprite>("KeyOutline");

                Font mapleOTF = bundle.LoadAsset<Font>("MAPLESTORY_OTF_BOLD");
                if (mapleOTF != null)
                {
                    mapleFont = TMP_FontAsset.CreateFontAsset(mapleOTF);
                    fontList.Add(new FontEntry("MapleStory", mapleFont));
                }

                Font cjkOTF = bundle.LoadAsset<Font>("cjkFonts-regular-normalized");
                if (cjkOTF != null)
                {
                    var cjkFont = TMP_FontAsset.CreateFontAsset(cjkOTF);
                    fontList.Insert(0, new FontEntry("CJK (预设)", cjkFont));
                }

                if (keyBackgroundSprite == null)
                    Debug.LogError("KeyViewer: AssetBundle 中未找到 KeyBackground");
                if (keyOutlineSprite == null)
                    Debug.LogError("KeyViewer: AssetBundle 中未找到 KeyOutline");

                bundle.Unload(false);
            }
            else
            {
                Debug.LogError($"KeyViewer: 无法加载 AssetBundle，路径: {bundlePath}");
            }

            if (Settings.FontIndex >= fontList.Count)
                Settings.FontIndex = 0;

            return true;
        }
        private TMP_FontAsset GetCurrentFont()
        {
            return fontList.Count > 0 ? fontList[Mathf.Clamp(Settings.FontIndex, 0, fontList.Count - 1)].font : null;
        }
        private static KeyCode[] GetKeyCode()
        {
            return Settings.KeyViewerStyle switch
            {
                KeyviewerStyle.Key8 => Settings.key8,
                KeyviewerStyle.Key12 => Settings.key12,
                KeyviewerStyle.Key16 => Settings.key16,
                KeyviewerStyle.Key20 => Settings.key20,
                KeyviewerStyle.Key10 => Settings.key10,
                _ => Settings.key16
            };
        }
        private static KeyCode[] GetFootKeyCode()
        {
            return Settings.FootKeyViewerStyle switch
            {
                FootKeyviewerStyle.Key2 => Settings.footkey2,
                FootKeyviewerStyle.Key4 => Settings.footkey4,
                FootKeyviewerStyle.Key6 => Settings.footkey6,
                FootKeyviewerStyle.Key8 => Settings.footkey8,
                FootKeyviewerStyle.Key10 => Settings.footkey10,
                FootKeyviewerStyle.Key12 => Settings.footkey12,
                FootKeyviewerStyle.Key14 => Settings.footkey14,
                FootKeyviewerStyle.Key16 => Settings.footkey16,
                _ => new KeyCode[0]
            };
        }
        private static string[] GetKeyText()
        {
            return Settings.KeyViewerStyle switch
            {
                KeyviewerStyle.Key8 => Settings.key8Text,
                KeyviewerStyle.Key12 => Settings.key12Text,
                KeyviewerStyle.Key16 => Settings.key16Text,
                KeyviewerStyle.Key20 => Settings.key20Text,
                KeyviewerStyle.Key10 => Settings.key10Text,
                _ => Settings.key16Text
            };
        }
        private static byte[] GetBackSequence()
        {
            return Settings.KeyViewerStyle switch
            {
                KeyviewerStyle.Key8 => BackSequence8,
                KeyviewerStyle.Key12 => BackSequence12,
                KeyviewerStyle.Key16 => BackSequence16,
                KeyviewerStyle.Key20 => BackSequence20,
                KeyviewerStyle.Key10 => BackSequence10,
                _ => BackSequence16
            };
        }
        private void ChangeKeyViewer()
        {
            currentKeyViewerStyle = Settings.KeyViewerStyle;
            ResetKeyViewer();
        }
        private void ResetKeyViewer()
        {
            SelectedKey = -1;
            // 销毁现有的按键
            if (Keys != null)
            {
                for (int i = 0; i < 20; i++) // Only destroy main keys (0-19)
                {
                    if (Keys[i] != null && Keys[i].gameObject != null)
                    {
                        Object.DestroyImmediate(Keys[i].gameObject);
                    }
                }
                if (Total != null && Total.gameObject != null)
                {
                    Object.DestroyImmediate(Total.gameObject);
                }
                if (Kps != null && Kps.gameObject != null)
                {
                    Object.DestroyImmediate(Kps.gameObject);
                }
            }
            // 重新初始化
            switch (Settings.KeyViewerStyle)
            {
                case KeyviewerStyle.Key12:
                    Initialize12KeyViewer();
                    break;
                case KeyviewerStyle.Key16:
                    Initialize16KeyViewer();
                    break;
                case KeyviewerStyle.Key20:
                    Initialize20KeyViewer();
                    break;
                case KeyviewerStyle.Key8:
                    Initialize8KeyViewer();
                    break;
                case KeyviewerStyle.Key10:
                    Initialize10KeyViewer();
                    break;
            }
            // 应用自定义位置
            if (Settings.CustomPositionEnabled)
            {
                ResetKeyViewerPosition();
            }
        }
        private void ResetFootKeyViewer()
        {
            if (Keys != null)
            {
                for (int i = 20; i < 36; i++) // Destroy foot keys (20-35)
                {
                    if (Keys[i] != null && Keys[i].gameObject != null)
                    {
                        Object.DestroyImmediate(Keys[i].gameObject);
                    }
                }
            }
            switch (Settings.FootKeyViewerStyle)
            {
                case FootKeyviewerStyle.Key2:
                    InitializeFootKeyViewer(2);
                    break;
                case FootKeyviewerStyle.Key4:
                    InitializeFootKeyViewer(4);
                    break;
                case FootKeyviewerStyle.Key6:
                    InitializeFootKeyViewer(6);
                    break;
                case FootKeyviewerStyle.Key8:
                    InitializeFootKeyViewer(8);
                    break;
                case FootKeyviewerStyle.Key10:
                    InitializeFootKeyViewer(10);
                    break;
                case FootKeyviewerStyle.Key12:
                    InitializeFootKeyViewer(12);
                    break;
                case FootKeyviewerStyle.Key14:
                    InitializeFootKeyViewer(14);
                    break;
                case FootKeyviewerStyle.Key16:
                    InitializeFootKeyViewer(16);
                    break;
            }
            // 应用自定义位置
            if (Settings.CustomPositionEnabled)
            {
                ResetFootKeyViewerPosition();
            }
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        public static string KeyToString(KeyCode keyCode)
        {
            string keyString = keyCode.ToString();
            if (keyString.StartsWith("Alpha")) keyString = keyString.Substring(5);
            if (keyString.StartsWith("Keypad")) keyString = keyString.Substring(6);
            if (keyString.StartsWith("Left")) keyString = 'L' + keyString.Substring(4);
            if (keyString.StartsWith("Right")) keyString = 'R' + keyString.Substring(5);
            if (keyString.EndsWith("Shift")) keyString = keyString.Substring(0, keyString.Length - 5) + "⇧";
            if (keyString.EndsWith("Control")) keyString = keyString.Substring(0, keyString.Length - 7) + "Ctrl";
            if (keyString.StartsWith("Mouse")) keyString = "M" + keyString.Substring(5);
            return keyString switch
            {
                "Plus" => "+",
                "Minus" => "-",
                "Multiply" => "*",
                "Divide" => "/",
                "Enter" => "↵",
                "Equals" => "=",
                "Period" => ".",
                "Return" => "↵",
                "None" => " ",
                "Tab" => "⇥",
                "Backslash" => "\\",
                "Backspace" => "Back",
                "Slash" => "/",
                "LBracket" => "[",
                "RBracket" => "]",
                "Semicolon" => ";",
                "Comma" => ",",
                "Quote" => "'",
                "UpArrow" => "↑",
                "DownArrow" => "↓",
                "LeftArrow" => "←",
                "RightArrow" => "→",
                "Space" => "␣",
                "BackQuote" => "`",
                "PageDown" => "Pg↓",
                "PageUp" => "Pg↑",
                "CapsLock" => "⇪",
                "Insert" => "Ins",
                _ => keyString
            };
        }
        static GUILayoutOption FloatFieldWidth(string text) => GUILayout.Width(Mathf.Max(30, text.Length * 9));
        #endregion
    }
    #region 辅助类和结构体
    [System.Serializable]
    public class KeyViewerSettings
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
        public bool EnableRainEffect = false;
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

        public Vector2 MainKeyViewerPosition = new Vector2(0, 0);
        public Vector2 FootKeyViewerPosition = new Vector2(432, 15);
        public bool CustomPositionEnabled = false;
        public int FontIndex;
        public string FontName = "";
        public string Language = "zh";
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
    #endregion
}