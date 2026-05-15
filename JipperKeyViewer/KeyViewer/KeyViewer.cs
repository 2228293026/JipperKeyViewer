using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>
    /// Core mod controller (partial class, split across multiple files) / Mod 核心控制器（分部类，分散在多个文件中）
    /// Manages lifecycle, settings, key overlay, rain effect, and input / 管理生命周期、设置、按键覆盖层、雨滴效果和输入
    /// </summary>
    public partial class KeyViewer : MonoBehaviour
    {
        /// <summary>Global settings instance / 全局设置实例</summary>
        public static KeyViewerSettings Settings;

        // Default color values used as initial settings and reset targets / 默认颜色值，用于初始设置和重置目标
        public static readonly Color Background = new(0.5607843f, 0.2352941f, 1, 0.1960784f);
        public static readonly Color BackgroundClicked = Color.white;
        public static readonly Color Outline = new(0.5529412f, 0.2431373f, 1);
        public static readonly Color OutlineClicked = Color.white;
        public static readonly Color Text = Color.white;
        public static readonly Color TextClicked = Color.black;
        public static readonly Color RainColor = new(0.5137255f, 0.1254902f, 0.858823538f);
        public static readonly Color RainColor2 = Color.white;
        public static readonly Color RainColor3 = Color.magenta;

        // Back-row key index mapping for each layout style / 每种布局样式的后排按键索引映射
        // Each byte array defines which keys go in the second row, in display order / 每个字节数组定义了第二排有哪些按键及其显示顺序
        public static readonly byte[] BackSequence8 = Array.Empty<byte>();
        public static readonly byte[] BackSequence10 = new byte[] { 8, 9 };
        public static readonly byte[] BackSequence12 = new byte[] { 9, 8, 10, 11 };
        public static readonly byte[] BackSequence16 = new byte[] { 12, 13, 9, 8, 10, 11, 14, 15 };
        public static readonly byte[] BackSequence20 = new byte[] { 12, 13, 9, 8, 10, 11, 14, 15, 17, 16, 18, 19 };

        /// <summary>Display names for main key layout selection grid / 主按键布局选择网格的显示名称</summary>
        static readonly string[] KeyLayoutNames = { "12K", "16K", "20K", "10K", "8K" };
        /// <summary>Display names for foot key layout selection grid / 脚键布局选择网格的显示名称</summary>
        static readonly string[] FootKeyLayoutNames = { "Off", "2K", "4K", "6K", "8K", "10K", "12K", "14K", "16K" };

        /// <summary>
        /// Static constructor: pre-compute AllKeyCodes (all non-Joystick keys) for input detection / 静态构造函数：预计算 AllKeyCodes（所有非摇杆按键），用于按键检测
        /// </summary>
        static KeyViewer()
        {
            var all = (KeyCode[])Enum.GetValues(typeof(KeyCode));
            AllKeyCodes = Array.FindAll(all, k => !k.ToString().StartsWith("Joystick"));
        }

        // --- Instance fields ---

        /// <summary>Root canvas GameObject for the key overlay / 按键覆盖层的根画布 GameObject</summary>
        GameObject KeyViewerObject;
        /// <summary>Child GameObject that applies the Size scale transform / 应用大小缩放的子 GameObject</summary>
        GameObject KeyViewerSizeObject;
        /// <summary>The overlay canvas (ScreenSpaceOverlay) / 覆盖层画布</summary>
        Canvas Canvas;
        /// <summary>All key instances (index 0-19 main, 20-35 foot) / 所有按键实例（0-19 主键，20-35 脚键）</summary>
        Key[] Keys;
        /// <summary>KPS display key / KPS 显示按键</summary>
        Key Kps;
        /// <summary>Last frame's KPS value for change detection / 上一帧的 KPS 值，用于变化检测</summary>
        int lastKps;
        /// <summary>Last frame's total count for change detection / 上一帧的总计数，用于变化检测</summary>
        int lastTotal;
        /// <summary>Total count display key / 总计数显示按键</summary>
        Key Total;
        /// <summary>Queue of press timestamps for KPS calculation / 按下时间戳队列，用于 KPS 计算</summary>
        Queue<long> PressTimes;
        /// <summary>High-resolution stopwatch for timing / 用于计时的高精度秒表</summary>
        Stopwatch Stopwatch;
        /// <summary>Timestamp of last frame for delta calculation / 上一帧的时间戳，用于增量计算</summary>
        /// <summary>Whether the key change section in settings is expanded / 设置中按键更改区域是否展开</summary>
        bool KeyChangeExpanded;
        /// <summary>Whether the text change section in settings is expanded / 设置中文本更改区域是否展开</summary>
        bool TextChangeExpanded;
        /// <summary>Per-color-section expanded state in settings / 设置中每个颜色区域的展开状态</summary>
        bool[] ColorExpanded;
        /// <summary>Cached current style for change detection / 缓存的当前样式，用于变化检测</summary>
        KeyviewerStyle currentKeyViewerStyle;
        /// <summary>Currently selected key index for rebinding (-1 = none) / 当前为重新绑定选中的按键索引（-1 = 无）</summary>
        int SelectedKey = -1;
        /// <summary>Whether user is editing text rather than binding a key / 用户是否在编辑文本而非绑定按键</summary>
        bool TextChanged;

        /// <summary>Path to the settings JSON file / 设置 JSON 文件路径</summary>
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

        /// <summary>Cached background sprite loaded from file / 从文件加载的背景精灵</summary>
        Sprite keyBackgroundSprite;
        /// <summary>Cached outline sprite loaded from file / 从文件加载的轮廓精灵</summary>
        Sprite keyOutlineSprite;
        /// <summary>Singleton instance reference / 单例实例引用</summary>
        public static KeyViewer instance;
        /// <summary>Rain effect system (object-pooled, zero-GC on hot path) / 雨滴效果系统</summary>
        private RainSystem rainSystem;
        /// <summary>Font name → index lookup dictionary / 字体名称到索引的查找字典</summary>
        static Dictionary<string, int> fontNameIndex;
        /// <summary>All non-joystick KeyCodes, cached for input detection / 所有非摇杆按键代码缓存，用于按键检测</summary>
        private static readonly KeyCode[] AllKeyCodes;
        /// <summary>Cached current style to avoid redundant GetKeyCode calls / 缓存当前样式，避免重复调用 GetKeyCode</summary>
        private KeyviewerStyle cachedKeyStyle = (KeyviewerStyle)(-1);
        /// <summary>Cached main key array / 缓存的主按键数组</summary>
        private KeyCode[] cachedMainKeys;
        /// <summary>Cached current foot style / 缓存当前的脚键样式</summary>
        private FootKeyviewerStyle cachedFootStyle = (FootKeyviewerStyle)(-1);
        /// <summary>Cached foot key array / 缓存的脚键数组</summary>
        private KeyCode[] cachedFootKeys;
        /// <summary>MapleStory font loaded from file / 从文件加载的 MapleStory 字体</summary>
        private TMP_FontAsset mapleFont;
        /// <summary>Cache of per-font shadow materials / 每个字体的阴影材质缓存</summary>
        private Dictionary<TMP_FontAsset, Material> shadowMaterials = new Dictionary<TMP_FontAsset, Material>();
        /// <summary>List of all available fonts (built-in + custom) / 所有可用字体列表（内置 + 自定义）</summary>
        static readonly List<FontEntry> fontList = new List<FontEntry>();
        /// <summary>Whether the font selection list is expanded in settings / 设置中字体选择列表是否展开</summary>
        bool fontListExpanded;
        /// <summary>Whether the overlay was enabled last frame (for toggle detection) / 上一帧覆盖层是否启用（用于开关检测）</summary>
        private bool wasEnabled;
        /// <summary>Whether the font has been restored after scene load / 场景加载后字体是否已恢复</summary>
        private bool fontRestored;

        // ======================== Unity Lifecycle / Unity 生命周期 ========================

        /// <summary>
        /// Initialize the mod: load settings, i18n, resources / 初始化 Mod：加载设置、国际化、资源
        /// </summary>
        void Awake()
        {
            instance = this;
            LoadSettings();
            I18n.Load();
            I18n.Lang = Settings.Language;
            currentKeyViewerStyle = Settings.KeyViewerStyle;
            rainSystem = new RainSystem(new RainSettings(Settings));
            TryLoadResources();
            wasEnabled = Settings.Enabled;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// Restore the user's font selection after scene load (once per scene) / 场景加载后恢复用户字体选择（每场景一次）
        /// </summary>
        void RestoreFontOnce()
        {
            if (fontNameIndex == null || fontRestored || string.IsNullOrEmpty(Settings.FontName)) return;
            if (fontNameIndex.TryGetValue(Settings.FontName, out int idx))
            {
                Settings.FontIndex = idx;
                UpdateAllFonts();
                SaveSettings();
            }
            fontRestored = true;
        }

        /// <summary>
        /// Called when the GameObject becomes active / GameObject 变为活跃时调用
        /// </summary>
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

        /// <summary>
        /// Called when the GameObject becomes inactive / GameObject 变为不活跃时调用
        /// </summary>
        void OnDisable()
        {
            DisableKeyViewer();
            SaveSettings();
        }

        /// <summary>
        /// Called when the GameObject is destroyed / GameObject 被销毁时调用
        /// </summary>
        void OnDestroy()
        {
            SaveSettings();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            rainSystem?.ClearAll(Keys);
            foreach (var mat in shadowMaterials.Values)
                Destroy(mat);
            shadowMaterials.Clear();
        }

        /// <summary>
        /// Called when a new scene is loaded: save counts, clean up rain, re-link fallback fonts / 新场景加载时调用：保存计数、清理雨滴、重新链接后备字体
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SaveSettings();
            for (int i = fontList.Count - 1; i >= 0; i--)
                if (fontList[i].font == null) fontList.RemoveAt(i);
            if (Settings.FontIndex >= fontList.Count)
                Settings.FontIndex = 0;
            fontRestored = false;
            LinkFallbackFonts();
            rainSystem.ClearActiveDrops(Keys);
        }

        /// <summary>
        /// Start is called after OnEnable; restore font selection / Start 在 OnEnable 之后调用；恢复字体选择
        /// </summary>
        void Start()
        {
            RestoreFontOnce();
        }

        /// <summary>
        /// Main update loop: input detection, KPS calculation, rain effect update / 主更新循环：按键检测、KPS 计算、雨滴效果更新
        /// </summary>
        void Update()
        {
            // Detect toggle change for enabled/disabled / 检测启用/禁用状态切换
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
                ProcessKeySelection();              // Handle key rebinding input / 处理按键重新绑定输入
                ProcessMainAndFootKeysInUpdate(now); // Detect key presses / 检测按键按下
                rainSystem.ProcessKeyQueues(Keys);  // Spawn rain objects from queue / 从队列生成雨滴对象
                ProcessKpsInUpdate(now);            // Update KPS counter / 更新 KPS 计数器
                if (Settings.EnableRainEffect) rainSystem.UpdateEffects(Keys); // Update rain drop positions / 更新雨滴位置
            }
        }

        // ======================== Config Management / 配置管理 ========================

        /// <summary>
        /// Load settings from JSON file, with v1→v2 migration for normalized coordinates / 从 JSON 文件加载设置，支持 v1→v2 归一化坐标迁移
        /// </summary>
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
                        // v1→v2：将绝对像素偏移迁移到归一化 0-1 坐标（X：0=左 1=右，Y：0=顶 1=底）
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
                        // Ensure arrays are initialized (prevents null refs from old saves) / 确保数组已初始化（防止旧存档的空引用）
                        Settings.key8Text = Settings.key8Text ?? new string[8];
                        Settings.key10Text = Settings.key10Text ?? new string[10];
                        Settings.key12Text = Settings.key12Text ?? new string[12];
                        Settings.key16Text = Settings.key16Text ?? new string[16];
                        Settings.key20Text = Settings.key20Text ?? new string[20];
                        Settings.footkey2Text = Settings.footkey2Text ?? new string[2];
                        Settings.footkey4Text = Settings.footkey4Text ?? new string[4];
                        Settings.footkey6Text = Settings.footkey6Text ?? new string[6];
                        Settings.footkey8Text = Settings.footkey8Text ?? new string[8];
                        Settings.footkey10Text = Settings.footkey10Text ?? new string[10];
                        Settings.footkey12Text = Settings.footkey12Text ?? new string[12];
                        Settings.footkey14Text = Settings.footkey14Text ?? new string[14];
                        Settings.footkey16Text = Settings.footkey16Text ?? new string[16];
                        Settings.Count = Settings.Count ?? new int[36];
                    }
                    else
                    {
                        Main.Mod.Logger.Error("Failed to parse settings file (empty or corrupt), creating new settings");
                        Settings = new KeyViewerSettings();
                        SaveSettings();
                    }
                }
                catch (Exception e)
                {
                    Main.Mod.Logger.Error($"Failed to load settings: {e.Message}");
                    Settings = new KeyViewerSettings();
                }
            }
            else
            {
                Settings = new KeyViewerSettings();
                SaveSettings();
            }
        }

        /// <summary>
        /// Save current settings to JSON file / 将当前设置保存到 JSON 文件
        /// </summary>
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
                Main.Mod.Logger.Error($"Failed to save settings: {e.Message}");
            }
        }
    }
}