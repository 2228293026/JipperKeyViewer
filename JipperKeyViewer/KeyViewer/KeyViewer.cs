using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace JipperKeyViewer.KeyViewer
{
    public partial class KeyViewer : MonoBehaviour
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

        static KeyViewer()
        {
            AllKeyCodes = (KeyCode[])Enum.GetValues(typeof(KeyCode));
        }

        // --- Instance fields ---
        GameObject KeyViewerObject;
        GameObject KeyViewerSizeObject;
        Canvas Canvas;
        Key[] Keys;
        Key Kps;
        int lastKps;
        int lastTotal;
        Key Total;
        Queue<long> PressTimes;
        Stopwatch Stopwatch;
        long lastFrameMs;
        const long MAX_DELTA_MS = 50;
        bool KeyChangeExpanded;
        bool TextChangeExpanded;
        bool[] ColorExpanded;
        KeyviewerStyle currentKeyViewerStyle;
        bool[] KeyPressed;
        int SelectedKey = -1;
        int WinAPICool;
        bool TextChanged;

        static string ConfigPath
        {
            get
            {
                if (configPath == null)
                {
                    string modPath = Path.GetDirectoryName(Main.Mod?.Path);
                    configPath = Path.Combine(modPath ?? Application.persistentDataPath, "config", "settings.json");
                }
                return configPath;
            }
        }
        static string configPath;

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
        private bool fontRestored;
        private readonly float[] rowSpeeds = new float[3];
        private readonly float[] rowHeights = new float[3];

        // --- Unity lifecycle ---
        void Awake()
        {
            instance = this;
            LoadSettings();
            I18n.Load();
            I18n.Lang = Settings.Language;
            currentKeyViewerStyle = Settings.KeyViewerStyle;
            TryLoadResources();
            wasEnabled = Settings.Enabled;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void TryRestoreFont()
        {
            if (fontRestored || string.IsNullOrEmpty(Settings.FontName)) return;
            string fontName = Settings.FontName;
            for (int i = 0; i < fontList.Count; i++)
            {
                if (fontList[i].name == fontName)
                {
                    Settings.FontIndex = i;
                    UpdateAllFonts();
                    SaveSettings();
                    fontRestored = true;
                    //Main.Mod.Logger.Log($"KeyViewer: \u5DF2\u6062\u590D\u5B57\u4F53 {fontName}");
                    return;
                }
            }
            ScanGameFonts();
            for (int i = 0; i < fontList.Count; i++)
            {
                if (fontList[i].name == fontName)
                {
                    Settings.FontIndex = i;
                    UpdateAllFonts();
                    SaveSettings();
                    fontRestored = true;
                    //Main.Mod.Logger.Log($"KeyViewer: \u5DF2\u6062\u590D\u5B57\u4F53 {fontName}");
                    return;
                }
            }
            ScanCustomFonts();
            for (int i = 0; i < fontList.Count; i++)
            {
                if (fontList[i].name == fontName)
                {
                    Settings.FontIndex = i;
                    UpdateAllFonts();
                    SaveSettings();
                    fontRestored = true;
                    //Main.Mod.Logger.Log($"KeyViewer: \u5DF2\u6062\u590D\u5B57\u4F53 {fontName}");
                    return;
                }
            }
            fontRestored = true; // font not found, stop trying each frame
        }

        void OnEnable()
        {
            if (Settings.Enabled) EnableKeyViewer();
            else DisableKeyViewer();
            if (Settings.CustomPositionEnabled)
            {
                ResetKeyViewerPosition();
                ResetFootKeyViewerPosition();
            }
        }

        void OnDisable()
        {
            DisableKeyViewer();
            SaveSettings();
        }

        void OnDestroy()
        {
            SaveSettings();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            ClearAllRains();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SaveSettings();
            fontRestored = false;
            LinkFallbackFonts();
            ClearAllRainDrops();
            //Main.Mod.Logger.Log($"Scene changed to {scene.name}, saved counts, cleared rain drops");
        }

        void Update()
        {
            TryRestoreFont();
            if (wasEnabled != Settings.Enabled)
            {
                if (Settings.Enabled)
                {
                    EnableKeyViewer();
                    if (Settings.CustomPositionEnabled)
                    {
                        ResetKeyViewerPosition();
                        ResetFootKeyViewerPosition();
                    }
                }
                else DisableKeyViewer();
                wasEnabled = Settings.Enabled;
            }
            if (KeyViewerObject != null && Settings.Enabled)
            {
                long now = Stopwatch.ElapsedMilliseconds;
                ProcessKeySelection();
                ProcessMainAndFootKeysInUpdate(now);
                ProcessKeyRainQueues();
                ProcessKpsInUpdate(now);
                if (Settings.EnableRainEffect) UpdateRainEffects();
            }
        }

        // --- Config management ---
        private void LoadSettings()
        {
            string directory = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    Settings = JsonUtility.FromJson<KeyViewerSettings>(json);
                    if (Settings != null)
                    {
                        // v1→v2: migrate absolute pixel offsets to normalized 0-1 (X: 0=left 1=right, Y: 0=top 1=bottom)
                        if (Settings.Version < 2)
                        {
                            const float refW = 1920f, refH = 1080f;
                            float Clamp01(float v) => v < 0 ? 0 : (v > 1 ? 1 : v);
                            Settings.MainKeyViewerPosition = new Vector2(
                                Clamp01(Settings.MainKeyViewerPosition.x / refW),
                                1f - Clamp01(Settings.MainKeyViewerPosition.y / refH));
                            Settings.FootKeyViewerPosition = new Vector2(
                                Clamp01(Settings.FootKeyViewerPosition.x / refW),
                                1f - Clamp01(Settings.FootKeyViewerPosition.y / refH));
                            Settings.Version = 2;
                        }
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
                    Main.Mod.Logger.Error($"\u52A0\u8F7D\u914D\u7F6E\u6587\u4EF6\u5931\u8D25: {e.Message}");
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
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                string json = JsonUtility.ToJson(Settings, true);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception e)
            {
                Main.Mod.Logger.Error($"\u4FDD\u5B58\u914D\u7F6E\u6587\u4EF6\u5931\u8D25: {e.Message}");
            }
        }
    }
}