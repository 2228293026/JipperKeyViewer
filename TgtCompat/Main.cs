// Shim entry: mirrors the reference KeyViewer's UMM entry point. The body is a no-op —
// this assembly exists so the game's replay bootstrap finds the mod it knows how to patch.
// / 垫片入口：镜像参考 KeyViewer 的 UMM 入口。方法体为空——本程序集的意义在于让回放引导器
// 找到它认识并会打补丁的那个 mod。

namespace KeyViewer
{
    public static class Main
    {
        public static void Load(UnityModManagerNet.UnityModManager.ModEntry modEntry) { }
    }
}
