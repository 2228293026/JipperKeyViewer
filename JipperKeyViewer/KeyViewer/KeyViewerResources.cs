// File-based resource and font management / 基于文件的资源和字体管理
// Loads built-in sprites from PNG, fonts from OTF/TTF, custom font files, and sets up shadow materials and fallback chains / 从 PNG 文件加载内置精灵，从 OTF/TTF 文件加载字体，以及自定义字体文件，设置阴影材质和后备链

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>
    /// Resource loading: file-based sprites, font scanning, shadow material creation / 资源加载：基于文件的精灵、字体扫描、阴影材质创建
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
                Main.Mod.Logger.Log($"KeyViewer: Converted {added} traditional font(s) to TMP_FontAsset");
        }

        /// <summary>
        /// Load sprites from PNG files, fonts from OTF/TTF files, and custom fonts / 从 PNG 文件加载精灵，从 OTF/TTF 文件加载字体，以及自定义字体
        /// </summary>
        private bool TryLoadResources()
        {
            if (keyBackgroundSprite != null) return true;

            fontList.Clear();
            shadowMaterials.Clear();

            string modPath = Path.GetDirectoryName(Main.Mod?.Path) ?? ".";
            string assetsDir = Path.Combine(modPath, "assets");

            if (!Directory.Exists(assetsDir))
                Main.Mod.Logger.Warning($"KeyViewer: assets/ directory not found at {assetsDir}, bundled resources will be missing");

            ScanGameFonts();

            keyBackgroundSprite = LoadSpriteFromFile(Path.Combine(assetsDir, "KeyBackground.png"));
            keyOutlineSprite = LoadSpriteFromFile(Path.Combine(assetsDir, "KeyOutline.png"));

            LoadFontFromFile(assetsDir, "MAPLESTORY_OTF_BOLD.OTF", "MapleStory", ref mapleFont, fontList);
            LoadCJKFontFromFile(assetsDir, "cjkFonts-regular-normalized.otf", "CJK (Default)", fontList);

            if (keyBackgroundSprite == null)
                Main.Mod.Logger.Warning("KeyViewer: KeyBackground.png not found in assets/");
            if (keyOutlineSprite == null)
                Main.Mod.Logger.Warning("KeyViewer: KeyOutline.png not found in assets/");

            ScanCustomFonts();
            LinkFallbackFonts();

            if (Settings.FontIndex >= fontList.Count)
                Settings.FontIndex = 0;

            fontNameIndex = new Dictionary<string, int>(fontList.Count);
            for (int i = 0; i < fontList.Count; i++)
                fontNameIndex[fontList[i].name] = i;

            return true;
        }

        /// <summary>
        /// Load a PNG file as a Sprite with 9-slice border / 加载 PNG 文件为带九宫格边框的 Sprite
        /// Border values (11px) match the original Unity import settings / 边框值（11px）与原始 Unity 导入设置一致
        /// Uses ImageConversion.LoadImage via reflection since the module isn't referenced at compile time / 通过反射调用 ImageConversion.LoadImage
        /// </summary>
        private static bool _loadImageCached;
        private static MethodInfo _cachedLoadImage;
        private static int _loadImageParamCount;
        private static Sprite LoadSpriteFromFile(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                byte[] bytes = File.ReadAllBytes(path);
                if (!_loadImageCached)
                {
                    _loadImageCached = true;
                    Type type = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
                    if (type == null)
                    {
                        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            type = asm.GetType("UnityEngine.ImageConversion");
                            if (type != null) break;
                        }
                    }
                    if (type != null)
                    {
                        foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                        {
                            if (m.Name != "LoadImage") continue;
                            var parms = m.GetParameters();
                            if (parms.Length >= 2 && parms[0].ParameterType == typeof(Texture2D) && parms[1].ParameterType == typeof(byte[]))
                            {
                                _cachedLoadImage = m;
                                _loadImageParamCount = parms.Length;
                                break;
                            }
                        }
                    }
                    if (_cachedLoadImage == null)
                        Main.Mod.Logger.Error("KeyViewer: ImageConversion.LoadImage not found via reflection, sprites will be missing");
                }
                if (_cachedLoadImage != null)
                {
                    if (_loadImageParamCount == 2)
                        _cachedLoadImage.Invoke(null, new object[] { tex, bytes });
                    else
                        _cachedLoadImage.Invoke(null, new object[] { tex, bytes, false });
                }
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.Tight, new Vector4(11, 11, 11, 11));
            }
            catch (Exception e)
            {
                Main.Mod.Logger.Error($"KeyViewer: Failed to load sprite from '{path}': {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load an OTF/TTF font file and add it to the font list / 加载 OTF/TTF 字体文件并添加到字体列表
        /// </summary>
        private static void LoadFontFromFile(string assetsDir, string fileName, string entryName, ref TMP_FontAsset target, List<FontEntry> fontList)
        {
            string path = Path.Combine(assetsDir, fileName);
            if (!File.Exists(path)) return;
            try
            {
                Font font = new Font(path);
                if (font != null)
                {
                    target = TMP_FontAsset.CreateFontAsset(font);
                    var entry = new FontEntry(entryName, target);
                    entry.sourceFontName = Path.GetFileNameWithoutExtension(fileName);
                    fontList.Add(entry);
                }
            }
            catch (Exception e)
            {
                Main.Mod.Logger.Error($"KeyViewer: Failed to load font '{fileName}': {e.Message}");
            }
        }

        /// <summary>
        /// Load CJK font and insert it at the front of the font list / 加载 CJK 字体并插入到字体列表最前面
        /// </summary>
        private static void LoadCJKFontFromFile(string assetsDir, string fileName, string entryName, List<FontEntry> fontList)
        {
            string path = Path.Combine(assetsDir, fileName);
            if (!File.Exists(path)) return;
            try
            {
                Font font = new Font(path);
                if (font != null)
                {
                    var cjkFont = TMP_FontAsset.CreateFontAsset(font);
                    var entry = new FontEntry(entryName, cjkFont);
                    entry.sourceFontName = Path.GetFileNameWithoutExtension(fileName);
                    fontList.Insert(0, entry);
                }
            }
            catch (Exception e)
            {
                Main.Mod.Logger.Error($"KeyViewer: Failed to load CJK font '{fileName}': {e.Message}");
            }
        }

        /// <summary>
        /// Get the currently selected font from the font list / 从字体列表中获取当前选中的字体
        /// </summary>
        private TMP_FontAsset GetCurrentFont()
        {
            return fontList.Count > 0 ? fontList[Mathf.Clamp(Settings.FontIndex, 0, fontList.Count - 1)].font : null;
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
            void UpdateText(TMP_Text t)
            {
                if (t == null) return;
                t.font = currentFont;
                t.fontMaterial = shadowMat;
            }
            if (Keys != null)
            {
                foreach (Key key in Keys)
                {
                    if (key == null) continue;
                    UpdateText(key.text);
                    UpdateText(key.value);
                }
            }
            UpdateText(Kps?.text);
            UpdateText(Kps?.value);
            UpdateText(Total?.text);
            UpdateText(Total?.value);
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
                Main.Mod.Logger.Error("KeyViewer: Cannot get material from font asset, skipping shadow");
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
                Main.Mod.Logger.Log($"KeyViewer: Font material resolved via {foundBy}");
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
            string modPath = Path.GetDirectoryName(Main.Mod?.Path) ?? ".";
            string customFontDir = Path.Combine(modPath, "CustomFont");

            if (!Directory.Exists(customFontDir))
            {
                Directory.CreateDirectory(customFontDir);
                Main.Mod.Logger.Log($"KeyViewer: Created CustomFont directory at {customFontDir}");
                return;
            }

            string[] ttfFiles = Directory.GetFiles(customFontDir, "*.ttf", SearchOption.TopDirectoryOnly);
            string[] otfFiles = Directory.GetFiles(customFontDir, "*.otf", SearchOption.TopDirectoryOnly);
            string[] fontFiles = new string[ttfFiles.Length + otfFiles.Length];
            Array.Copy(ttfFiles, fontFiles, ttfFiles.Length);
            Array.Copy(otfFiles, 0, fontFiles, ttfFiles.Length, otfFiles.Length);

            if (fontFiles.Length == 0)
            {
                Main.Mod.Logger.Log($"KeyViewer: No .ttf/.otf files found in CustomFont directory");
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
                        Main.Mod.Logger.Log($"KeyViewer: Custom font '{fileName}' already loaded, skipping");
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
                        Main.Mod.Logger.Error($"KeyViewer: Failed to create TMP_FontAsset from '{fontPath}'");
                    }
                }
                catch (Exception e)
                {
                    Main.Mod.Logger.Error($"KeyViewer: Failed to load custom font '{fontPath}': {e.Message}");
                }
            }
        }
    }
}
