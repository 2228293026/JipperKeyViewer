// Central key-state source with exactly two origins:
// 1) Physical keys — plain UnityEngine.Input per-frame polling, the same as the mod always
//    used. (Proven by probe: the game's async-input mode does NOT hide physical keys from
//    Unity Input, and replay systems do not inject at OS level — a millisecond polling
//    thread added nothing and leaked keystrokes from other windows when unfocused.)
// 2) Replay keys — the replay bootstrap (TogetherBootstrap & co.) detects a mod named
//    KeyViewer and Harmony-patches its KeyViewer.Core.Input.KeyInput.GetKey to feed the
//    replay's real key state. The TgtCompat shim project ships that exact name/API; this
//    file binds the patched method by reflection so replayed presses reach every consumer.
// / 按键状态源，恰好两条来路：
// 1) 物理按键——直接逐帧 UnityEngine.Input，与 mod 一直以来的做法相同（探针已证明：游戏
//    异步输入模式不会对 Unity Input 隐藏物理按键；回放系统也不在 OS 层注入——毫秒轮询线程
//    毫无增益，反而在失焦时把别的窗口的按键泄漏到画面上）。
// 2) 回放按键——回放引导器（TogetherBootstrap 等）会检测名为 KeyViewer 的 mod，并对其
//    KeyViewer.Core.Input.KeyInput.GetKey 打 Harmony 补丁注入回放真实按键状态。TgtCompat
//    垫片工程提供同名同 API 的替身；本文件经反射绑定补丁后的方法，让回放按键流入所有
//    消费端。

using System;
using System.Reflection;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    internal static class KeySource
    {
        private static Func<KeyCode, bool> _shimGetKey;
        private static bool _shimResolved;
        private static bool _shimLoadAttempted;

        /// <summary>Make the embedded shim assembly exist in the AppDomain. Called at mod
        /// init (before the replay bootstrap's startup scan) and defensively from the lazy
        /// bind. If a real KeyViewer mod is already installed, that one wins.
        /// / 让内嵌垫片程序集出现在 AppDomain。mod 初始化时调用（早于回放引导器的启动扫
        /// 描），懒绑定处再兜底一次。若玩家已安装真正的 KeyViewer mod，则优先用它。</summary>
        internal static void EnsureShimLoaded()
        {
            if (_shimLoadAttempted) return;
            _shimLoadAttempted = true;
            try
            {
                if (Array.Find(AppDomain.CurrentDomain.GetAssemblies(),
                        a => a.GetName().Name == "KeyViewer") != null)
                    return; // a real KeyViewer mod is installed / 已安装真正的 KeyViewer mod
                const string resourceName = "JipperKeyViewer.TgtCompat.KeyViewer.dll";
                var stream = typeof(KeySource).Assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    Loader.Warning("TGT shim resource missing: " + resourceName);
                    return;
                }
                using (stream)
                {
                    var bytes = new byte[stream.Length];
                    int read = 0;
                    while (read < bytes.Length)
                        read += stream.Read(bytes, read, bytes.Length - read);
                    Assembly.Load(bytes);
                }
                Loader.Log("TGT compat shim loaded from embedded resource");
            }
            catch (Exception e)
            {
                Loader.Warning("TGT shim embedded load failed: " + e.Message);
            }
        }

        public static bool GetKey(KeyCode code)
        {
            if (ShimGetKey(code)) return true;
            return Input.GetKey(code);
        }

        private static bool ShimGetKey(KeyCode code)
        {
            if (!_shimResolved)
            {
                _shimResolved = true;
                EnsureShimLoaded();
                try
                {
                    var asm = Array.Find(AppDomain.CurrentDomain.GetAssemblies(),
                        a => a.GetName().Name == "KeyViewer");
                    var mi = asm?.GetType("KeyViewer.Core.Input.KeyInput")
                        ?.GetMethod("GetKey", new[] { typeof(KeyCode) });
                    if (mi != null && mi.IsStatic)
                        _shimGetKey = (Func<KeyCode, bool>)Delegate.CreateDelegate(
                            typeof(Func<KeyCode, bool>), mi);
                    if (_shimGetKey != null)
                        Loader.Log("TGT compat shim bound: KeyViewer.Core.Input.KeyInput.GetKey");
                }
                catch (Exception e)
                {
                    Loader.Warning("TGT compat shim lookup failed: " + e.Message);
                }
            }
            return _shimGetKey != null && _shimGetKey(code);
        }
    }
}
