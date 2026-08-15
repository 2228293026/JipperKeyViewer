using UnityEditor;
using UnityEngine;
using System.IO;

public static class BuildAssetBundles
{
    [MenuItem("Tools/Build KeyViewer AssetBundle")]
    public static void Build()
    {
        string outputDir = "AssetBundles";
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        EnsureSpriteImportSettings();
        const string bundleName = "keyviewer_resources_6000";
        SetBundleName(bundleName);
        BuildPipeline.BuildAssetBundles(outputDir,
            BuildAssetBundleOptions.AssetBundleStripUnityVersion | BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows64);
        SetBundleName("keyviewer_resources"); // restore

        System.Diagnostics.Process.Start("explorer.exe", Path.GetFullPath(outputDir));
    }

    // The .meta files are not version-controlled; a fresh clone re-imports the PNGs with border 0,
    // which would silently collapse the 9-sliced key frames and the bordered ghost-rain tiling in
    // BOTH mods (the renderers read sprite.border at runtime). Enforce the import settings here so
    // a rebuilt bundle always carries the 11px borders regardless of local .meta state.
    // .meta 文件未纳入版本管理；新克隆的项目会以 0 边框重新导入 PNG，静默破坏两个变体的九宫格
    // 按键框与带边框鬼雨平铺（渲染器在运行时读取 sprite.border）。在此强制导入设置，保证重建的
    // bundle 无论本地 .meta 状态如何都带 11px 边框。
    static void EnsureSpriteImportSettings()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
            string file = Path.GetFileName(path);
            if (file != "KeyBackground.png" && file != "KeyOutline.png" && file != "GhostRain.png") continue;
            bool changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteBorder != new Vector4(11, 11, 11, 11)
                || !Mathf.Approximately(importer.spritePixelsPerUnit, 100f);
            if (!changed) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteBorder = new Vector4(11, 11, 11, 11);
            importer.spritePixelsPerUnit = 100f;
            importer.SaveAndReimport();
        }
    }

    static void SetBundleName(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture t:Font"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path);
            if (importer.assetBundleName != name)
            {
                importer.assetBundleName = name;
                importer.SaveAndReimport();
            }
        }
    }
}
