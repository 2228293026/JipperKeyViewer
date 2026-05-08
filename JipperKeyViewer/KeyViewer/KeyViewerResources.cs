using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    public partial class KeyViewer : MonoBehaviour
    {
        void ScanGameFonts()
        {
            var allFonts = Resources.FindObjectsOfTypeAll<Font>();
            if (allFonts == null || allFonts.Length == 0)
                return;

            int added = 0;
            foreach (var font in allFonts)
            {
                if (fontList.Exists(e => e.font != null && e.font.name == font.name))
                    continue;

                // ´«Í³ Font ¡ú TMP_FontAsset
                var tmpFont = TMP_FontAsset.CreateFontAsset(font);
                if (tmpFont != null)
                {
                    fontList.Add(new FontEntry(font.name, tmpFont));
                    added++;
                }
            }

            if (added > 0)
                Main.Mod.Logger.Log($"KeyViewer: Converted {added} traditional font(s) to TMP_FontAsset");
        }

        private bool TryLoadResources()
        {
            if (keyBackgroundSprite != null) return true;

            fontList.Clear();
            shadowMaterials.Clear();

            string modPath = Path.GetDirectoryName(Main.Mod?.Path) ?? ".";
            string assetsDir = Path.Combine(modPath, "assets");

            /*
            string unityVersion = Application.unityVersion;
            //Main.Mod.Logger.Log($"KeyViewer: Detected Unity version: {unityVersion}");
            string bundleName = unityVersion.StartsWith("6000") ? "keyviewer_resources_6000" : "keyviewer_resources_2022";
            string bundlePath = Path.Combine(assetsDir, bundleName);
            //Main.Mod.Logger.Log($"KeyViewer: Trying AssetBundle: {bundlePath}");
            */
            string bundlePath = Path.Combine(assetsDir, "keyviewer_resources");

            var bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle != null)
            {
                //Main.Mod.Logger.Log($"KeyViewer: Loaded version-specific AB: {bundlePath}");
            }
            if (bundle != null)
            {
                keyBackgroundSprite = bundle.LoadAsset<Sprite>("KeyBackground");
                keyOutlineSprite = bundle.LoadAsset<Sprite>("KeyOutline");

                Font mapleOTF = bundle.LoadAsset<Font>("MAPLESTORY_OTF_BOLD");
                if (mapleOTF != null)
                {
                    mapleFont = TMP_FontAsset.CreateFontAsset(mapleOTF);
                    fontList.Add(new FontEntry("MapleStory", mapleFont));
                    //Main.Mod.Logger.Log($"KeyViewer: MapleStory font created, valid={mapleFont != null}");
                }
                else
                {
                    Main.Mod.Logger.Error("KeyViewer: MAPLESTORY_OTF_BOLD not found in AB");
                }

                Font cjkOTF = bundle.LoadAsset<Font>("cjkFonts-regular-normalized");
                if (cjkOTF != null)
                {
                    var cjkFont = TMP_FontAsset.CreateFontAsset(cjkOTF);
                    fontList.Insert(0, new FontEntry("CJK (\u9884\u8BBE)", cjkFont));
                    //Main.Mod.Logger.Log($"KeyViewer: CJK font created, valid={cjkFont != null}");
                }
                else
                {
                    Main.Mod.Logger.Error("KeyViewer: cjkFonts-regular-normalized not found in AB");
                }
                if (keyBackgroundSprite == null)
                    Main.Mod.Logger.Error("KeyViewer: AssetBundle \u4E2D\u672A\u627E\u5230 KeyBackground");
                if (keyOutlineSprite == null)
                    Main.Mod.Logger.Error("KeyViewer: AssetBundle \u4E2D\u672A\u627E\u5230 KeyOutline");

                bundle.Unload(false);
            }
            else
            {
                Main.Mod.Logger.Error($"KeyViewer: \u65E0\u6CD5\u52A0\u8F7D AssetBundle\uFF0C\u8DEF\u5F84: {bundlePath}");
            }

            // Load custom fonts from CustomFont directory and ensure fallback
            ScanGameFonts();
            ScanCustomFonts();
            LinkFallbackFonts();

            if (Settings.FontIndex >= fontList.Count)
                Settings.FontIndex = 0;

            return bundle != null;
        }

        private TMP_FontAsset GetCurrentFont()
        {
            return fontList.Count > 0 ? fontList[Mathf.Clamp(Settings.FontIndex, 0, fontList.Count - 1)].font : null;
        }

        private void UpdateAllFonts()
        {
            TMP_FontAsset currentFont = GetCurrentFont();
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

		static void LinkFallbackFonts()
		{
			FontEntry cjkEntry = null;
			foreach (var e in fontList)
				if (e.name == "CJK (\u9884\u8BBE)") { cjkEntry = e; break; }
			if (cjkEntry?.font == null) return;

			foreach (var entry in fontList)
			{
				if (entry.font == null || entry == cjkEntry) continue;
				if (entry.font.fallbackFontAssetTable == null)
					entry.font.fallbackFontAssetTable = new List<TMP_FontAsset>();
				if (!entry.font.fallbackFontAssetTable.Contains(cjkEntry.font))
					entry.font.fallbackFontAssetTable.Add(cjkEntry.font);
			}
			//Main.Mod.Logger.Log($"KeyViewer: CJK font linked as fallback for bundled fonts");
		}

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

            string[] fontFiles = Directory.GetFiles(customFontDir, "*.ttf", SearchOption.TopDirectoryOnly)
				.Concat(Directory.GetFiles(customFontDir, "*.otf", SearchOption.TopDirectoryOnly))
				.ToArray();

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

					// Avoid duplicates by checking existing font name
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
						//Main.Mod.Logger.Log($"KeyViewer: Loaded custom font '{fileName}'");
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
