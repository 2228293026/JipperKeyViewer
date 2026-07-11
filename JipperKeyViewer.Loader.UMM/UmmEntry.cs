using JipperKeyViewer;
using System;
using System.IO;
using UnityModManagerNet;

namespace JipperKeyViewer.Loader
{
    /// <summary>
    /// UMM entry point — called by UnityModManager via Info.json / UMM 入口 — 由 UnityModManager 通过 Info.json 调用
    /// "EntryMethod": "JipperKeyViewer.Loader.UmmEntry.Load"
    /// </summary>
    public static class UmmEntry
    {
        public static bool Load(UnityModManager.ModEntry entry)
        {
            var handler = new UmmHandler(entry);
            Main.Init(handler);
            return true;
        }
    }

    /// <summary>
    /// IModLoader implementation for UnityModManager / UnityModManager 的 IModLoader 实现
    /// </summary>
    class UmmHandler : IModLoader
    {
        readonly UnityModManager.ModEntry _entry;

        public string ModPath => Path.GetDirectoryName(_entry.Path);

        public event Action<float> OnUpdate;
        public event Action<bool> OnToggle;
        public event Action OnGUI;
        public event Action OnSaveGUI;

        public UmmHandler(UnityModManager.ModEntry entry)
        {
            _entry = entry;
            _entry.OnUpdate = (UnityModManager.ModEntry e, float dt) => OnUpdate?.Invoke(dt);
            _entry.OnToggle = (UnityModManager.ModEntry e, bool v) => { OnToggle?.Invoke(v); return true; };
            _entry.OnGUI = (UnityModManager.ModEntry e) => OnGUI?.Invoke();
            _entry.OnSaveGUI = (UnityModManager.ModEntry e) => OnSaveGUI?.Invoke();
            _entry.OnHideGUI = (UnityModManager.ModEntry e) => OnSaveGUI?.Invoke();
        }

        public void Log(string msg)      => _entry.Logger.Log(msg);
        public void Warning(string msg)  => _entry.Logger.Warning(msg);
        public void Error(string msg)    => _entry.Logger.Error(msg);
    }
}
