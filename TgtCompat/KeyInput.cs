// Shim mirror of the reference KeyViewer's central input funnel. Every public member has
// the same name and signature as the original — the replay bootstrap's Harmony patch (which
// targets these members by name) attaches here and injects replay key state; JipperKeyViewer
// reads the patched result by reflection. Unpatched, the fallback is plain Unity input.
// / 参考版 KeyViewer 输入总线的镜像。公开成员与原版同名同签名——回放引导器的 Harmony 补丁按
// 名字挂到这里并注入回放按键状态；JipperKeyViewer 经反射读取补丁后的结果。未被补丁时回落
// Unity 原生输入。

using UnityEngine;
using SyncInput = UnityEngine.Input;

namespace KeyViewer.Core.Input
{
    public static class KeyInput
    {
        public static bool AnyKey => SyncInput.anyKey;
        public static bool AnyKeyDown => SyncInput.anyKeyDown;

        public static bool Shift => GetKey(KeyCode.LeftShift) || GetKey(KeyCode.RightShift);
        public static bool Control => GetKey(KeyCode.LeftControl) || GetKey(KeyCode.RightControl);
        public static bool Alt => GetKey(KeyCode.LeftAlt) || GetKey(KeyCode.RightAlt);

        public static bool GetKey(KeyCode code) => SyncInput.GetKey(code);
        public static bool GetKeyDown(KeyCode code) => SyncInput.GetKeyDown(code);
        public static bool GetKeyUp(KeyCode code) => SyncInput.GetKeyUp(code);
    }
}
