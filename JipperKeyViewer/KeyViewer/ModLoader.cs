// Mod loader abstraction / Mod 加载器抽象层
// Decouples mod core from UnityModManager / MelonLoader / etc.
// 将 Mod 核心与 UnityModManager / MelonLoader 等解耦

using System;

namespace JipperKeyViewer
{
    /// <summary>
    /// Abstract mod loader interface / 抽象 Mod 加载器接口
    /// Each supported mod loader (UMM, MelonLoader) implements this.
    /// 每个受支持的 Mod 加载器（UMM、MelonLoader）实现此接口。
    /// </summary>
    public interface IModLoader
    {
        /// <summary>Mod installation directory path / Mod 安装目录路径</summary>
        string ModPath { get; }

        /// <summary>Log an informational message / 记录信息日志</summary>
        void Log(string message);
        /// <summary>Log a warning / 记录警告</summary>
        void Warning(string message);
        /// <summary>Log an error / 记录错误</summary>
        void Error(string message);

        /// <summary>Called every frame / 每帧调用</summary>
        event Action<float> OnUpdate;
        /// <summary>Called when the mod is toggled on/off / 开关 Mod 时调用</summary>
        event Action<bool> OnToggle;
        /// <summary>Called to draw the settings GUI / 绘制设置 GUI 时调用</summary>
        event Action OnGUI;
        /// <summary>Called when settings should be saved / 需要保存设置时调用</summary>
        event Action OnSaveGUI;

        /// <summary>
        /// Optional hook for the loader to draw extra settings rows inside the shared
        /// settings window (e.g. MelonLoader hotkey binding). UMM leaves this empty.
        /// 加载器在共享设置窗口内绘制额外设置行的可选钩子（如 MelonLoader 热键绑定）。
        /// UMM 留空。
        /// </summary>
        void DrawExtraSettings();
    }

    /// <summary>
    /// Static accessor for the active mod loader / 当前活跃 Mod 加载器的静态访问器
    /// All mod code references Loader.Instance instead of Main.Mod directly.
    /// 所有 Mod 代码通过 Loader.Instance 引用，而非直接引用 Main.Mod。
    /// </summary>
    public static class Loader
    {
        public static IModLoader Instance { get; internal set; }

        /// <summary>Mod installation path (shorthand) / Mod 安装路径（简写）</summary>
        public static string ModPath => Instance?.ModPath ?? ".";

        public static void Log(string msg)   { Instance?.Log(msg); }
        public static void Warning(string msg) { Instance?.Warning(msg); }
        public static void Error(string msg) { Instance?.Error(msg); }

        /// <summary>
        /// Invoke the loader's optional extra settings drawing hook, if any.
        /// 调用加载器可选的额外设置绘制钩子（若有）。
        /// </summary>
        public static void DrawExtraSettingsUI() => Instance?.DrawExtraSettings();
    }
}
