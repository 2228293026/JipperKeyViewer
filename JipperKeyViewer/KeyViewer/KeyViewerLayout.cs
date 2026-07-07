using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>
    /// Layout and positioning: creating, initializing, positioning key elements / 布局和定位：创建、初始化、定位按键元素
    /// </summary>
    public partial class KeyViewer : MonoBehaviour
    {
        /// <summary>
        /// Create the canvas overlay and initialize all keys / 创建画布覆盖层并初始化所有按键
        /// Called when the mod is toggled on or when settings require rebuilding / 在 Mod 打开或设置需要重建时调用
        /// </summary>
        private void EnableKeyViewer()
        {
            if (KeyViewerObject != null || !Settings.Data.Enabled) return;
            if (!TryLoadResources())
            {
                Main.Mod.Logger.Error("KeyViewer: Cannot load AssetBundle, please check assets/ directory");
                return;
            }
            // Create ScreenSpaceOverlay canvas (independent of game UI) / 创建 ScreenSpaceOverlay 画布（独立于游戏 UI）
            KeyViewerObject = new GameObject("Jipper KeyViewer");
            Canvas = KeyViewerObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = Canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f; // Match height so vertical positions are resolution-independent / 匹配高度使垂直位置不受分辨率影响
            // SizeObject applies the Size scale and serves as parent for all keys / SizeObject 应用大小缩放并作为所有按键的父级
            KeyViewerSizeObject = new GameObject("SizeObject");
            RectTransform rectTransform = KeyViewerSizeObject.AddComponent<RectTransform>();
            rectTransform.SetParent(KeyViewerObject.transform);
            rectTransform.localScale = new Vector3(Settings.Data.Size, Settings.Data.Size, 1);
            // Fill full canvas with bottom-left pivot so localScale doesn't shift child positions / 填满画布，左下角轴心，使缩放不改变子元素位置
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = Vector2.zero;
            rectTransform.offsetMin = rectTransform.offsetMax = Vector2.zero;
            // Initialize main keys based on selected layout / 根据选中的布局初始化主按键
            Keys = new Key[MaxKeySlots];
            InitializeMainKeys(GetLayout(Settings.Data.KeyViewerStyle));
            // Initialize foot keys based on selected layout / 根据选中的布局初始化脚键
            int footSize = FootKeySize(Settings.Data.FootKeyViewerStyle);
            if (footSize > 0) InitializeFootKeyViewer(footSize);
            // Apply streamer mode (hide KPS/Total)
            if (Settings.Data.StreamerMode)
            {
                if (Kps != null) Kps.gameObject.SetActive(false);
                if (Total != null) Total.gameObject.SetActive(false);
            }
            // Persist the overlay across scene loads / 使覆盖层在场景加载中持久化
            Object.DontDestroyOnLoad(KeyViewerObject);
            PressTimes = new Queue<long>(256);
            keyPressTimes = new Queue<long>[MaxKeySlots];
            for (int i = 0; i < MaxKeySlots; i++)
                keyPressTimes[i] = new Queue<long>(32);
            lastPerKeyKps = new int[MaxKeySlots];
            Stopwatch = System.Diagnostics.Stopwatch.StartNew();
            RefreshAllCountDisplay();
        }

        /// <summary>
        /// Destroy the canvas overlay and clean up all resources / 销毁画布覆盖层并清理所有资源
        /// </summary>
        private void DisableKeyViewer()
        {
            if (KeyViewerObject == null) return;
            Object.Destroy(KeyViewerObject);
            KeyViewerObject = null;
            KeyViewerSizeObject = null;
            rainSystem?.ClearPool();
            // Destroy shadow materials / 销毁阴影材质
            foreach (var mat in shadowMaterials.Values)
                Object.Destroy(mat);
            shadowMaterials.Clear();
            Canvas = null;
            Keys = null;
            PressTimes = null;
            keyPressTimes = null;
            lastPerKeyKps = null;
            Stopwatch = null;
        }

        struct ExtraSlot { public int index; public float x, y, w; public int rainRow; public bool slim; }

        struct LayoutDesc { public float frontY; public float bottomY; public ExtraSlot[] extras; }

        private static LayoutDesc GetLayout(KeyviewerStyle style)
        {
            // Standardized layout: all back-row keys 50px, KPS/Total fill ends
            // 标准模式：后排统一50px宽，KPS/Total填满两端
            if (Settings?.Data?.StandardKeyWidth == true)
            {
                switch (style)
                {
                    case KeyviewerStyle.Key10:
                        return new LayoutDesc
                        {
                            frontY = 279, bottomY = 200,
                            extras = new ExtraSlot[]
                            {
                                new() { index = 8, x = 162, y = 225, w = 50, rainRow = 1 },
                                new() { index = 9, x = 216, y = 225, w = 50, rainRow = 1 },
                                new() { index = -1, x = 0, y = 225, w = 158, rainRow = -1 },
                                new() { index = -2, x = 270, y = 225, w = 158, rainRow = -1 },
                            }
                        };
                    case KeyviewerStyle.Key12:
                        return new LayoutDesc
                        {
                            frontY = 279, bottomY = 200,
                            extras = new ExtraSlot[]
                            {
                                new() { index = 9, x = 108, y = 225, w = 50, rainRow = 1 },
                                new() { index = 8, x = 162, y = 225, w = 50, rainRow = 1 },
                                new() { index = 10, x = 216, y = 225, w = 50, rainRow = 1 },
                                new() { index = 11, x = 270, y = 225, w = 50, rainRow = 1 },
                                new() { index = -1, x = 0, y = 225, w = 104, rainRow = -1 },
                                new() { index = -2, x = 324, y = 225, w = 104, rainRow = -1 },
                            }
                        };
                    case KeyviewerStyle.Key20:
                        return new LayoutDesc
                        {
                            frontY = 333, bottomY = 200,
                            extras = new ExtraSlot[]
                            {
                                new() { index = 12, x = 0, y = 279, w = 50, rainRow = 1 },
                                new() { index = 13, x = 54, y = 279, w = 50, rainRow = 1 },
                                new() { index = 9, x = 108, y = 279, w = 50, rainRow = 1 },
                                new() { index = 8, x = 162, y = 279, w = 50, rainRow = 1 },
                                new() { index = 10, x = 216, y = 279, w = 50, rainRow = 1 },
                                new() { index = 11, x = 270, y = 279, w = 50, rainRow = 1 },
                                new() { index = 14, x = 324, y = 279, w = 50, rainRow = 1 },
                                new() { index = 15, x = 378, y = 279, w = 50, rainRow = 1 },
                                new() { index = 17, x = 108, y = 225, w = 50, rainRow = 3 },
                                new() { index = 16, x = 162, y = 225, w = 50, rainRow = 3 },
                                new() { index = 18, x = 216, y = 225, w = 50, rainRow = 3 },
                                new() { index = 19, x = 270, y = 225, w = 50, rainRow = 3 },
                                new() { index = -1, x = 0, y = 225, w = 104, rainRow = -1 },
                                new() { index = -2, x = 324, y = 225, w = 104, rainRow = -1 },
                            }
                        };
                }
            }

            return style switch
            {
                KeyviewerStyle.Key8 => new LayoutDesc
                {
                    frontY = 266, bottomY = 205,
                    extras = new ExtraSlot[]
                    {
                        new() { index = -1, x = 0, y = 220, w = 212, rainRow = -1, slim = true },
                        new() { index = -2, x = 216, y = 220, w = 212, rainRow = -1, slim = true },
                    }
                },
                KeyviewerStyle.Key10 => new LayoutDesc
                {
                    frontY = 279, bottomY = 200,
                    extras = new ExtraSlot[]
                    {
                        new() { index = 8, x = 81, y = 225, w = 129, rainRow = 1 },
                        new() { index = 9, x = 216, y = 225, w = 129, rainRow = 1 },
                        new() { index = -1, x = 0, y = 225, w = 77, rainRow = -1 },
                        new() { index = -2, x = 351, y = 225, w = 77, rainRow = -1 },
                    }
                },
                KeyviewerStyle.Key12 => new LayoutDesc
                {
                    frontY = 279, bottomY = 200,
                    extras = new ExtraSlot[]
                    {
                        new() { index = 8, x = 135, y = 225, w = 77, rainRow = 1 },
                        new() { index = 9, x = 81, y = 225, w = 50, rainRow = 1 },
                        new() { index = 10, x = 216, y = 225, w = 77, rainRow = 1 },
                        new() { index = 11, x = 297, y = 225, w = 50, rainRow = 1 },
                        new() { index = -1, x = 0, y = 225, w = 77, rainRow = -1 },
                        new() { index = -2, x = 351, y = 225, w = 77, rainRow = -1 },
                    }
                },
                KeyviewerStyle.Key14 => new LayoutDesc
                {
                    frontY = 320, bottomY = 205,
                    extras = new ExtraSlot[]
                    {
                        new() { index = 13, x = 54, y = 266, w = 50, rainRow = 1 },
                        new() { index = 9, x = 108, y = 266, w = 50, rainRow = 1 },
                        new() { index = 8, x = 162, y = 266, w = 50, rainRow = 1 },
                        new() { index = 10, x = 216, y = 266, w = 50, rainRow = 1 },
                        new() { index = 11, x = 270, y = 266, w = 50, rainRow = 1 },
                        new() { index = 12, x = 324, y = 266, w = 50, rainRow = 1 },
                        new() { index = -1, x = 0, y = 220, w = 212, rainRow = -1, slim = true },
                        new() { index = -2, x = 216, y = 220, w = 212, rainRow = -1, slim = true },
                    }
                },
                KeyviewerStyle.Key16 => new LayoutDesc
                {
                    frontY = 320, bottomY = 205,
                    extras = new ExtraSlot[]
                    {
                        new() { index = 12, x = 0, y = 266, w = 50, rainRow = 1 },
                        new() { index = 13, x = 54, y = 266, w = 50, rainRow = 1 },
                        new() { index = 9, x = 108, y = 266, w = 50, rainRow = 1 },
                        new() { index = 8, x = 162, y = 266, w = 50, rainRow = 1 },
                        new() { index = 10, x = 216, y = 266, w = 50, rainRow = 1 },
                        new() { index = 11, x = 270, y = 266, w = 50, rainRow = 1 },
                        new() { index = 14, x = 324, y = 266, w = 50, rainRow = 1 },
                        new() { index = 15, x = 378, y = 266, w = 50, rainRow = 1 },
                        new() { index = -1, x = 0, y = 220, w = 212, rainRow = -1, slim = true },
                        new() { index = -2, x = 216, y = 220, w = 212, rainRow = -1, slim = true },
                    }
                },
                KeyviewerStyle.Key20 => new LayoutDesc
                {
                    frontY = 333, bottomY = 200,
                    extras = new ExtraSlot[]
                    {
                        new() { index = 12, x = 0, y = 279, w = 50, rainRow = 1 },
                        new() { index = 13, x = 54, y = 279, w = 50, rainRow = 1 },
                        new() { index = 9, x = 108, y = 279, w = 50, rainRow = 1 },
                        new() { index = 8, x = 162, y = 279, w = 50, rainRow = 1 },
                        new() { index = 10, x = 216, y = 279, w = 50, rainRow = 1 },
                        new() { index = 11, x = 270, y = 279, w = 50, rainRow = 1 },
                        new() { index = 14, x = 324, y = 279, w = 50, rainRow = 1 },
                        new() { index = 15, x = 378, y = 279, w = 50, rainRow = 1 },
                        new() { index = 16, x = 135, y = 225, w = 77, rainRow = 3 },
                        new() { index = 17, x = 81, y = 225, w = 50, rainRow = 3 },
                        new() { index = 18, x = 216, y = 225, w = 77, rainRow = 3 },
                        new() { index = 19, x = 297, y = 225, w = 50, rainRow = 3 },
                        new() { index = -1, x = 0, y = 225, w = 77, rainRow = -1 },
                        new() { index = -2, x = 351, y = 225, w = 77, rainRow = -1 },
                    }
                },
                KeyviewerStyle.Key24 => new LayoutDesc
                {
                    frontY = 375, bottomY = 205,
                    extras = new ExtraSlot[]
                    {
                        new() { index = 12, x = 0, y = 321, w = 50, rainRow = 1 },
                        new() { index = 13, x = 54, y = 321, w = 50, rainRow = 1 },
                        new() { index = 9, x = 108, y = 321, w = 50, rainRow = 1 },
                        new() { index = 8, x = 162, y = 321, w = 50, rainRow = 1 },
                        new() { index = 10, x = 216, y = 321, w = 50, rainRow = 1 },
                        new() { index = 11, x = 270, y = 321, w = 50, rainRow = 1 },
                        new() { index = 14, x = 324, y = 321, w = 50, rainRow = 1 },
                        new() { index = 15, x = 378, y = 321, w = 50, rainRow = 1 },
                        new() { index = 17, x = 0, y = 267, w = 50, rainRow = 3 },
                        new() { index = 16, x = 54, y = 267, w = 50, rainRow = 3 },
                        new() { index = 18, x = 108, y = 267, w = 50, rainRow = 3 },
                        new() { index = 19, x = 162, y = 267, w = 50, rainRow = 3 },
                        new() { index = 21, x = 216, y = 267, w = 50, rainRow = 3 },
                        new() { index = 20, x = 270, y = 267, w = 50, rainRow = 3 },
                        new() { index = 22, x = 324, y = 267, w = 50, rainRow = 3 },
                        new() { index = 23, x = 378, y = 267, w = 50, rainRow = 3 },
                        new() { index = -1, x = 0, y = 221, w = 212, rainRow = -1, slim = true },
                        new() { index = -2, x = 216, y = 221, w = 212, rainRow = -1, slim = true },
                    }
                },
                _ => throw new System.ArgumentOutOfRangeException(nameof(style), style, null)
            };
        }

        private void InitializeMainKeys(LayoutDesc layout)
        {
            int remove = Settings.Data.DownLocation ? 200 : 0;
            for (int i = 0; i < 8; i++)
                Keys[i] = CreateKey(i, 54 * i, layout.frontY - remove, 50, 0);
            foreach (var e in layout.extras)
            {
                Key key = CreateKey(e.index, e.x, e.y - remove, e.w, e.rainRow, e.slim);
                if (e.index == -1) Kps = key;
                else if (e.index == -2) Total = key;
                else Keys[e.index] = key;
            }
            // Keep each key's own RainLine — adjust X offset and width for front-column alignment
            // 每键独立雨滴容器，不共享，只调 X 偏移和宽度对齐前列
            ApplyRainContainerSharing();
        }

        /// <summary>
        /// Redirect back-row rain containers to front-row ones for layouts with non-standard key widths.
        /// Matches JipperResourcePack's RainPool sharing — rain drops inherit front row's X position and width,
        /// while per-row settings (color, speed, height) remain from the pressed key's row.
        /// 将非标准宽度的后排雨滴容器重指向前排按键，雨滴位置与前列对齐
        /// </summary>
        private void ApplyRainContainerSharing()
        {
            switch (Settings.Data.KeyViewerStyle)
            {
                case KeyviewerStyle.Key10:
                    // Back row [8,9] → front [3,4]
                    ShareRainContainer(8, 3);
                    ShareRainContainer(9, 4);
                    break;
                case KeyviewerStyle.Key12:
                    // Back row [9,8,10,11] → front [2,3,4,5]
                    ShareRainContainer(9, 2);
                    ShareRainContainer(8, 3);
                    ShareRainContainer(10, 4);
                    ShareRainContainer(11, 5);
                    break;
                case KeyviewerStyle.Key20:
                    // Third row [17,16,18,19] → front [2,3,4,5] (same positions as 12K back)
                    ShareRainContainer(17, 2);
                    ShareRainContainer(16, 3);
                    ShareRainContainer(18, 4);
                    ShareRainContainer(19, 5);
                    break;
                case KeyviewerStyle.Key24:
                    // Third row [16-23] all 50px aligned, no sharing needed; included for consistency
                    break;
            }
        }

        private void ShareRainContainer(int backIndex, int frontIndex)
        {
            if (backIndex < 0 || backIndex >= Keys.Length || frontIndex < 0 || frontIndex >= Keys.Length) return;
            var backKey = Keys[backIndex];
            var frontKey = Keys[frontIndex];
            if (backKey?.rain == null || frontKey?.rain == null) return;
            if (backKey.rain == frontKey.rain) return;

            // Keep back key's own RainLine — just align its X and width to the front column.
            // Each key keeps its own container, so Z-order is naturally correct (no Canvas hack needed).
            // 保留后排按键的雨滴容器，不共享，只调 X 偏移对齐前列，宽度改为标准 50px
            RectTransform rt = backKey.rain.GetComponent<RectTransform>();
            float frontX = frontKey.GetComponent<RectTransform>().anchoredPosition.x;
            float backX = backKey.GetComponent<RectTransform>().anchoredPosition.x;
            backKey.rainOffsetX = frontX - backX;
            Vector2 pos = rt.anchoredPosition;
            rt.anchoredPosition = new Vector2(backKey.rainOffsetX, pos.y);
            rt.sizeDelta = new Vector2(50, rt.sizeDelta.y);
        }

        private void RepositionMainKeys(LayoutDesc layout, float baseX, float baseY)
        {
            int remove = Settings.Data.DownLocation ? 200 : 0;
            for (int i = 0; i < 8; i++)
                SetKeyPosition(i, baseX + 54 * i, baseY + layout.frontY - remove);
            foreach (var e in layout.extras)
                SetKeyPosition(e.index, baseX + e.x, baseY + e.y - remove);
        }

        /// <summary>
        /// Initialize foot keys starting at Keys[20] / 初始化从 Keys[20] 开始的脚键
        /// Supports 2-16 keys, automatically arranging in 1 or 2 rows / 支持 2-16 个键，自动排列为 1 或 2 排
        /// </summary>
        private void InitializeFootKeyViewer(int size)
        {
            for (int i = FootKeyBase; i < FootKeyBase + size; i++)
            {
                int col;
                int row;
                int footOfs = i - FootKeyBase;
                if (size <= 8)
                {
                    col = footOfs;
                    row = 0;
                }
                else
                {
                    if (footOfs < 8)
                    {
                        col = footOfs;
                        row = 0;
                    }
                    else
                    {
                        col = footOfs - 8;
                        row = 1;
                    }
                }
                int baseY = size > 8 ? 15 + 34 : 15;
                int x = 432 + col * 34;
                // Center the second row under the first when not full / 第二排不满时居中于第一排下方
                if (size > 8 && row == 1)
                    x += (8 - (size - 8)) * 17;
                int y = baseY - row * 34;
                Keys[i] = CreateKey(i, x, y, 30, -1, true, false);
            }
        }

        /// <summary>
        /// Create a single key GameObject with background, outline, text, count, and optional rain container / 创建单个按键 GameObject，包含背景、轮廓、文本、计数和可选的雨滴容器
        /// </summary>
        /// <param name="i">Key index (-1=KPS, -2=Total, 0-35=keys) / 按键索引（-1=KPS，-2=Total，0-35=按键）</param>
        /// <param name="x">X position in canvas reference coordinates / 画布参考坐标系中的 X 位置</param>
        /// <param name="y">Y position in canvas reference coordinates / 画布参考坐标系中的 Y 位置</param>
        /// <param name="sizeX">Key width / 按键宽度</param>
        /// <param name="raining">Rain row index (-1=no rain, 0=row1, 1=row2, 3=row3) / 雨滴行索引（-1=无雨滴，0=第1排，1=第2排，3=第3排）</param>
        /// <param name="slim">Use slim style (for KPS/Total display) / 使用窄样式（用于 KPS/Total 显示）</param>
        /// <param name="count">Show press count text / 显示按下计数文本</param>
        private Key CreateKey(int i, float x, float y, float sizeX, int raining, bool slim = false, bool count = true)
        {
            if (i >= 0 && i < FootKeyBase && Settings.Data.HideMainKeyCount)
                count = false;
            GameObject obj = new("Key " + i);
            KeyViewerSettings settings = Settings;
            RectTransform transform = obj.AddComponent<RectTransform>();
            transform.SetParent(KeyViewerSizeObject.transform);
            transform.sizeDelta = new Vector2(sizeX, slim ? 30 : 50);
            transform.anchorMin = transform.anchorMax = Vector2.zero;
            transform.pivot = new Vector2(0, 0.5f);
            transform.anchoredPosition = new Vector2(x, y);
            transform.localScale = Vector3.one;
            Key key = obj.AddComponent<Key>();
            key.isPressed = false;
            // Visuals wrapper: center-pivot so scale animation is naturally centered / 视觉包裹层：轴心居中，缩放自动从中心向四周
            // Rain container stays outside — unaffected by press animation / 雨滴容器在包裹层外，不受缩放影响
            GameObject visuals = new("Visuals");
            RectTransform vrt = visuals.AddComponent<RectTransform>();
            vrt.SetParent(obj.transform);
            vrt.sizeDelta = new Vector2(sizeX, slim ? 30 : 50);
            vrt.anchorMin = vrt.anchorMax = new Vector2(0, 0.5f);
            vrt.pivot = new Vector2(0.5f, 0.5f);
            vrt.anchoredPosition = new Vector2(sizeX * 0.5f, 0);
            vrt.localScale = Vector3.one;
            key.visuals = visuals.transform;
            key.background = CreateImage(visuals, "Background", sizeX, slim, keyBackgroundSprite, settings.Data.Background);
            key.outline = CreateImage(visuals, "Outline", sizeX, slim, keyOutlineSprite, settings.Data.Outline);
            key.text = CreateKeyText(visuals, sizeX, slim, count, settings);
            if (count)
                key.value = CreateCountText(visuals, sizeX, slim, settings);
            UpdateKeyText(key, i);
            SetupRainContainer(key, obj, sizeX, raining);
            ApplyKeyColors(key, i, raining);
            return key;
        }

        private static Image CreateImage(GameObject parent, string name, float sizeX, bool slim, Sprite sprite, Color color)
        {
            GameObject go = new(name);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent.transform);
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(sizeX * 2, (slim ? 30 : 50) * 2);
            rt.localScale = new Vector3(0.5f, 0.5f);
            Image image = go.AddComponent<Image>();
            image.color = color;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }
            image.raycastTarget = false;
            return image;
        }

        private TextMeshProUGUI CreateKeyText(GameObject parent, float sizeX, bool slim, bool count, KeyViewerSettings settings)
        {
            GameObject go = new("KeyText");
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent.transform);
            if (slim)
            {
                rt.sizeDelta = new Vector2(sizeX / 2, 30);
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 0.5f);
                rt.anchoredPosition = new Vector2(count ? 10 : 7.5f, 0);
            }
            else
            {
                rt.sizeDelta = new Vector2(sizeX - 4, 32);
                if (!count)
                {
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                }
                else
                {
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1);
                    rt.anchoredPosition = new Vector2(0, 2);
                }
            }
            rt.localScale = Vector3.one;
            return ConfigureText(go, settings, slim ? TextAlignmentOptions.Left : TextAlignmentOptions.Center);
        }

        private TextMeshProUGUI CreateCountText(GameObject parent, float sizeX, bool slim, KeyViewerSettings settings)
        {
            GameObject go = new("CountText");
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent.transform);
            if (slim)
            {
                rt.sizeDelta = new Vector2(sizeX / 2, 30);
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0.5f);
                rt.anchoredPosition = new Vector2(-10, 0);
            }
            else
            {
                rt.sizeDelta = new Vector2(sizeX - 4, 16);
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0);
                rt.anchoredPosition = new Vector2(0, 2);
            }
            rt.localScale = Vector3.one;
            return ConfigureText(go, settings, slim ? TextAlignmentOptions.Right : TextAlignmentOptions.Top);
        }

        private TextMeshProUGUI ConfigureText(GameObject go, KeyViewerSettings settings, TextAlignmentOptions alignment)
        {
            var text = go.AddComponent<TextMeshProUGUI>();
            var keyFont = GetCurrentFont();
            if (keyFont != null)
            {
                text.font = keyFont;
                var mat = GetShadowMaterial(keyFont);
                if (mat != null) text.fontMaterial = mat;
            }
            text.fontStyle = (FontStyles)settings.Data.FontStyleFlags;
            text.enableAutoSizing = true;
            text.fontSizeMin = 0;
            text.fontSizeMax = settings.Data.KeyFontSize;
            text.alignment = alignment;
            text.color = settings.Data.Text;
            text.raycastTarget = false;
            return text;
        }

        private void UpdateRainContainerPositions()
        {
            if (Keys == null) return;
            var processed = new HashSet<RectTransform>();
            foreach (var key in Keys)
            {
                if (key?.rain == null) continue;
                RectTransform rt = key.rain.GetComponent<RectTransform>();
                if (rt == null || !processed.Add(rt)) continue;
                rt.anchoredPosition = new Vector2(key.rainOffsetX, key.color switch
                {
                    0 => Settings.Data.RainStartYRow1,
                    3 => Settings.Data.RainStartYRow3,
                    _ => Settings.Data.RainStartYRow2
                });
            }
        }

        private void UpdateGhostRainStartY()
        {
            if (Keys == null || rainSystem == null) return;
            for (int i = 0; i < Keys.Length; i++)
            {
                var key = Keys[i];
                if (key?.rain == null) continue;
                int row = i < 8 ? 1 : i < 16 ? 2 : 3;
                float baseY = row == 1 ? Settings.Data.RainStartYRow1
                    : row == 2 ? Settings.Data.RainStartYRow2
                    : Settings.Data.RainStartYRow3;
                float ghostY = row == 1 ? Settings.Data.GhostRainStartYRow1
                    : row == 2 ? Settings.Data.GhostRainStartYRow2
                    : Settings.Data.GhostRainStartYRow3;
                float startY = ghostY - baseY;
                foreach (var rawRain in key.rainList)
                {
                    if (rawRain.isGhost)
                        rawRain.startY = startY;
                }
            }
        }

        private static void SetupRainContainer(Key key, GameObject parent, float sizeX, int raining)
        {
            if (raining >= 0)
            {
                if (key.rain == null)
                {
                    key.rain = new GameObject("RainLine");
                    RectTransform rt = key.rain.AddComponent<RectTransform>();
                    rt.SetParent(parent.transform);
                    rt.sizeDelta = new Vector2(sizeX, 275);
                    rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
                    rt.anchoredPosition = new Vector2(0, raining switch
                    {
                        0 => KeyViewer.Settings.Data.RainStartYRow1,
                        3 => KeyViewer.Settings.Data.RainStartYRow3,
                        _ => KeyViewer.Settings.Data.RainStartYRow2
                    });
                    rt.localScale = Vector3.one;
                    key.rain.AddComponent<Canvas>();
                }
                key.color = (byte)raining;
            }
            else
            {
                key.color = 1;
                key.rain?.SetActive(false);
                key.rain = null;
            }
        }

        private int KeyIndex(int i) => i >= 0 && i < Keys.Length ? i : i == -1 ? Keys.Length : i == -2 ? Keys.Length + 1 : -1;

        private void ApplyKeyColors(Key key, int i, int raining)
        {
            int pi = KeyIndex(i);
            if (Settings.Data.EnablePerKeyColors)
            {
                if (pi < 0) return;
                key.background.color = Settings.Data.PerKeyBackground[pi];
                key.outline.color = Settings.Data.PerKeyOutline[pi];
                key.text.color = Settings.Data.PerKeyText[pi];
                if (key.value != null) key.value.color = Settings.Data.PerKeyText[pi];
                key.rainColor = Settings.Data.PerKeyRainColor[pi];
                return;
            }
            if (pi >= Keys.Length)
            {
                bool isKps = pi == Keys.Length;
                key.background.color = isKps ? Settings.Data.KpsBackground : Settings.Data.TotalBackground;
                key.outline.color = isKps ? Settings.Data.KpsOutline : Settings.Data.TotalOutline;
                key.text.color = isKps ? Settings.Data.KpsText : Settings.Data.TotalText;
                if (key.value != null) key.value.color = key.text.color;
            }
            if (raining >= 0)
                key.rainColor = rainSystem.GetRainColor((byte)raining);
        }

        /// <summary>
        /// Set the display text for a key based on its index and current bindings / 根据按键索引和当前绑定设置显示文本
        /// Special indices: -1=KPS, -2=Total / 特殊索引：-1=KPS，-2=Total
        /// </summary>
        private static void UpdateKeyText(Key key, int i)
        {
            if (key == null) return;
            if (i == -1)
            {
                key.text.text = "KPS";
                if (key.value != null) key.value.text = "0";
                return;
            }
            if (i == -2)
            {
                key.text.text = "Total";
                if (key.value != null) key.value.text = FormatCount(Settings.Data.TotalCount);
                return;
            }
            if (i < FootKeyBase)
            {
                KeyCode[] keyCodes = GetKeyCode();
                string[] keyTexts = GetKeyText();
                if (keyCodes != null && keyTexts != null && i < keyCodes.Length && i < keyTexts.Length)
                {
                    string displayText = !string.IsNullOrEmpty(keyTexts[i]) ? keyTexts[i] : KeyToString(keyCodes[i]);
                    key.text.text = displayText;
                    if (key.value != null)
                        key.value.text = FormatCount(Settings.Data.Count[i]);
                }
            }
            else
            {
                KeyCode[] footKeyCodes = GetFootKeyCode();
                string[] footTexts = GetFootKeyText();
                int footIndex = i - FootKeyBase;
                if (footKeyCodes != null && footIndex >= 0 && footIndex < footKeyCodes.Length)
                {
                    string displayText = footTexts != null && footIndex < footTexts.Length && !string.IsNullOrEmpty(footTexts[footIndex])
                        ? footTexts[footIndex] : KeyToString(footKeyCodes[footIndex]);
                    key.text.text = displayText;
                }
            }
        }

        /// <summary>
        /// Lowest key bottom edge Y for normalized positioning / 归一化定位中最低按键底边的 Y 值
        /// </summary>
        private float GetMinMainKeyOffset()
        {
            float bottomY = GetLayout(Settings.Data.KeyViewerStyle).bottomY;
            return Settings.Data.DownLocation ? bottomY - 200 : bottomY;
        }

        /// <summary>Total width of the main key layout in reference pixels / 主按键布局的总宽度（参考像素）</summary>
        private float GetMainLayoutRightmostOffset() => 428f;

        /// <summary>Topmost key top edge Y for normalized positioning / 归一化定位中最顶部按键顶边的 Y 值</summary>
        private float GetMaxMainKeyOffset()
        {
            return GetLayout(Settings.Data.KeyViewerStyle).frontY + 25;
        }

        /// <summary>Width of the foot key section in reference pixels / 脚键区域的宽度（参考像素）</summary>
        private float GetFootLayoutRightmostOffset(int size)
        {
            int row0Cols = Mathf.Min(size, 8);
            return (row0Cols - 1) * 34 + 30;
        }

        /// <summary>
        /// Reposition main keys based on normalized (0-1) custom position / 基于归一化（0-1）自定义位置重新定位主按键
        /// Maps (0,0) to screen top-left and (1,1) to screen bottom-right / (0,0) 映射到屏幕左上角，(1,1) 映射到屏幕右下角
        /// </summary>
        private void ResetKeyViewerPosition()
        {
            if (Keys == null || !Settings.Data.CustomPositionEnabled) return;
            Vector2 norm = Settings.Data.MainKeyViewerPosition;
            // Convert normalized (X: 0=left 1=right, Y: 0=top 1=bottom) to reference pixel offsets from bottom-left.
            // X: interpolate so X=0 = left edge at screen left, X=1 = right edge at screen right.
            // Y: subtract min layout offset so Y=1 puts the lowest key's bottom edge at screen bottom.
            float r = GetMainLayoutRightmostOffset();
            float baseX = norm.x * (CanvasWidth - r);
            int remove = Settings.Data.DownLocation ? 200 : 0;
            // Y: lerp so Y=0 = top edge at screen top, Y=1 = bottom edge at screen bottom
            float topBaseY = 1080f - GetMaxMainKeyOffset() + remove;
            float bottomBaseY = -GetMinMainKeyOffset();
            float baseY = Mathf.Lerp(bottomBaseY, topBaseY, 1f - norm.y);
            RepositionMainKeys(GetLayout(Settings.Data.KeyViewerStyle), baseX, baseY);
        }

        private static int FootKeySize(FootKeyviewerStyle style) => style switch
        {
            FootKeyviewerStyle.Key2 => 2,
            FootKeyviewerStyle.Key4 => 4,
            FootKeyviewerStyle.Key6 => 6,
            FootKeyviewerStyle.Key8 => 8,
            FootKeyviewerStyle.Key10 => 10,
            FootKeyviewerStyle.Key12 => 12,
            FootKeyviewerStyle.Key14 => 14,
            FootKeyviewerStyle.Key16 => 16,
            _ => 0
        };

        private void ResetFootKeyViewerPosition()
        {
            if (Keys == null || !Settings.Data.CustomPositionEnabled) return;
            Vector2 norm = Settings.Data.FootKeyViewerPosition;
            int size = FootKeySize(Settings.Data.FootKeyViewerStyle);
            if (size == 0) return;
            float r = GetFootLayoutRightmostOffset(size);
            float baseX = norm.x * (CanvasWidth - r);
            // Y: lerp so Y=0 = top edge at screen top, Y=1 = bottom edge at screen bottom
            // Single row (size≤8): top edge at baseY+15. Two rows: top+34, top edge at baseY+49.
            float footTopOffset = size <= 8 ? 15f : 49f;
            float topBaseY = 1080f - footTopOffset;
            float bottomBaseY = 15f; // lowest key center at baseY, bottom edge = baseY-15 → baseY=15
            float baseY = Mathf.Lerp(bottomBaseY, topBaseY, 1f - norm.y);
            int firstRowCount = size <= 8 ? size : 8;
            float yBase = size > 8 ? baseY + 34 : baseY;
            for (int i = FootKeyBase; i < FootKeyBase + size; i++)
            {
                int offset = i - FootKeyBase;
                if (offset < firstRowCount)
                {
                    SetKeyPosition(i, baseX + offset * 34, yBase);
                }
                else
                {
                    int col = offset - firstRowCount;
                    float x = baseX + col * 34 + (firstRowCount - (size - firstRowCount)) * 17;
                    SetKeyPosition(i, x, yBase - 34);
                }
            }
        }

        private int lastScreenWidth, lastScreenHeight;
        private float canvasWidth;

        private float CanvasWidth
        {
            get
            {
                if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height)
                {
                    canvasWidth = Screen.width * 1080f / Screen.height;
                    lastScreenWidth = Screen.width;
                    lastScreenHeight = Screen.height;
                }
                return canvasWidth;
            }
        }

        private void SetKeyPosition(int keyIndex, float x, float y)
        {
            if (keyIndex == -1 && Kps != null)
            {
                ((RectTransform)Kps.transform).anchoredPosition = new Vector2(x, y);
            }
            else if (keyIndex == -2 && Total != null)
            {
                ((RectTransform)Total.transform).anchoredPosition = new Vector2(x, y);
            }
            else if (keyIndex >= 0 && keyIndex < Keys.Length && Keys[keyIndex] != null)
            {
                ((RectTransform)Keys[keyIndex].transform).anchoredPosition = new Vector2(x, y);
            }
        }

        /// <summary>Get color setting by numeric index (0-8) for the color picker / 通过数字索引（0-8）获取颜色设置，用于颜色选择器</summary>
        private Color GetColorByIndex(int index)
        {
            return index switch
            {
                0 => Settings.Data.Background,
                1 => Settings.Data.BackgroundClicked,
                2 => Settings.Data.Outline,
                3 => Settings.Data.OutlineClicked,
                4 => Settings.Data.Text,
                5 => Settings.Data.TextClicked,
                6 => Settings.Data.RainColor,
                7 => Settings.Data.RainColor2,
                8 => Settings.Data.RainColor3,
                9 => Settings.Data.GhostRainColor,
                10 => Settings.Data.GhostRainColor2,
                11 => Settings.Data.GhostRainColor3,
                _ => Color.white
            };
        }

        /// <summary>Set color setting by numeric index (0-8) from the color picker / 通过数字索引（0-8）从颜色选择器设置颜色</summary>
        private void SetColorByIndex(int index, Color color)
        {
            switch (index)
            {
                case 0: Settings.Data.Background = color; break;
                case 1: Settings.Data.BackgroundClicked = color; break;
                case 2: Settings.Data.Outline = color; break;
                case 3: Settings.Data.OutlineClicked = color; break;
                case 4: Settings.Data.Text = color; break;
                case 5: Settings.Data.TextClicked = color; break;
                case 6: Settings.Data.RainColor = color; break;
                case 7: Settings.Data.RainColor2 = color; break;
                case 8: Settings.Data.RainColor3 = color; break;
                case 9: Settings.Data.GhostRainColor = color; break;
                case 10: Settings.Data.GhostRainColor2 = color; break;
                case 11: Settings.Data.GhostRainColor3 = color; break;
            }
        }

        private void UpdateAllKeyColors()
        {
            if (Keys == null) return;
            if (Settings.Data.EnablePerKeyColors)
                ApplyPerKeyColorsToAll();
            else
                ApplyGlobalColorsToAll();
            ApplyKpsTotalColors();
        }

        private void ApplyKpsTotalColors()
        {
            ApplyColorToKey(Kps, Keys.Length);
            ApplyColorToKey(Total, Keys.Length + 1);
        }

        private void ApplyColorToKey(Key k, int pi)
        {
            if (k == null) return;
            if (Settings.Data.EnablePerKeyColors)
            {
                k.background.color = Settings.Data.PerKeyBackground[pi];
                k.outline.color = Settings.Data.PerKeyOutline[pi];
                k.text.color = Settings.Data.PerKeyText[pi];
                if (k.value != null) k.value.color = Settings.Data.PerKeyText[pi];
            }
            else if (pi == Keys.Length)
            {
                k.background.color = Settings.Data.KpsBackground;
                k.outline.color = Settings.Data.KpsOutline;
                k.text.color = Settings.Data.KpsText;
                if (k.value != null) k.value.color = Settings.Data.KpsText;
            }
            else if (pi == Keys.Length + 1)
            {
                k.background.color = Settings.Data.TotalBackground;
                k.outline.color = Settings.Data.TotalOutline;
                k.text.color = Settings.Data.TotalText;
                if (k.value != null) k.value.color = Settings.Data.TotalText;
            }
        }

        private void ApplyPerKeyColorsToAll()
        {
            for (int i = 0; i < Keys.Length; i++)
            {
                if (Keys[i] == null) continue;
                Keys[i].background.color = Settings.Data.PerKeyBackground[i];
                Keys[i].outline.color = Settings.Data.PerKeyOutline[i];
                Keys[i].text.color = Settings.Data.PerKeyText[i];
                if (Keys[i].value != null) Keys[i].value.color = Settings.Data.PerKeyText[i];
                Keys[i].rainColor = Settings.Data.PerKeyRainColor[i];
            }
        }

        private void ApplyGlobalColorsToAll()
        {
            KeyCode[] keyCodes = GetKeyCode();
            for (int i = 0; i < keyCodes.Length && i < Keys.Length; i++)
            {
                if (Keys[i] == null) continue;
                Keys[i].background.color = Settings.Data.Background;
                Keys[i].outline.color = Settings.Data.Outline;
                Keys[i].text.color = Settings.Data.Text;
                if (Keys[i].value != null) Keys[i].value.color = Settings.Data.Text;
                Keys[i].rainColor = rainSystem?.GetRainColor(Keys[i].color) ?? Settings.Data.RainColor;
            }
            KeyCode[] footKeyCodes = GetFootKeyCode();
            if (footKeyCodes == null) return;
            for (int i = 0; i < footKeyCodes.Length; i++)
            {
                int index = i + FootKeyBase;
                if (index >= Keys.Length || Keys[index] == null) continue;
                Keys[index].background.color = Settings.Data.Background;
                Keys[index].outline.color = Settings.Data.Outline;
                Keys[index].text.color = Settings.Data.Text;
                if (Keys[index].value != null) Keys[index].value.color = Settings.Data.Text;
            }
        }

        /// <summary>
        /// Handle main key layout change / 处理主按键布局变化
        /// </summary>
        private void ChangeKeyViewer()
        {
            ResetKeyViewer();
            ResetFootKeyViewer();
        }

        /// <summary>
        /// Destroy and recreate main keys (for layout/style changes) / 销毁并重建主按键（用于布局/样式变化）
        /// </summary>
        private void ResetKeyViewer()
        {
            SelectedKey = -1;
            if (Keys != null)
            {
                rainSystem.ClearActiveDrops(Keys);
                for (int i = 0; i < FootKeyBase; i++)
                {
                    if (Keys[i] != null && Keys[i].gameObject != null)
                        Object.Destroy(Keys[i].gameObject);
                }
                if (Total != null && Total.gameObject != null)
                    Object.Destroy(Total.gameObject);
                if (Kps != null && Kps.gameObject != null)
                    Object.Destroy(Kps.gameObject);
            }
            rainSystem.ClearPool();
            InitializeMainKeys(GetLayout(Settings.Data.KeyViewerStyle));
            if (Settings.Data.StreamerMode)
            {
                if (Kps != null) Kps.gameObject.SetActive(false);
                if (Total != null) Total.gameObject.SetActive(false);
            }
            if (Settings.Data.CustomPositionEnabled)
                ResetKeyViewerPosition();
            RefreshAllCountDisplay();
        }

        /// <summary>
        /// Destroy and recreate foot keys (for layout/style changes) / 销毁并重建脚键（用于布局/样式变化）
        /// </summary>
        private void ResetFootKeyViewer()
        {
            SelectedKey = -1;
            if (Keys != null)
            {
                for (int i = FootKeyBase; i < Keys.Length; i++)
                {
                    var key = Keys[i];
                    if (key == null) continue;
                    foreach (var rain in key.rainList)
                    {
                        if (rain.rainComponent != null)
                        {
                            rainSystem.ReturnRain(rain.rainComponent);
                            rain.rainComponent = null;
                        }
                        rainSystem.ReturnRawRain(rain);
                    }
                    key.rainList.Clear();
                }
                for (int i = FootKeyBase; i < Keys.Length; i++)
                {
                    if (Keys[i] != null && Keys[i].gameObject != null)
                        Object.Destroy(Keys[i].gameObject);
                }
            }
            rainSystem.ClearPool();
            int footSize = FootKeySize(Settings.Data.FootKeyViewerStyle);
            if (footSize > 0) InitializeFootKeyViewer(footSize);
            if (Settings.Data.CustomPositionEnabled)
                ResetFootKeyViewerPosition();
            RefreshAllCountDisplay();
        }

        /// <summary>
        /// Get the key code array for the current main layout / 获取当前主布局的按键代码数组
        /// </summary>
        private static KeyCode[] GetKeyCode()
        {
            return Settings.Data.KeyViewerStyle switch
            {
                KeyviewerStyle.Key8 => Settings.Data.key8,
                KeyviewerStyle.Key12 => Settings.Data.key12,
                KeyviewerStyle.Key14 => Settings.Data.key14,
                KeyviewerStyle.Key16 => Settings.Data.key16,
                KeyviewerStyle.Key20 => Settings.Data.key20,
                KeyviewerStyle.Key24 => Settings.Data.key24,
                KeyviewerStyle.Key10 => Settings.Data.key10,
                _ => Settings.Data.key16
            };
        }

        /// <summary>
        /// Get the foot key code array for the current foot layout / 获取当前脚键布局的按键代码数组
        /// </summary>
        private static KeyCode[] GetFootKeyCode()
        {
            return Settings.Data.FootKeyViewerStyle switch
            {
                FootKeyviewerStyle.Key2 => Settings.Data.footkey2,
                FootKeyviewerStyle.Key4 => Settings.Data.footkey4,
                FootKeyviewerStyle.Key6 => Settings.Data.footkey6,
                FootKeyviewerStyle.Key8 => Settings.Data.footkey8,
                FootKeyviewerStyle.Key10 => Settings.Data.footkey10,
                FootKeyviewerStyle.Key12 => Settings.Data.footkey12,
                FootKeyviewerStyle.Key14 => Settings.Data.footkey14,
                FootKeyviewerStyle.Key16 => Settings.Data.footkey16,
                _ => new KeyCode[0]
            };
        }

        /// <summary>
        /// Get the ghost key code array for the current main layout / 获取当前主布局的鬼键代码数组
        /// </summary>
        private static KeyCode[] GetGhostKeyCode()
        {
            return Settings.Data.KeyViewerStyle switch
            {
                KeyviewerStyle.Key8 => Settings.Data.GhostKey8,
                KeyviewerStyle.Key10 => Settings.Data.GhostKey10,
                KeyviewerStyle.Key12 => Settings.Data.GhostKey12,
                KeyviewerStyle.Key14 => Settings.Data.GhostKey14,
                KeyviewerStyle.Key16 => Settings.Data.GhostKey16,
                KeyviewerStyle.Key20 => Settings.Data.GhostKey20,
                KeyviewerStyle.Key24 => Settings.Data.GhostKey24,
                _ => Settings.Data.GhostKey16
            };
        }

        /// <summary>
        /// Get the custom text labels for the current main layout / 获取当前主布局的自定义文本标签
        /// </summary>
        private static string[] GetKeyText()
        {
            return Settings.Data.KeyViewerStyle switch
            {
                KeyviewerStyle.Key8 => Settings.Data.key8Text,
                KeyviewerStyle.Key12 => Settings.Data.key12Text,
                KeyviewerStyle.Key14 => Settings.Data.key14Text,
                KeyviewerStyle.Key16 => Settings.Data.key16Text,
                KeyviewerStyle.Key20 => Settings.Data.key20Text,
                KeyviewerStyle.Key24 => Settings.Data.key24Text,
                KeyviewerStyle.Key10 => Settings.Data.key10Text,
                _ => Settings.Data.key16Text
            };
        }

        /// <summary>
        /// Get the custom text labels for the current foot key layout / 获取当前脚键布局的自定义文本标签
        /// </summary>
        private static string[] GetFootKeyText()
        {
            return Settings.Data.FootKeyViewerStyle switch
            {
                FootKeyviewerStyle.Key2 => Settings.Data.footkey2Text,
                FootKeyviewerStyle.Key4 => Settings.Data.footkey4Text,
                FootKeyviewerStyle.Key6 => Settings.Data.footkey6Text,
                FootKeyviewerStyle.Key8 => Settings.Data.footkey8Text,
                FootKeyviewerStyle.Key10 => Settings.Data.footkey10Text,
                FootKeyviewerStyle.Key12 => Settings.Data.footkey12Text,
                FootKeyviewerStyle.Key14 => Settings.Data.footkey14Text,
                FootKeyviewerStyle.Key16 => Settings.Data.footkey16Text,
                _ => new string[0]
            };
        }

        /// <summary>
        /// Get the back-row index mapping for the current main layout / 获取当前主布局的后排索引映射
        /// </summary>
        private static byte[] GetBackSequence()
        {
            return Settings.Data.KeyViewerStyle switch
            {
                KeyviewerStyle.Key8 => BackSequence8,
                KeyviewerStyle.Key12 => BackSequence12,
                KeyviewerStyle.Key14 => BackSequence14,
                KeyviewerStyle.Key16 => BackSequence16,
                KeyviewerStyle.Key20 => BackSequence20,
                KeyviewerStyle.Key24 => BackSequence24,
                KeyviewerStyle.Key10 => BackSequence10,
                _ => BackSequence16
            };
        }

        /// <summary>Format count with thousands separator if enabled / 千分位格式化数字</summary>
        private static string FormatCount(int count)
        {
            return Settings.Data.EnableCountFormatting ? count.ToString("N0") : count.ToString();
        }

        /// <summary>Refresh all key value displays (count or per-key KPS) / 刷新所有按键数值显示（计数或每键 KPS）</summary>
        public void RefreshAllCountDisplay()
        {
            if (Keys == null) return;
            for (int i = 0; i < Keys.Length; i++)
            {
                if (Keys[i] != null && Keys[i].value != null)
                {
                    if (Settings.Data.EnablePerKeyKps)
                        Keys[i].value.text = (keyPressTimes != null && i < keyPressTimes.Length && keyPressTimes[i] != null) ? keyPressTimes[i].Count.ToString() : "0";
                    else
                        Keys[i].value.text = FormatCount(Settings.Data.Count[i]);
                }
            }
            if (Total != null && Total.value != null)
                Total.value.text = FormatCount(Settings.Data.TotalCount);
        }

        public void AutoAssignRainbowColors()
        {
            int mainCount = Settings.Data.KeyViewerStyle switch
            {
                KeyviewerStyle.Key8 => 8,
                KeyviewerStyle.Key10 => 10,
                KeyviewerStyle.Key12 => 12,
                KeyviewerStyle.Key14 => 14,
                KeyviewerStyle.Key16 => 16,
                KeyviewerStyle.Key20 => 20,
                KeyviewerStyle.Key24 => 24,
                _ => 16
            };
            int footCount = FootKeySize(Settings.Data.FootKeyViewerStyle);
            Settings.Data.EnablePerKeyColors = true;
            int slot = 0;
            void AssignSlot(int i)
            {
                float hue = slot * 0.618033988f;
                hue -= Mathf.Floor(hue);
                float h = hue * 6f;
                int sector = (int)h;
                float f = h - sector;
                float p = 0.9f * (1f - 0.85f);
                float q = 0.9f * (1f - 0.85f * f);
                float t = 0.9f * (1f - 0.85f * (1f - f));
                float r, g, b;
                switch (sector % 6)
                {
                    case 0: r = 0.9f; g = t; b = p; break;
                    case 1: r = q; g = 0.9f; b = p; break;
                    case 2: r = p; g = 0.9f; b = t; break;
                    case 3: r = p; g = q; b = 0.9f; break;
                    case 4: r = t; g = p; b = 0.9f; break;
                    default: r = 0.9f; g = p; b = q; break;
                }
                Color baseColor = new Color(r, g, b);
                float bright = baseColor.grayscale > 0.5f ? 0f : 1f;
                Settings.Data.PerKeyBackground[i] = baseColor;
                Settings.Data.PerKeyBackgroundClicked[i] = Color.Lerp(baseColor, Color.white, 0.5f);
                Settings.Data.PerKeyOutline[i] = baseColor;
                Settings.Data.PerKeyOutlineClicked[i] = Color.Lerp(baseColor, Color.white, 0.7f);
                Settings.Data.PerKeyText[i] = new Color(bright, bright, bright);
                Settings.Data.PerKeyTextClicked[i] = new Color(1f - bright, 1f - bright, 1f - bright);
                Settings.Data.PerKeyRainColor[i] = baseColor;
                slot++;
            }
            for (int i = 0; i < mainCount; i++) AssignSlot(i);
            for (int i = 0; i < footCount; i++) AssignSlot(FootKeyBase + i);
            AssignSlot(MaxKeySlots);
            AssignSlot(MaxKeySlots + 1);
            ResetKeyViewer();
            ResetFootKeyViewer();
            SaveSettings();
        }
    }
}
