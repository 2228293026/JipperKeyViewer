using UnityEditor;
using System.IO;

public static class BuildAssetBundles
{
    [MenuItem("Tools/Build KeyViewer AssetBundle")]
    public static void Build()
    {
        string outputDir = "AssetBundles";
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        const string bundleName = "keyviewer_resources_2022";
        SetBundleName(bundleName);
        BuildPipeline.BuildAssetBundles(outputDir,
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows64);
        SetBundleName("keyviewer_resources"); // restore

        System.Diagnostics.Process.Start("explorer.exe", Path.GetFullPath(outputDir));
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
