// Mod core entry point — loader-agnostic / Mod 核心入口 — 加载器无关
// Called by loader-specific assemblies (UMM, MelonLoader) / 由加载器专属程序集调用
using JipperKeyViewer.KeyViewer;
using UnityEngine;

namespace JipperKeyViewer
{
    /// <summary>
    /// Mod core entry point / Mod 核心入口
    /// Initialises the mod when called by a loader-specific assembly.
    /// 由加载器专属程序集调用以初始化 Mod。
    /// </summary>
    public static class Main
    {
        /// <summary>The persistent GameObject hosting the KeyViewer component / 持有 KeyViewer 组件的持久化 GameObject</summary>
        static GameObject KeyViewerGO;
        /// <summary>Init guard — a second Init call would double-subscribe every loader event. / Init 防护——二次调用会让每个加载器事件被重复订阅。</summary>
        static bool initialized;

        /// <summary>
        /// Initialise the mod with the given loader implementation / 使用指定的加载器实现初始化 Mod
        /// Called by loader-specific entry points (UMM, MelonLoader).
        /// 由加载器专属入口（UMM、MelonLoader）调用。
        /// </summary>
        public static void Init(IModLoader loader)
        {
            // Defensive: both shipped loaders call Init exactly once, but a double call would
            // double-subscribe OnToggle/OnGUI/OnSaveGUI (duplicate overlay toggling, double saves).
            // 防御性:两个加载器都只调一次 Init,但重复调用会双重订阅 OnToggle/OnGUI/OnSaveGUI
            //(覆盖层重复开关、双重保存)。
            if (initialized) return;
            initialized = true;

            Loader.Instance = loader;

            loader.OnToggle += (enabled) =>
            {
                if (enabled) EnableKeyViewer();
                else DisableKeyViewer();
            };

            // Unity-aware null checks (?. bypasses the destroyed-object check on UnityEngine.Object) /
            // Unity 感知的空检查（?. 会绕过 UnityEngine.Object 的已销毁判断）
            loader.OnGUI += () => { var kv = KeyViewer.KeyViewer.instance; if (kv != null) kv.DrawSettingsWindow(); };
            loader.OnSaveGUI += () => { var kv = KeyViewer.KeyViewer.instance; if (kv != null) kv.SaveSettings(); };
        }

        /// <summary>
        /// Call this after Init() if the loader doesn't fire OnToggle (e.g. MelonLoader).
        /// Ensures the overlay is created immediately.
        /// 在 Init() 之后调用，用于不会触发 OnToggle 的加载器（如 MelonLoader）。
        /// 确保立即创建覆盖层。
        /// </summary>
        public static void EnableNow()
        {
            EnableKeyViewer();
        }

        internal static void EnableKeyViewer()
        {
            if (KeyViewerGO != null) return;
            KeyViewerGO = new GameObject("JipperKeyViewer");
            GameObject.DontDestroyOnLoad(KeyViewerGO);
            KeyViewerGO.AddComponent<KeyViewer.KeyViewer>();
        }

        internal static void DisableKeyViewer()
        {
            if (KeyViewerGO == null) return;
            GameObject.Destroy(KeyViewerGO);
            KeyViewerGO = null;
        }
    }
}
