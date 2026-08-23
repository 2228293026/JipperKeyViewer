using JipperKeyViewer;
using System;
using System.IO;
using UnityModManagerNet;
using UnityEngine;

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
        // UMM only invokes OnGUI while its panel is shown — remember the last drawn frame and
        // treat "drawn recently" as visible. / UMM 仅在面板显示时调用 OnGUI——记录最近绘制帧,
        // 以"近期绘制过"作为可见判定。
        int _lastGuiFrame = -1000;

        public string ModPath => Path.GetDirectoryName(_entry.Path);

        public bool IsSettingsWindowVisible => Time.frameCount - _lastGuiFrame <= 2;

        // UMM's show/hide hotkey lives in UMM's own config and isn't exposed via ModEntry.
        // / UMM 的显隐热键存于 UMM 自身配置,ModEntry 未暴露。
        public KeyCode SettingsHotkey => KeyCode.None;

        public event Action<float> OnUpdate;
        public event Action<bool> OnToggle;
        public event Action OnGUI;
        public event Action OnSaveGUI;

        public void DrawExtraSettings() { }

        public UmmHandler(UnityModManager.ModEntry entry)
        {
            _entry = entry;
            _entry.OnUpdate = (UnityModManager.ModEntry e, float dt) => OnUpdate?.Invoke(dt);
            _entry.OnToggle = (UnityModManager.ModEntry e, bool v) => { OnToggle?.Invoke(v); return true; };
            _entry.OnGUI = (UnityModManager.ModEntry e) => { _lastGuiFrame = Time.frameCount; OnGUI?.Invoke(); };
            _entry.OnSaveGUI = (UnityModManager.ModEntry e) => OnSaveGUI?.Invoke();
            _entry.OnHideGUI = (UnityModManager.ModEntry e) => OnSaveGUI?.Invoke();
        }

        public void Log(string msg)      => _entry.Logger.Log(msg);
        public void Warning(string msg)  => _entry.Logger.Warning(msg);
        public void Error(string msg)    => _entry.Logger.Error(msg);
    }
}
