using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        public static readonly Color GhostRainColorDefault = new(1, 1, 1, 0.6f);
        public static readonly Color GhostRainColor2Default = new(1, 1, 1, 0.6f);
        public static readonly Color GhostRainColor3Default = new(1, 1, 1, 0.6f);

        public static readonly Color RainShadowColorDefault = new(0, 0, 0, 0.35f);
        public static readonly Color RainOutlineColorDefault = new(1, 1, 1, 0.5f);

        // Back-row key index mapping for each layout style / 每种布局样式的后排按键索引映射
        // Each byte array defines which keys go in the second row, in display order / 每个字节数组定义了第二排有哪些按键及其显示顺序
        public static readonly byte[] BackSequence8 = Array.Empty<byte>();
        public static readonly byte[] BackSequence10 = new byte[] { 8, 9 };
        public static readonly byte[] BackSequence12 = new byte[] { 9, 8, 10, 11 };
        public static readonly byte[] BackSequence14 = new byte[] { 13, 9, 8, 10, 11, 12 };
        public static readonly byte[] BackSequence16 = new byte[] { 12, 13, 9, 8, 10, 11, 14, 15 };
        public static readonly byte[] BackSequence20 = new byte[] { 12, 13, 9, 8, 10, 11, 14, 15, 17, 16, 18, 19 };
        public static readonly byte[] BackSequence24 = new byte[] { 12, 13, 9, 8, 10, 11, 14, 15, 17, 16, 18, 19, 21, 20, 22, 23 };

        /// <summary>Display names for main key layout selection grid / 主按键布局选择网格的显示名称</summary>
        static readonly string[] KeyLayoutNames = { "12K", "16K", "20K", "10K", "8K", "14K", "24K", "108K" };
        /// <summary>Display names for foot key layout selection grid / 脚键布局选择网格的显示名称</summary>
        static readonly string[] FootKeyLayoutNames = { "Off", "2K", "4K", "6K", "8K", "10K", "12K", "14K", "16K" };

        /// <summary>Foot key starting index (20 for normal layouts, 24 for 24K) / 脚键起始索引</summary>
        internal static int FootKeyBase => 24;
        /// <summary>Whether the current layout has a third row of keys / 当前布局是否有第三排按键</summary>
        internal static bool HasThirdRow => Settings.Data.KeyViewerStyle is KeyviewerStyle.Key20 or KeyviewerStyle.Key24;
        /// <summary>Maximum key slots (keys can be at indices 0..MaxKeySlots-1) / 最大键位槽数</summary>
        internal const int MaxKeySlots = 40;
        /// <summary>Whether the current layout is the full 108-key keyboard / 当前布局是否为全键盘</summary>
        internal static bool IsFullKeyboard => Settings.Data.KeyViewerStyle == KeyviewerStyle.Full108;
        /// <summary>Number of main keys for the current layout (40 normal, 108 full) / 当前布局的主键数</summary>
        private static int GetKeyCount() => IsFullKeyboard ? Settings.Data.key108.Length : MaxKeySlots;

        /// <summary>Default 108-key physical keyboard bindings, indexed by Full108 array slot / 全键盘默认键位绑定，下标对应 Full108 数组槽位</summary>
        internal static KeyCode[] BuildDefaultKey108()
        {
            // 105 keys, index-aligned with the slot list in InitializeFullKeyboard (function / number / QWERTY / ASDF / ZXCV / bottom / edit / arrows / numpad).
            return new KeyCode[]
            {
                KeyCode.Escape,
                KeyCode.F1,
                KeyCode.F2,
                KeyCode.F3,
                KeyCode.F4,
                KeyCode.F5,
                KeyCode.F6,
                KeyCode.F7,
                KeyCode.F8,
                KeyCode.F9,
                KeyCode.F10,
                KeyCode.F11,
                KeyCode.F12,
                KeyCode.Print,
                KeyCode.ScrollLock,
                KeyCode.Pause,
                KeyCode.SysReq,
                KeyCode.BackQuote,
                KeyCode.Alpha1,
                KeyCode.Alpha2,
                KeyCode.Alpha3,
                KeyCode.Alpha4,
                KeyCode.Alpha5,
                KeyCode.Alpha6,
                KeyCode.Alpha7,
                KeyCode.Alpha8,
                KeyCode.Alpha9,
                KeyCode.Alpha0,
                KeyCode.Minus,
                KeyCode.Equals,
                KeyCode.Backspace,
                KeyCode.Tab,
                KeyCode.Q,
                KeyCode.W,
                KeyCode.E,
                KeyCode.R,
                KeyCode.T,
                KeyCode.Y,
                KeyCode.U,
                KeyCode.I,
                KeyCode.O,
                KeyCode.P,
                KeyCode.LeftBracket,
                KeyCode.RightBracket,
                KeyCode.Backslash,
                KeyCode.CapsLock,
                KeyCode.A,
                KeyCode.S,
                KeyCode.D,
                KeyCode.F,
                KeyCode.G,
                KeyCode.H,
                KeyCode.J,
                KeyCode.K,
                KeyCode.L,
                KeyCode.Semicolon,
                KeyCode.Quote,
                KeyCode.Return,
                KeyCode.LeftShift,
                KeyCode.Z,
                KeyCode.X,
                KeyCode.C,
                KeyCode.V,
                KeyCode.B,
                KeyCode.N,
                KeyCode.M,
                KeyCode.Comma,
                KeyCode.Period,
                KeyCode.Slash,
                KeyCode.RightShift,
                KeyCode.LeftControl,
                KeyCode.LeftWindows,
                KeyCode.LeftAlt,
                KeyCode.Space,
                KeyCode.RightAlt,
                KeyCode.RightWindows,
                KeyCode.Menu,
                KeyCode.RightControl,
                KeyCode.Insert,
                KeyCode.Delete,
                KeyCode.Home,
                KeyCode.End,
                KeyCode.PageUp,
                KeyCode.PageDown,
                KeyCode.UpArrow,
                KeyCode.LeftArrow,
                KeyCode.DownArrow,
                KeyCode.RightArrow,
                KeyCode.Numlock,
                KeyCode.KeypadDivide,
                KeyCode.KeypadMultiply,
                KeyCode.KeypadMinus,
                KeyCode.Keypad7,
                KeyCode.Keypad8,
                KeyCode.Keypad9,
                KeyCode.KeypadPlus,
                KeyCode.Keypad4,
                KeyCode.Keypad5,
                KeyCode.Keypad6,
                KeyCode.Keypad1,
                KeyCode.Keypad2,
                KeyCode.Keypad3,
                KeyCode.Keypad0,
                KeyCode.KeypadPeriod,
                KeyCode.KeypadEnter
            };
        }

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
        /// <summary>Merged background-shape layer (owns slot state) / 合并背景形状层（持有槽位状态）</summary>
        KeyShapeLayer keyShapeLayer;
        /// <summary>Merged outline-shape layer (shares state with keyShapeLayer) / 合并描边形状层（与背景层共享状态）</summary>
        KeyShapeLayer keyOutlineLayer;
        /// <summary>Sub-canvas holding all key texts (isolates text rebatching from shape layer) / 持有全部按键文本的子画布（文本重批与形状层隔离）</summary>
        Transform textLayer;
        /// <summary>Merged rain layer (solid quads: normal bodies + ghost shadow/outline) / 合并雨滴层（纯色四边形：普通本体 + 鬼雨阴影/描边）</summary>
        RainLayer rainLayer;
        /// <summary>Merged ghost rain layer (ghost sprite bodies) / 合并鬼雨层（鬼雨贴图本体）</summary>
        GhostRainLayer ghostRainLayer;
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
        /// <summary>Per-key press timestamp queues for per-key KPS / 每键按下时间戳队列，用于每键 KPS</summary>
        Queue<long>[] keyPressTimes;
        /// <summary>Last frame per-key KPS values for change detection / 上一帧每键 KPS 值，用于变化检测</summary>
        int[] lastPerKeyKps;
        /// <summary>High-resolution stopwatch for timing / 用于计时的高精度秒表</summary>
        Stopwatch Stopwatch;
        /// <summary>Timestamp of last frame for delta calculation / 上一帧的时间戳，用于增量计算</summary>
        /// <summary>Whether the key change section in settings is expanded / 设置中按键更改区域是否展开</summary>
        bool KeyChangeExpanded;
        /// <summary>Whether the ghost rain key section in settings is expanded / 设置中鬼键区域是否展开</summary>
        bool GhostRainChangeExpanded;
        /// <summary>Whether the text change section in settings is expanded / 设置中文本更改区域是否展开</summary>
        bool TextChangeExpanded;
        /// <summary>Whether the rain effect section in settings is expanded / 设置中雨线效果区域是否展开</summary>
        bool RainExpanded;
        /// <summary>Per-color-section expanded state in settings / 设置中每个颜色区域的展开状态</summary>
        bool[] ColorExpanded;
        /// <summary>Whether the custom position section in settings is expanded / 设置中自定义位置区域是否展开</summary>
        bool CustomPositionExpanded;
        /// <summary>Currently selected key index for rebinding (-1 = none) / 当前为重新绑定选中的按键索引（-1 = 无）</summary>
        int SelectedKey = -1;
        /// <summary>Current rebind mode: 0=key, 1=text, 2=ghost key / 当前重绑定模式：0=按键，1=文本，2=鬼键</summary>
        int changeState;

        /// <summary>Path to the settings JSON file / 设置 JSON 文件路径</summary>
        static string ConfigPath
        {
            get
            {
                if (configPath == null)
                {
                    string modPath = Loader.ModPath;
                    configPath = Path.Combine(modPath ?? Application.persistentDataPath, "config", "settings.json");
                }
                return configPath;
            }
        }
        static string configPath;

        /// <summary>Path to the profiles directory / 配置目录路径</summary>
        static string ProfileDir
        {
            get
            {
                if (profileDir == null)
                {
                    string modPath = Loader.ModPath;
                    profileDir = Path.Combine(modPath ?? Application.persistentDataPath, "config", "profiles");
                }
                return profileDir;
            }
        }
        static string profileDir;

        /// <summary>Sanitize a profile name for use as a filename / 将配置名称净化用于文件名</summary>
        static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "Unnamed" : name;
        }

        /// <summary>Get the full path to a profile JSON file / 获取配置 JSON 文件的完整路径</summary>
        static string GetProfilePath(string name) => Path.Combine(ProfileDir, SanitizeFileName(name) + ".json");

        /// <summary>Cached background sprite from AssetBundle / 从 AssetBundle 缓存的背景精灵</summary>
        Sprite keyBackgroundSprite;
        /// <summary>Cached outline sprite from AssetBundle / 从 AssetBundle 缓存的轮廓精灵</summary>
        Sprite keyOutlineSprite;
        /// <summary>Cached ghost rain sprite (loaded from PNG file) / 从 PNG 文件缓存的鬼雨精灵</summary>
        Sprite ghostRainSprite;
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
        /// <summary>Cached ghost key array / 缓存的鬼键数组</summary>
        private KeyCode[] cachedGhostKeys;
        /// <summary>Ghost key press state tracking / 鬼键按下状态跟踪</summary>
        private bool[] ghostKeyStates;
        /// <summary>MapleStory font loaded from AssetBundle / 从 AssetBundle 加载的 MapleStory 字体</summary>
        private TMP_FontAsset mapleFont;
        /// <summary>Cache of per-font shadow materials / 每个字体的阴影材质缓存</summary>
        private Dictionary<TMP_FontAsset, Material> shadowMaterials = new Dictionary<TMP_FontAsset, Material>();
        /// <summary>List of all available fonts (built-in + custom) / 所有可用字体列表（内置 + 自定义）</summary>
        static readonly List<FontEntry> fontList = new List<FontEntry>();
        /// <summary>Whether the font selection list is expanded in settings / 设置中字体选择列表是否展开</summary>
        bool fontListExpanded;
        bool fontStyleExpanded;
        /// <summary>Whether the overlay was enabled last frame (for toggle detection) / 上一帧覆盖层是否启用（用于开关检测）</summary>
        private bool wasEnabled;
        /// <summary>Whether the font has been restored after scene load / 场景加载后字体是否已恢复</summary>
        private bool fontRestored;
        /// <summary>Whether any key press occurred recently (skip idle per-key KPS loop) / 最近是否有按键（跳过空闲的每键 KPS 循环）</summary>
        private bool _hasKeyPressActivity;

        // ======================== Unity Lifecycle / Unity 生命周期 ========================

        /// <summary>
        /// Initialize the mod: load settings, i18n, resources / 初始化 Mod：加载设置、国际化、资源
        /// </summary>
        void Awake()
        {
            instance = this;
            LoadSettings();
            I18n.Lang = Settings.Language;
            rainSystem = new RainSystem(Settings);
            TryLoadResources();
            wasEnabled = Settings.Data.Enabled;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// Restore the user's font selection after scene load (once per scene) / 场景加载后恢复用户字体选择（每场景一次）
        /// </summary>
        void RestoreFontOnce()
        {
            if (fontNameIndex == null || fontRestored || string.IsNullOrEmpty(Settings.Data.FontName)) return;
            if (fontNameIndex.TryGetValue(Settings.Data.FontName, out int idx))
            {
                Settings.Data.FontIndex = idx;
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
            if (Settings.Data.Enabled) EnableKeyViewer();
            else DisableKeyViewer();
            if (Settings.Data.CustomPositionEnabled)
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
            SaveSettings();
            DisableKeyViewer();
        }

        /// <summary>
        /// Called when the GameObject is destroyed / GameObject 被销毁时调用
        /// </summary>
        void OnDestroy()
        {
            SaveSettings();
            instance = null; // stop loader GUI callbacks from running on the destroyed component / 阻止加载器 GUI 回调继续在已销毁组件上运行
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
            if (fontList.Count == 0 || Settings.Data.FontIndex >= fontList.Count)
                Settings.Data.FontIndex = 0;
            fontRestored = false;
            LinkFallbackFonts();
            rainSystem.ClearActiveDrops(Keys);
            // RestoreFontOnce re-maps FontName to a valid index — the pruning above may have shifted
            // the list. It used to run only from Start, so the per-scene reset above was dead logic.
            // RestoreFontOnce 把 FontName 重新映射为有效索引——上面的清理可能使列表移位。
            // 此前它只从 Start 调用，上面的每场景重置是死逻辑。
            RestoreFontOnce();
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
            // Skip all processing when game window is not focused / 窗口未激活时跳过所有处理
            if (!Application.isFocused) return;

            bool enabled = Settings.Data.Enabled;
            // Detect toggle change for enabled/disabled / 检测启用/禁用状态切换
            if (wasEnabled != enabled)
            {
                if (enabled)
                {
                    EnableKeyViewer();
                    if (Settings.Data.CustomPositionEnabled)
                    {
                        ResetKeyViewerPosition();
                        ResetFootKeyViewerPosition();
                    }
                }
                else DisableKeyViewer();
                wasEnabled = enabled;
            }
            if (KeyViewerObject != null && enabled)
            {
                CheckResolutionChanged();
                long now = Stopwatch.ElapsedMilliseconds;
                ProcessKeySelection();              // Handle key rebinding input / 处理按键重新绑定输入
                ProcessMainAndFootKeysInUpdate(now); // Detect key presses / 检测按键按下
                ProcessKpsInUpdate(now);            // Update KPS counter / 更新 KPS 计数器
                ProcessPerKeyKpsInUpdate(now);       // Update per-key KPS / 更新每键 KPS
                ProcessGhostKeysInUpdate();          // Process ghost key inputs / 处理鬼键输入
                if (Settings.Data.EnableRainEffect) rainSystem.UpdateEffects(Keys); // Update rain drop positions / 更新雨滴位置
            }
        }

        // ======================== Config Management / 配置管理 ========================

        private void LoadSettings()
        {
            string directory = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            if (!File.Exists(ConfigPath))
            {
                Settings = new KeyViewerSettings();
                SaveSettings();
                return;
            }

            try
            {
                string json = File.ReadAllText(ConfigPath);
                Settings = JsonUtility.FromJson<KeyViewerSettings>(json);
                if (Settings == null)
                {
                    Loader.Error("Failed to parse settings file (empty or corrupt), creating new settings");
                    BackupCorruptConfig();
                    Settings = new KeyViewerSettings();
                    return;
                }

                // Backward compat: old flat JSON had profile fields directly on KeyViewerSettings,
                // now they live in ProfileData. Overwrite Data from the flat JSON to preserve them.
                JsonUtility.FromJsonOverwrite(json, Settings.Data);

                if (Settings.Version < 2) MigrateV1toV2();
                if (Settings.Version < 3) MigrateV2toV3();
                LoadProfileFromMeta();
                if (Settings.Version < 4) MigrateV3toV4();
                if (Settings.Version < 5) MigrateV4toV5();

                EnsureSettingsArrays();
                SyncProfilesWithDisk();
                settingsGuiTab = Mathf.Clamp(Settings.UiTab, 0, TabCount - 1);
            }
            catch (Exception e)
            {
                Loader.Error($"Failed to load settings: {e.Message}");
                // Back up the offending files before falling back to defaults — the next SaveSettings
                // would otherwise overwrite them and permanently lose the user's config (an old
                // mid-migration crash used to wipe profiles this way).
                // 回退默认前先备份出问题的文件——否则下一次 SaveSettings 会直接覆盖，用户的配置就
                // 永久丢了（旧版本迁移中途崩溃曾以此方式清空配置）。
                BackupCorruptConfig();
                Settings = new KeyViewerSettings();
            }
        }

        /// <summary>Copy the config meta + current profile to *.corrupt backups before falling back to defaults / 回退默认前把配置元数据与当前 Profile 备份为 *.corrupt</summary>
        private void BackupCorruptConfig()
        {
            try
            {
                if (File.Exists(ConfigPath)) File.Copy(ConfigPath, ConfigPath + ".corrupt", true);
                string cur = Settings?.CurrentProfile;
                if (!string.IsNullOrEmpty(cur))
                {
                    string pp = GetProfilePath(cur);
                    if (File.Exists(pp)) File.Copy(pp, pp + ".corrupt", true);
                }
            }
            catch { /* best-effort backup; the load failure is already reported / 尽力备份；加载失败已另行报告 */ }
        }

        private void MigrateV1toV2()
        {
            const float refW = 1920f, refH = 1080f;
            float Clamp01(float v) => v < 0 ? 0 : (v > 1 ? 1 : v);
            Settings.Data.MainKeyViewerPosition = new Vector2(
                Clamp01(Settings.Data.MainKeyViewerPosition.x / refW),
                1f - Clamp01(Settings.Data.MainKeyViewerPosition.y / refH));
            Settings.Data.FootKeyViewerPosition = new Vector2(
                Clamp01(Settings.Data.FootKeyViewerPosition.x / refW),
                1f - Clamp01(Settings.Data.FootKeyViewerPosition.y / refH));
            Settings.Version = 2;
        }

        private void MigrateV2toV3()
        {
            Loader.Log("Migrating settings v2 → v3: creating Default profile");
            Settings.Version = 3;
            Settings.CurrentProfile = "Default";
            Settings.ProfileNames = new[] { "Default" };
            EnsureSettingsArrays();
            SaveCurrentProfile();
            SaveMetaOnly();
            Loader.Log("Migration v2→v3 complete");
        }

        private void MigrateV3toV4()
        {
            Loader.Log("Migrating settings v3 → v4: FootKeyBase fixed to 24");
            Settings.Version = 4;
            var d = Settings.Data;
            const int oldFootBase = 20;

            if (d.KeyViewerStyle == KeyviewerStyle.Key24)
            {
                SaveCurrentProfile();
                SaveMetaOnly();
                return;
            }

            int footSize = d.FootKeyViewerStyle switch
            {
                FootKeyviewerStyle.Key2 => 2,
                FootKeyviewerStyle.Key4 => 4,
                FootKeyviewerStyle.Key6 => 6,
                FootKeyviewerStyle.Key8 => 8,
                FootKeyviewerStyle.Key10 => 10,
                FootKeyviewerStyle.Key12 => 12,
                FootKeyviewerStyle.Key14 => 14,
                FootKeyviewerStyle.Key16 => 16,
                _ => 0
            };
            if (footSize == 0)
            {
                SaveCurrentProfile();
                SaveMetaOnly();
                return;
            }

            static void ShiftColorArray(Color[] arr, int from, int to, int count)
            {
                if (arr == null) return;
                for (int i = count - 1; i >= 0; i--)
                {
                    if (to + i < arr.Length)
                        arr[to + i] = from + i < arr.Length ? arr[from + i] : default;
                }
                for (int i = 0; i < count && from + i < arr.Length; i++)
                    arr[from + i] = default;
            }

            Array.Copy(d.Count, oldFootBase, d.Count, FootKeyBase, footSize);
            Array.Clear(d.Count, oldFootBase, footSize);
            ShiftColorArray(d.PerKeyBackground, oldFootBase, FootKeyBase, footSize);
            ShiftColorArray(d.PerKeyBackgroundClicked, oldFootBase, FootKeyBase, footSize);
            ShiftColorArray(d.PerKeyOutline, oldFootBase, FootKeyBase, footSize);
            ShiftColorArray(d.PerKeyOutlineClicked, oldFootBase, FootKeyBase, footSize);
            ShiftColorArray(d.PerKeyText, oldFootBase, FootKeyBase, footSize);
            ShiftColorArray(d.PerKeyTextClicked, oldFootBase, FootKeyBase, footSize);
            ShiftColorArray(d.PerKeyRainColor, oldFootBase, FootKeyBase, footSize);

            SaveCurrentProfile();
            MigrateAllProfileFiles();
            SaveMetaOnly();
            Loader.Log("Migration v3→v4 complete");
        }

        private void MigrateV4toV5()
        {
            // No array reshaping: a dedicated key108 array is filled lazily by EnsureSettingsArrays.
            // Existing 8K-24K + foot-key profiles load unchanged.
            Settings.Version = 5;
            EnsureSettingsArrays();
            SaveCurrentProfile();
            SaveMetaOnly();
            Loader.Log("Migration v4→v5 complete");
        }

        private void MigrateAllProfileFiles()
        {
            if (Settings.ProfileNames == null) return;
            string savedProfile = Settings.CurrentProfile;
            foreach (string name in Settings.ProfileNames)
            {
                if (name == savedProfile) continue;
                try
                {
                    string path = GetProfilePath(name);
                    if (!File.Exists(path)) continue;
                    string json = File.ReadAllText(path);
                    var pd = new ProfileData();
                    JsonUtility.FromJsonOverwrite(json, pd);
                    if (pd.KeyViewerStyle == KeyviewerStyle.Key24) continue;
                    int fs = pd.FootKeyViewerStyle switch
                    {
                        FootKeyviewerStyle.Key2 => 2,
                        FootKeyviewerStyle.Key4 => 4,
                        FootKeyviewerStyle.Key6 => 6,
                        FootKeyviewerStyle.Key8 => 8,
                        FootKeyviewerStyle.Key10 => 10,
                        FootKeyviewerStyle.Key12 => 12,
                        FootKeyviewerStyle.Key14 => 14,
                        FootKeyviewerStyle.Key16 => 16,
                        _ => 0
                    };
                    if (fs == 0) continue;
                    const int oldBase = 20;
                    // Old builds wrote Count[36]; FromJsonOverwrite restores that shorter array and the
                    // copy below would run past its end (throwing, and the profile would then be
                    // skipped forever because the meta Version already advanced). Resize first, the
                    // same way EnsureSettingsArrays handles the live settings.
                    // 旧版本写入的是 Count[36]；FromJsonOverwrite 会还原成短数组，下面的复制会越界
                    //（抛异常后该 Profile 被永久跳过——meta 的 Version 已经先升上去了）。先按
                    // EnsureSettingsArrays 处理在线设置的同样方式重定长度。
                    if (pd.Count == null || pd.Count.Length != MaxKeySlots)
                    {
                        int[] c = new int[MaxKeySlots];
                        if (pd.Count != null) Array.Copy(pd.Count, c, Math.Min(pd.Count.Length, MaxKeySlots));
                        pd.Count = c;
                    }
                    Array.Copy(pd.Count, oldBase, pd.Count, FootKeyBase, fs);
                    Array.Clear(pd.Count, oldBase, fs);
                    static void Shift(Color[] a, int from, int to, int n)
                    {
                        if (a == null) return;
                        for (int i = n - 1; i >= 0; i--)
                        {
                            if (to + i < a.Length)
                                a[to + i] = from + i < a.Length ? a[from + i] : default;
                        }
                        for (int i = 0; i < n && from + i < a.Length; i++)
                            a[from + i] = default;
                    }
                    Shift(pd.PerKeyBackground, oldBase, FootKeyBase, fs);
                    Shift(pd.PerKeyBackgroundClicked, oldBase, FootKeyBase, fs);
                    Shift(pd.PerKeyOutline, oldBase, FootKeyBase, fs);
                    Shift(pd.PerKeyOutlineClicked, oldBase, FootKeyBase, fs);
                    Shift(pd.PerKeyText, oldBase, FootKeyBase, fs);
                    Shift(pd.PerKeyTextClicked, oldBase, FootKeyBase, fs);
                    Shift(pd.PerKeyRainColor, oldBase, FootKeyBase, fs);
                    WriteAllTextSafe(path, JsonUtility.ToJson(pd, true));
                }
                catch (Exception e)
                {
                    Loader.Warning($"Failed to migrate profile '{name}': {e.Message}");
                }
            }
        }

        private void LoadProfileFromMeta()
        {
            string profileName = !string.IsNullOrEmpty(Settings.CurrentProfile)
                ? Settings.CurrentProfile : "Default";
            if (File.Exists(GetProfilePath(profileName)))
            {
                LoadProfile(profileName);
                Settings.CurrentProfile = profileName;
                EnsureSettingsArrays();
            }
            else
            {
                Loader.Warning($"Profile '{profileName}' not found, creating new profile");
                Settings.CurrentProfile = profileName;
                if (Settings.ProfileNames == null || Settings.ProfileNames.Length == 0)
                    Settings.ProfileNames = new[] { profileName };
                EnsureSettingsArrays();
                SaveCurrentProfile();
            }
        }

        private static Color[] EnsureColorArray(Color[] arr, int n, Color fill)
        {
            if (arr != null && arr.Length == n) return arr;
            Color[] result = new Color[n];
            for (int i = 0; i < n; i++)
                result[i] = arr != null && i < arr.Length ? arr[i] : fill;
            return result;
        }

        /// <summary>Ensure all settings arrays are initialized / 确保所有设置数组已初始化</summary>
        private static void EnsureSettingsArrays()
        {
            Settings.Data.key8Text = Settings.Data.key8Text ?? new string[8];
            Settings.Data.key10Text = Settings.Data.key10Text ?? new string[10];
            Settings.Data.key12Text = Settings.Data.key12Text ?? new string[12];
            Settings.Data.key14Text = Settings.Data.key14Text ?? new string[14];
            Settings.Data.key16Text = Settings.Data.key16Text ?? new string[16];
            Settings.Data.key20Text = Settings.Data.key20Text ?? new string[20];
            Settings.Data.key24Text = Settings.Data.key24Text ?? new string[24];
            // A truncated key108 (hand-edited profile) would crash InitializeFullKeyboard's fixed
            // 105-slot table; the array isn't user-rebindable (SetupKey ignores full-keyboard mode),
            // so a wrong-length array is simply replaced with the default.
            // 截断的 key108（手工编辑的 Profile）会让 InitializeFullKeyboard 的固定 105 槽位表越界；
            // 该数组不支持用户重绑（SetupKey 在全键盘模式下直接返回），长度不对时直接换回默认值。
            if (Settings.Data.key108 == null || Settings.Data.key108.Length != 105)
                Settings.Data.key108 = BuildDefaultKey108();
            Settings.Data.footkey2Text = Settings.Data.footkey2Text ?? new string[2];
            Settings.Data.footkey4Text = Settings.Data.footkey4Text ?? new string[4];
            Settings.Data.footkey6Text = Settings.Data.footkey6Text ?? new string[6];
            Settings.Data.footkey8Text = Settings.Data.footkey8Text ?? new string[8];
            Settings.Data.footkey10Text = Settings.Data.footkey10Text ?? new string[10];
            Settings.Data.footkey12Text = Settings.Data.footkey12Text ?? new string[12];
            Settings.Data.footkey14Text = Settings.Data.footkey14Text ?? new string[14];
            Settings.Data.footkey16Text = Settings.Data.footkey16Text ?? new string[16];
            Settings.Data.Count = Settings.Data.Count ?? new int[MaxKeySlots];
            // FromJsonOverwrite restores whatever array length the profile JSON carries — builds
            // between the profile refactor and MaxKeySlots=40 wrote Count[36]. A short array made
            // the V3→V4 foot-base migration's Array.Copy throw (which reset ALL settings to
            // defaults and saved them over the profile files). Resize like the color arrays.
            // FromJsonOverwrite 会按 Profile JSON 里的数组长度原样还原——profile 重构到
            // MaxKeySlots=40 之间的版本写入的是 Count[36]。短数组会让 V3→V4 脚键基线迁移的
            // Array.Copy 抛异常（进而把全部设置重置为默认值并覆盖 Profile 文件）。像颜色数组
            // 一样重定长度。
            if (Settings.Data.Count.Length != MaxKeySlots)
            {
                int[] c = new int[MaxKeySlots];
                Array.Copy(Settings.Data.Count, c, Math.Min(Settings.Data.Count.Length, MaxKeySlots));
                Settings.Data.Count = c;
            }
            int n = MaxKeySlots + 2;
            Settings.Data.PerKeyBackground = EnsureColorArray(Settings.Data.PerKeyBackground, n, Settings.Data.Background);
            Settings.Data.PerKeyBackgroundClicked = EnsureColorArray(Settings.Data.PerKeyBackgroundClicked, n, Settings.Data.BackgroundClicked);
            Settings.Data.PerKeyOutline = EnsureColorArray(Settings.Data.PerKeyOutline, n, Settings.Data.Outline);
            Settings.Data.PerKeyOutlineClicked = EnsureColorArray(Settings.Data.PerKeyOutlineClicked, n, Settings.Data.OutlineClicked);
            Settings.Data.PerKeyText = EnsureColorArray(Settings.Data.PerKeyText, n, Settings.Data.Text);
            Settings.Data.PerKeyTextClicked = EnsureColorArray(Settings.Data.PerKeyTextClicked, n, Settings.Data.TextClicked);
            Settings.Data.PerKeyRainColor = EnsureColorArray(Settings.Data.PerKeyRainColor, n, Settings.Data.RainColor);
            // Same gap class as Count: these two are indexed unguarded by the rain system
            // (PerKeyGhostRainColor) and the per-key font path, and were only sized by the
            // constructor that FromJsonOverwrite bypasses. / 与 Count 同类缺口:雨滴系统未加守卫地
            // 索引 PerKeyGhostRainColor,每键字号路径亦然;此前仅靠被 FromJsonOverwrite 绕过的
            // 构造函数定长。
            Settings.Data.PerKeyGhostRainColor = EnsureColorArray(Settings.Data.PerKeyGhostRainColor, n, GhostRainColorDefault);
            if (Settings.Data.PerKeyFontSize == null || Settings.Data.PerKeyFontSize.Length != n)
            {
                float[] f = new float[n]; // 0 = use the global font size / 0 = 使用全局字号
                if (Settings.Data.PerKeyFontSize != null)
                    Array.Copy(Settings.Data.PerKeyFontSize, f, Math.Min(Settings.Data.PerKeyFontSize.Length, n));
                Settings.Data.PerKeyFontSize = f;
            }
        }

        private void ClearKpsTimers()
        {
            PressTimes?.Clear();
            if (keyPressTimes != null)
                for (int i = 0; i < keyPressTimes.Length; i++)
                    keyPressTimes[i]?.Clear();
            if (lastPerKeyKps != null)
                for (int i = 0; i < lastPerKeyKps.Length; i++)
                    lastPerKeyKps[i] = 0;
            lastKps = -1;
            _hasKeyPressActivity = false;
        }

        /// <summary>
        /// Save current settings: meta + current profile / 保存当前设置：元数据 + 当前配置
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                Settings.UiTab = settingsGuiTab;
                SaveCurrentProfile();
                SaveMetaOnly();
            }
            catch (Exception e)
            {
                Loader.Error($"Failed to save settings: {e.Message}");
            }
        }

        /// <summary>
        /// Save only the meta file (settings.json) — Version, CurrentProfile, ProfileNames, Language / 仅保存元数据文件（settings.json）
        /// </summary>
        private void SaveMetaOnly()
        {
            string directory = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            string metaJson = JsonUtility.ToJson(new SettingsMeta
            {
                Version = Settings.Version,
                CurrentProfile = Settings.CurrentProfile,
                ProfileNames = Settings.ProfileNames,
                Language = Settings.Language,
                UiTab = Settings.UiTab
            }, true);
            WriteAllTextSafe(ConfigPath, metaJson);
        }

        /// <summary>
        /// Atomic file write: temp file + replace, so a crash or power loss mid-write can't truncate
        /// the live config (SaveSettings runs on every scene load, so the write window is exercised
        /// constantly). / 原子写文件：先写临时文件再替换，崩溃或断电在写入中途也不会截断现有配置
        ///（SaveSettings 每次场景加载都会执行，写入窗口一直在被反复触发）。
        /// </summary>
        private static void WriteAllTextSafe(string path, string contents)
        {
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, contents);
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }

        /// <summary>
        /// Save the current profile to its file / 将当前配置保存到文件
        /// </summary>
        private void SaveCurrentProfile()
        {
            if (!Directory.Exists(ProfileDir)) Directory.CreateDirectory(ProfileDir);
            string profilePath = GetProfilePath(Settings.CurrentProfile);
            string json = JsonUtility.ToJson(Settings.Data, true);
            WriteAllTextSafe(profilePath, json);
        }

        /// <summary>
        /// Load a named profile into Settings.Data / 加载指定配置到 Settings.Data
        /// </summary>
        private bool LoadProfile(string name)
        {
            string profilePath = GetProfilePath(name);
            if (!File.Exists(profilePath)) return false;
            try
            {
                string json = File.ReadAllText(profilePath);
                // Replace the instance first: FromJsonOverwrite only writes fields present in the JSON
                // and leaves any other field/array entry from the previously loaded profile intact,
                // which would then leak into (and be saved over) the new profile. A fresh default
                // instance guarantees no stale data survives a profile switch.
                // 先替换实例：FromJsonOverwrite 只写入 JSON 中存在的字段，会保留上一套配置残留的
                // 字段/数组项，这些残留随后会被保存并覆盖新配置。用全新默认实例可杜绝跨配置污染。
                Settings.Data = new ProfileData();
                JsonUtility.FromJsonOverwrite(json, Settings.Data);
                return true;
            }
            catch (Exception e)
            {
                Loader.Error($"Failed to load profile '{name}': {e.Message}");
                return false;
            }
        }

        private void SwitchProfile(string newName)
        {
            if (newName == Settings.CurrentProfile) return;
            string oldName = Settings.CurrentProfile;
            SaveCurrentProfile();
            if (!LoadProfile(newName))
            {
                Loader.Warning($"Failed to switch to profile '{newName}', staying on '{oldName}'");
                LoadProfile(oldName);
                return;
            }
            Settings.CurrentProfile = newName;
            EnsureSettingsArrays();
            ClearKpsTimers();
            cachedKeyStyle = (KeyviewerStyle)(-1);
            cachedFootStyle = (FootKeyviewerStyle)(-1);
            cachedMainKeys = null;
            cachedFootKeys = null;
            cachedGhostKeys = null;
            // Rebuild overlay for new settings. ResetKeyViewer recreates foot keys internally (it
            // destroys every child including them), so the outer ResetFootKeyViewer here would only
            // destroy and recreate them a second time.
            // 为新设置重建覆盖层。ResetKeyViewer 内部已重建脚键（它销毁含脚键在内的全部子物体），
            // 此处再调 ResetFootKeyViewer 只会把脚键销毁重建第二遍。
            ResetKeyViewer();
            UpdateAllFonts();
            UpdateAllKeyColors();
            if (Settings.Data.StreamerMode && !IsFullKeyboard)
            {
                SetKeyObjectActive(Kps, false);
                SetKeyObjectActive(Total, false);
            }
            SaveSettings();
        }

        /// <summary>
        /// Delete a profile file (cannot delete the last one) / 删除配置文件（不能删除最后一个）
        /// </summary>
        private void DeleteProfile(string name)
        {
            if (Settings.ProfileNames == null || Settings.ProfileNames.Length <= 1) return;
            // If deleting the current profile, switch to first available first / 如果删除的是当前配置，先切走
            bool wasCurrent = Settings.CurrentProfile == name;
            if (wasCurrent)
            {
                var others = new List<string>(Settings.ProfileNames);
                others.Remove(name);
                SwitchProfile(others[0]);
            }
            // Now delete the file and remove from list / 然后删文件和列表
            try
            {
                string profilePath = GetProfilePath(name);
                if (File.Exists(profilePath))
                    File.Delete(profilePath);
            }
            catch (Exception e)
            {
                Loader.Error($"Failed to delete profile file '{name}': {e.Message}");
            }
            var list = new List<string>(Settings.ProfileNames);
            list.Remove(name);
            Settings.ProfileNames = list.ToArray();
            SaveMetaOnly();
        }

        /// <summary>
        /// Rename a profile / 重命名配置
        /// </summary>
        private void RenameProfile(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) return;
            newName = SanitizeFileName(newName.Trim());
            if (oldName == newName) return;
            // Case-insensitive: on NTFS a case variant names the same file, so the File.Delete below
            // would remove that other profile before the move. (oldName vs newName stays an exact
            // compare — a case-only rename is a legitimate way to fix a profile's casing.)
            // 大小写不敏感：NTFS 上大小写变体指向同一文件，否则下面的 File.Delete 会在移动前删掉
            // 那个 Profile。（oldName 与 newName 仍精确比较——仅改大小写的重命名是合法操作。）
            if (Settings.ProfileNames != null && Settings.ProfileNames.Any(p => string.Equals(SanitizeFileName(p), newName, StringComparison.OrdinalIgnoreCase))) return;
            string oldPath = GetProfilePath(oldName);
            string newPath = GetProfilePath(newName);
            if (oldPath != newPath)
            {
                try
                {
                    if (File.Exists(oldPath))
                    {
                        if (File.Exists(newPath))
                            File.Delete(newPath);
                        File.Move(oldPath, newPath);
                    }
                }
                catch (Exception e)
                {
                    Loader.Error($"Failed to rename profile file '{oldName}' → '{newName}': {e.Message}");
                }
            }
            var list = new List<string>(Settings.ProfileNames);
            int idx = list.IndexOf(oldName);
            if (idx >= 0) list[idx] = newName;
            else list.Add(newName);
            Settings.ProfileNames = list.ToArray();
            if (Settings.CurrentProfile == oldName)
                Settings.CurrentProfile = newName;
            SaveSettings();
        }

        /// <summary>
        /// Sync ProfileNames with actual files on disk — remove entries with no file, recreate Default if empty / 同步配置列表与磁盘文件 — 移除无对应文件的条目，空列表时重建 Default
        /// </summary>
        private void SyncProfilesWithDisk()
        {
            if (!Directory.Exists(ProfileDir))
            {
                Directory.CreateDirectory(ProfileDir);
                Settings.ProfileNames = new[] { "Default" };
                Settings.CurrentProfile = "Default";
                SaveCurrentProfile();
                SaveMetaOnly();
                return;
            }
            var valid = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in Settings.ProfileNames ?? Array.Empty<string>())
            {
                string sp = SanitizeFileName(p);
                if (File.Exists(GetProfilePath(p)) && seen.Add(sp))
                    valid.Add(p);
            }
            var nameSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            nameSeen.UnionWith(valid.Select(v => SanitizeFileName(v)));
            foreach (string filePath in Directory.GetFiles(ProfileDir, "*.json"))
            {
                string name = Path.GetFileNameWithoutExtension(filePath);
                if (nameSeen.Add(SanitizeFileName(name)))
                    valid.Add(name);
            }
            bool changed = valid.Count != (Settings.ProfileNames?.Length ?? 0)
                || !valid.SequenceEqual(Settings.ProfileNames ?? Array.Empty<string>());
            if (valid.Count == 0)
            {
                valid.Add("Default");
                Settings.CurrentProfile = "Default";
                SaveCurrentProfile();
                changed = true;
            }
            Settings.ProfileNames = valid.ToArray();
            if (!valid.Contains(Settings.CurrentProfile))
            {
                SwitchProfile(valid[0]);
                return;
            }
            if (changed)
                SaveMetaOnly();
        }

        [System.Serializable]
        private class SettingsMeta
        {
            public int Version = 5;
            public string CurrentProfile = "Default";
            public string[] ProfileNames = new[] { "Default" };
            public string Language = "en";
            public int UiTab;
        }

    }
}
