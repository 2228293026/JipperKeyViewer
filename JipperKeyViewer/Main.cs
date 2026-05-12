// UnityModManager entry point / UnityModManager 入口
// Handles mod toggle, GUI, and save lifecycle / 处理 Mod 开关、GUI 和保存生命周期
using JipperKeyViewer.KeyViewer;
using UnityModManagerNet;
using UnityEngine;

namespace JipperKeyViewer
{
    /// <summary>
    /// UnityModManager mod entry point / Mod 入口类
    /// </summary>
    public class Main
    {
        /// <summary>Reference to the mod entry for logging and path resolution / Mod 条目引用，用于日志和路径</summary>
        public static UnityModManager.ModEntry Mod { get; private set; }

        /// <summary>The persistent GameObject hosting the KeyViewer component / 持有 KeyViewer 组件的持久化 GameObject</summary>
        static GameObject KeyViewerGO;

        /// <summary>
        /// Called by UnityModManager to initialize the mod / 由 UnityModManager 调用以初始化 Mod
        /// </summary>
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Mod = modEntry;
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = (entry) => KeyViewer.KeyViewer.instance?.DrawSettingsWindow();
            modEntry.OnSaveGUI = (entry) => KeyViewer.KeyViewer.instance?.SaveSettings();
            modEntry.OnHideGUI = (entry) => KeyViewer.KeyViewer.instance?.SaveSettings();
            return true;
        }

        /// <summary>
        /// Called when the user toggles the mod on/off in UnityModManager / 用户在 UnityModManager 中开关 Mod 时调用
        /// Creates or destroys the persistent KeyViewer GameObject / 创建或销毁持久的 KeyViewer GameObject
        /// </summary>
        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (value)
            {
                if (KeyViewerGO == null)
                {
                    KeyViewerGO = new GameObject("JipperKeyViewer");
                    GameObject.DontDestroyOnLoad(KeyViewerGO);
                    KeyViewerGO.AddComponent<KeyViewer.KeyViewer>();
                }
            }
            else
            {
                if (KeyViewerGO != null)
                {
                    GameObject.Destroy(KeyViewerGO);
                    KeyViewerGO = null;
                }
            }
            return true;
        }
    }
}
