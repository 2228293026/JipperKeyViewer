// AssetBundle and font management / AssetBundle 和字体管理
// Loads built-in sprites, game fonts, custom font files, and sets up shadow materials and fallback chains / 加载内置精灵、游戏字体、自定义字体文件，设置阴影材质和后备链

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

using JipperKeyViewer.KeyViewer.Settings;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>
    /// Resource loading: AssetBundle sprites, font scanning, shadow material creation / 资源加载：AssetBundle 精灵、字体扫描、阴影材质创建
    /// </summary>
    public partial class KeyViewer : MonoBehaviour
    {
        /// <summary>
        /// Scan for traditional Unity Font objects in the scene and convert them to TMP_FontAsset / 扫描场景中的传统 Unity Font 对象并转换为 TMP_FontAsset
        /// This allows the mod to use any font the game itself uses / 这使 Mod 可以使用游戏本身使用的任何字体
        /// </summary>
        void ScanGameFonts()
        {
            var allFonts = Resources.FindObjectsOfTypeAll<Font>();
            if (allFonts == null || allFonts.Length == 0)
                return;

            int added = 0;
            foreach (var font in allFonts)
            {
                bool exists = false;
                foreach (var e in fontList)
                    if (e.sourceFontName == font.name) { exists = true; break; }
                if (exists) continue;

                var tmpFont = TMP_FontAsset.CreateFontAsset(font);
                if (tmpFont != null)
                {
                    var entry = new FontEntry(font.name, tmpFont);
                    entry.sourceFontName = font.name;
                    fontList.Add(entry);
                    added++;
                }
            }

            if (added > 0)
                Loader.Log($"KeyViewer: Converted {added} traditional font(s) to TMP_FontAsset");
        }

        /// <summary>
        /// Load AssetBundle, game fonts, and custom fonts / 加载 AssetBundle、游戏字体和自定义字体
        /// Returns false if the AssetBundle cannot be loaded / 如果无法加载 AssetBundle 则返回 false
        /// </summary>
        private bool TryLoadResources()
        {
            if (keyBackgroundSprite != null) return true;

            // Destroy the previous dynamically-created assets before dropping the references —
            // TMP_FontAssets carry atlas textures/materials; without this, every loader-level
            // toggle (UMM off→on) or failed-bundle retry leaked the whole set.
            // 清空前先销毁旧的动态创建资产——TMP_FontAsset 持有图集纹理/材质;否则每次加载器级
            // 开关(UMM 关→开)或 bundle 失败重试都会泄漏一整套。
            foreach (var e in fontList)
                if (e.font != null) Destroy(e.font);
            fontList.Clear();
            foreach (var m in shadowMaterials.Values)
                if (m != null) Destroy(m);
            shadowMaterials.Clear();

            string modPath = Loader.ModPath;
            string assetsDir = Path.Combine(modPath, "assets");

            string bundlePath = Path.Combine(assetsDir, "keyviewer_resources");

            var bundle = AssetBundle.LoadFromFile(bundlePath);

            ScanGameFonts();

            if (bundle != null)
            {
                keyBackgroundSprite = bundle.LoadAsset<Sprite>("KeyBackground");
                keyOutlineSprite = bundle.LoadAsset<Sprite>("KeyOutline");

                Font mapleOTF = bundle.LoadAsset<Font>("MAPLESTORY_OTF_BOLD");
                if (mapleOTF != null)
                {
                    mapleFont = TMP_FontAsset.CreateFontAsset(mapleOTF);
                    // CreateFontAsset can return null (unreadable/unsupported font) — a null entry
                    // here would render as an empty row in the font list. ScanCustomFonts already
                    // null-checks; these two paths now match it.
                    // CreateFontAsset 可能返回 null(不可读/不支持的字体)——null 条目会在字体列表
                    // 中渲染成空行。ScanCustomFonts 已判空;这两处现在对齐。
                    if (mapleFont != null)
                    {
                        var entry = new FontEntry("MapleStory", mapleFont);
                        entry.sourceFontName = "MAPLESTORY_OTF_BOLD";
                        fontList.Add(entry);
                    }
                    else
                    {
                        Loader.Error("KeyViewer: TMP_FontAsset.CreateFontAsset failed for MAPLESTORY_OTF_BOLD");
                    }
                }
                else
                {
                    Loader.Error("KeyViewer: MAPLESTORY_OTF_BOLD not found in AB");
                }

                Font cjkOTF = bundle.LoadAsset<Font>("cjkFonts-regular-normalized");
                if (cjkOTF != null)
                {
                    var cjkFont = TMP_FontAsset.CreateFontAsset(cjkOTF);
                    // Null CJK font breaks the whole fallback chain (CJK labels render as boxes);
                    // don't insert the entry when creation failed — insert(0) would occupy the
                    // default slot with a dead font.
                    // CJK 字体为 null 会破坏整条后备链(中文渲染成方块);创建失败时不要插入条目
                    // ——Insert(0) 会把默认槽位让给死字体。
                    if (cjkFont != null)
                    {
                        var entry = new FontEntry("CJK (Default)", cjkFont);
                        entry.sourceFontName = "cjkFonts-regular-normalized";
                        fontList.Insert(0, entry);
                    }
                    else
                    {
                        Loader.Error("KeyViewer: TMP_FontAsset.CreateFontAsset failed for cjkFonts-regular-normalized (CJK labels render as boxes)");
                    }
                }
                else
                {
                    Loader.Error("KeyViewer: cjkFonts-regular-normalized not found in AB");
                }
                if (keyBackgroundSprite == null)
                    Loader.Error("KeyViewer: KeyBackground not found in AssetBundle");
                if (keyOutlineSprite == null)
                    Loader.Error("KeyViewer: KeyOutline not found in AssetBundle");

                ghostRainSprite = bundle.LoadAsset<Sprite>("GhostRain");
                if (ghostRainSprite == null)
                    Loader.Warning("KeyViewer: GhostRain not found in AssetBundle");

                bundle.Unload(false);
            }
            else
            {
                Loader.Error($"KeyViewer: Cannot load AssetBundle at {bundlePath}");
            }

            ScanCustomFonts();
            LinkFallbackFonts();

            if (Settings.Data.FontIndex >= fontList.Count)
                Settings.Data.FontIndex = 0;

            fontNameIndex = new Dictionary<string, int>(fontList.Count);
            for (int i = 0; i < fontList.Count; i++)
                fontNameIndex[fontList[i].name] = i;

            return bundle != null;
        }

        /// <summary>
        /// Get the currently selected font from the font list / 从字体列表中获取当前选中的字体
        /// </summary>
        private TMP_FontAsset GetCurrentFont()
        {
            return fontList.Count > 0 ? fontList[Mathf.Clamp(Settings.Data.FontIndex, 0, fontList.Count - 1)].font : null;
        }

        /// <summary>
        /// Update the font on all key text elements / 更新所有按键文本元素的字体
        /// Called when the user changes font selection / 用户更改字体选择时调用
        /// </summary>
        private void UpdateAllFonts()
        {
            TMP_FontAsset currentFont = GetCurrentFont();
            if (currentFont == null) return;
            Material shadowMat = GetShadowMaterial(currentFont);
            FontStyles style = (FontStyles)Settings.Data.FontStyleFlags;
            void UpdateText(TMP_Text t)
            {
                if (t == null) return;
                t.font = currentFont;
                t.fontMaterial = shadowMat;
                t.fontStyle = style;
                t.fontSizeMax = Settings.Data.KeyFontSize;
            }
            bool hasPerKey = Settings.Data.EnablePerKeyTextSize;
            void ApplyPerKeyOverride(TMP_Text t, int pi)
            {
                if (t == null || !hasPerKey || pi < 0 || pi >= Settings.Data.PerKeyFontSize.Length) return;
                float fs = Settings.Data.PerKeyFontSize[pi];
                if (fs > 0f) t.fontSizeMax = fs;
            }
            if (Keys != null)
            {
                for (int i = 0; i < Keys.Length; i++)
                {
                    if (Keys[i] == null) continue;
                    int pi = i;
                    UpdateText(Keys[i].text);
                    ApplyPerKeyOverride(Keys[i].text, pi);
                    // value: reset FIRST, then override — the old order let UpdateText's
                    // unconditional fontSizeMax write clobber the per-key size (the Kps/Total
                    // blocks below already had the correct order).
                    // value:先重置后覆盖——旧顺序会让 UpdateText 的无条件 fontSizeMax 写入
                    // 抹掉每键字号(下方 Kps/Total 段原本顺序就正确)。
                    UpdateText(Keys[i].value);
                    ApplyPerKeyOverride(Keys[i].value, pi);
                }
            }
            int kpsPi = MaxKeySlots;
            int totalPi = MaxKeySlots + 1;
            // Explicit null checks, not `?.` — Key is a MonoBehaviour, and the null-conditional
            // bypasses Unity's destroyed-check (a destroyed component would slip through and only
            // survive via the later Unity-overload checks). / 显式判空而非 `?.`——Key 是
            // MonoBehaviour，空条件运算符绕过 Unity 的销毁检查（已销毁组件会漏进来，仅靠后续
            // Unity 重载检查兜底）。
            if (Kps != null)
            {
                UpdateText(Kps.text);
                ApplyPerKeyOverride(Kps.text, kpsPi);
                UpdateText(Kps.value);
                ApplyPerKeyOverride(Kps.value, kpsPi);
            }
            if (Total != null)
            {
                UpdateText(Total.text);
                ApplyPerKeyOverride(Total.text, totalPi);
                UpdateText(Total.value);
                ApplyPerKeyOverride(Total.value, totalPi);
            }
        }

        /// <summary>
        /// Get or create a shadow material for the given font / 获取或为指定字体创建阴影材质
        /// Uses the "UNDERLAY_ON" shader keyword for TMP drop shadow / 使用 TMP 的 "UNDERLAY_ON" 着色器关键字实现投影
        /// Materials are cached and reused / 材质会被缓存和复用
        /// </summary>
        Material GetShadowMaterial(TMP_FontAsset font)
        {
            if (font == null) return null;
            if (shadowMaterials.TryGetValue(font, out var mat)) return mat;

            var fontMat = GetFontMaterial(font);
            if (fontMat == null)
            {
                Loader.Error("KeyViewer: Cannot get material from font asset, skipping shadow");
                return null;
            }
            mat = new Material(fontMat);
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.5f));
            mat.SetFloat("_UnderlayOffsetX", 1f);
            mat.SetFloat("_UnderlayOffsetY", -1f);
            mat.SetFloat("_UnderlaySoftness", 0f);
            shadowMaterials[font] = mat;
            return mat;
        }

        static MemberInfo cachedMaterialMember;
        static bool cachedMaterialLogged;

        /// <summary>
        /// Get material from TMP_FontAsset via reflection (handles API differences across Unity/TMP versions) / 通过反射从 TMP_FontAsset 获取材质（处理不同 Unity/TMP 版本的 API 差异）
        /// </summary>
        static Material GetFontMaterial(TMP_FontAsset font)
        {
            if (cachedMaterialMember == null)
            {
                var t = font.GetType();
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
                cachedMaterialMember = (MemberInfo)t.GetProperty("material", flags) ?? t.GetField("material", flags);
            }

            Material result = null;
            if (cachedMaterialMember is PropertyInfo pi)
            {
                var val = pi.GetValue(font);
                if (val != null) result = (Material)val;
            }
            else if (cachedMaterialMember is FieldInfo fi)
            {
                var val = fi.GetValue(font);
                if (val != null) result = (Material)val;
            }

            if (!cachedMaterialLogged)
            {
                cachedMaterialLogged = true;
                string foundBy = cachedMaterialMember != null
                    ? $"{cachedMaterialMember.MemberType} \"{cachedMaterialMember.Name}\""
                    : "none";
                Loader.Log($"KeyViewer: Font material resolved via {foundBy}");
            }
            return result;
        }

        /// <summary>
        /// Link CJK font as fallback to all other fonts so Chinese characters display correctly / 将 CJK 字体链接为所有其他字体的后备字体，使中文字符正确显示
        /// </summary>
        static void LinkFallbackFonts()
        {
            FontEntry cjkEntry = null;
            foreach (var e in fontList)
                if (e.name == "CJK (Default)") { cjkEntry = e; break; }
            if (cjkEntry?.font == null) return;

            foreach (var entry in fontList)
            {
                if (entry.font == null || entry == cjkEntry) continue;
                if (entry.font.fallbackFontAssetTable == null)
                    entry.font.fallbackFontAssetTable = new List<TMP_FontAsset>();
                if (!entry.font.fallbackFontAssetTable.Contains(cjkEntry.font))
                    entry.font.fallbackFontAssetTable.Add(cjkEntry.font);
            }
        }

        /// <summary>
        /// Scan the CustomFont directory for .ttf and .otf files and load them as TMP_FontAsset / 扫描 CustomFont 目录中的 .ttf 和 .otf 文件并将其作为 TMP_FontAsset 加载
        /// </summary>
        void ScanCustomFonts()
        {
            string modPath = Loader.ModPath;
            string customFontDir = Path.Combine(modPath, "CustomFont");

            if (!Directory.Exists(customFontDir))
            {
                Directory.CreateDirectory(customFontDir);
                Loader.Log($"KeyViewer: Created CustomFont directory at {customFontDir}");
                return;
            }

            string[] ttfFiles = Directory.GetFiles(customFontDir, "*.ttf", SearchOption.TopDirectoryOnly);
            string[] otfFiles = Directory.GetFiles(customFontDir, "*.otf", SearchOption.TopDirectoryOnly);
            string[] fontFiles = new string[ttfFiles.Length + otfFiles.Length];
            Array.Copy(ttfFiles, fontFiles, ttfFiles.Length);
            Array.Copy(otfFiles, 0, fontFiles, ttfFiles.Length, otfFiles.Length);

            if (fontFiles.Length == 0)
            {
                Loader.Log($"KeyViewer: No .ttf/.otf files found in CustomFont directory");
                return;
            }

            foreach (string fontPath in fontFiles)
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(fontPath);
                    string entryName = $"Custom: {fileName}";

                    // Avoid duplicates by checking existing entries / 检查已有条目以避免重复
                    bool exists = false;
                    foreach (var e in fontList)
                    {
                        if (e.name.Equals(entryName, StringComparison.OrdinalIgnoreCase))
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (exists)
                    {
                        Loader.Log($"KeyViewer: Custom font '{fileName}' already loaded, skipping");
                        continue;
                    }

                    Font font = new Font(fontPath);
                    TMP_FontAsset tmpFont = TMP_FontAsset.CreateFontAsset(font);
                    if (tmpFont != null)
                    {
                        fontList.Add(new FontEntry(entryName, tmpFont));
                    }
                    else
                    {
                        Loader.Error($"KeyViewer: Failed to create TMP_FontAsset from '{fontPath}'");
                    }
                }
                catch (Exception e)
                {
                    Loader.Error($"KeyViewer: Failed to load custom font '{fontPath}': {e.Message}");
                }
            }
        }
    }
}
