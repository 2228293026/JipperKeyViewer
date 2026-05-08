using JipperKeyViewer.KeyViewer;
using System.Reflection;
using UnityModManagerNet;
using UnityEngine;

namespace JipperKeyViewer
{
    public class Main
    {
        public static UnityModManager.ModEntry Mod { get; private set; }

        static GameObject KeyViewerGO;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Mod = modEntry;

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = (entry) => KeyViewer.KeyViewer.instance?.DrawSettingsWindow();
            modEntry.OnSaveGUI = (entry) => KeyViewer.KeyViewer.instance?.SaveSettings();
            modEntry.OnHideGUI = (entry) => KeyViewer.KeyViewer.instance?.SaveSettings();
            return true;
        }

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
