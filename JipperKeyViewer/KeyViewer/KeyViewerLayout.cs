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
                Loader.Error("KeyViewer: Cannot load AssetBundle, please check assets/ directory");
                return;
            }
            // Create ScreenSpaceOverlay canvas (independent of game UI) / 创建 ScreenSpaceOverlay 画布（独立于游戏 UI）
            KeyViewerObject = new GameObject("Jipper KeyViewer");
            Canvas = KeyViewerObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 2;
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
            Keys = new Key[GetKeyCount()];
            InitializeMainKeys(GetLayout(Settings.Data.KeyViewerStyle));
            // Initialize foot keys based on selected layout (full keyboard has none) / 根据选中的布局初始化脚键（全键盘无脚键）
            if (!IsFullKeyboard)
            {
                int footSize = FootKeySize(Settings.Data.FootKeyViewerStyle);
                if (footSize > 0) InitializeFootKeyViewer(footSize);
            }
            // Apply streamer mode (hide KPS/Total) — only for normal layouts; the full keyboard has its
            // own dedicated "Show KPS / Total" toggle, so don't let streamer mode fight it.
            // 应用主播模式（隐藏 KPS/Total）——仅普通布局生效；全键盘有专属开关，不与之冲突。
            if (Settings.Data.StreamerMode && !IsFullKeyboard)
            {
                if (Kps != null) Kps.gameObject.SetActive(false);
                if (Total != null) Total.gameObject.SetActive(false);
            }
            // Persist the overlay across scene loads / 使覆盖层在场景加载中持久化
            Object.DontDestroyOnLoad(KeyViewerObject);
            PressTimes = new Queue<long>(256);
            keyPressTimes = new Queue<long>[MaxKeySlots];
            for (int i = 0; i < MaxKeySlots; i++)
                keyPressTimes[i] = new Queue<long>(128);
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
            if (style == KeyviewerStyle.Full108)
                return default; // full keyboard ignores LayoutDesc (built from key108) / 全键盘不依赖 LayoutDesc（用 key108 构建）
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

        /// <summary>Whether the current layout's KPS / Total boxes use the flat (slim) left-label / right-number design / 当前布局的 KPS/Total 是否采用扁平（slim）左右设计</summary>
        private static bool KpsTotalIsSlim()
        {
            if (IsFullKeyboard) return true; // full keyboard KPS/Total are always slim / 全键盘 KPS/Total 始终为 slim
            var layout = GetLayout(Settings.Data.KeyViewerStyle);
            if (layout.extras != null)
            {
                foreach (var e in layout.extras)
                    if (e.index == -1) return e.slim;
            }
            return false;
        }

        /// <summary>Single source of truth: centered KPS/Total applies only to flat (slim) designs with the toggle on.
        /// 唯一判定：仅扁平（slim）设计且开关开启时，KPS/Total 才居中。显示开关与功能生效共用此判定，保证一致。</summary>
        private static bool KpsTotalCenteredApplies()
            => KpsTotalIsSlim() && Settings.Data.KpsTotalCentered;

        private void InitializeMainKeys(LayoutDesc layout)
        {
            if (IsFullKeyboard) { InitializeFullKeyboard(); return; }
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
        /// Build the 108-key physical keyboard layout with realistic QWERTY stagger / 构建 108 键物理键盘，真实 QWERTY 错位排布
        /// Keys are created from Settings.Data.key108 (index-aligned with BuildDefaultKey108). / 按键来自 key108 数组（下标与 BuildDefaultKey108 对齐）
        /// No foot keys, no per-key colors, no ghost keys. / 无脚键、无每键配色、无鬼键。
        /// </summary>
        private void InitializeFullKeyboard()
        {
            const float U = 50f; // base key unit / 基准键宽
            // y values are absolute px in the standard-layout visual band (top row ~350, bottom ~42).
            // DownLocation shifts the whole block down. / y 为标准布局可见视觉带内的绝对像素，DownLocation 整体下移。
            float yShift = Settings.Data.DownLocation ? 200f : 0f;
            System.Collections.Generic.List<(int idx, double x, double y, double w)> slots = new()
            {
                (0, 0*U, 580, 1*U),
                (1, 2*U, 580, 1*U),
                (2, 3*U, 580, 1*U),
                (3, 4*U, 580, 1*U),
                (4, 5*U, 580, 1*U),
                (5, 6.5*U, 580, 1*U),
                (6, 7.5*U, 580, 1*U),
                (7, 8.5*U, 580, 1*U),
                (8, 9.5*U, 580, 1*U),
                (9, 11*U, 580, 1*U),
                (10, 12*U, 580, 1*U),
                (11, 13*U, 580, 1*U),
                (12, 14*U, 580, 1*U),
                (13, 15*U, 580, 1*U),
                (14, 16*U, 580, 1*U),
                (15, 17*U, 580, 1*U),
                (16, 18*U, 580, 1*U),
                (17, 0*U, 524, 1*U),
                (18, 1*U, 524, 1*U),
                (19, 2*U, 524, 1*U),
                (20, 3*U, 524, 1*U),
                (21, 4*U, 524, 1*U),
                (22, 5*U, 524, 1*U),
                (23, 6*U, 524, 1*U),
                (24, 7*U, 524, 1*U),
                (25, 8*U, 524, 1*U),
                (26, 9*U, 524, 1*U),
                (27, 10*U, 524, 1*U),
                (28, 11*U, 524, 1*U),
                (29, 12*U, 524, 1*U),
                (30, 13*U, 524, 2*U),
                (31, 0*U, 468, 1.5*U),
                (32, 1.5*U, 468, 1*U),
                (33, 2.5*U, 468, 1*U),
                (34, 3.5*U, 468, 1*U),
                (35, 4.5*U, 468, 1*U),
                (36, 5.5*U, 468, 1*U),
                (37, 6.5*U, 468, 1*U),
                (38, 7.5*U, 468, 1*U),
                (39, 8.5*U, 468, 1*U),
                (40, 9.5*U, 468, 1*U),
                (41, 10.5*U, 468, 1*U),
                (42, 11.5*U, 468, 1*U),
                (43, 12.5*U, 468, 1*U),
                (44, 13.5*U, 468, 1.5*U),
                (45, 0*U, 412, 1.75*U),
                (46, 1.75*U, 412, 1*U),
                (47, 2.75*U, 412, 1*U),
                (48, 3.75*U, 412, 1*U),
                (49, 4.75*U, 412, 1*U),
                (50, 5.75*U, 412, 1*U),
                (51, 6.75*U, 412, 1*U),
                (52, 7.75*U, 412, 1*U),
                (53, 8.75*U, 412, 1*U),
                (54, 9.75*U, 412, 1*U),
                (55, 10.75*U, 412, 1*U),
                (56, 11.75*U, 412, 1*U),
                (57, 12.75*U, 412, 2.25*U),
                (58, 0*U, 356, 2.25*U),
                (59, 2.25*U, 356, 1*U),
                (60, 3.25*U, 356, 1*U),
                (61, 4.25*U, 356, 1*U),
                (62, 5.25*U, 356, 1*U),
                (63, 6.25*U, 356, 1*U),
                (64, 7.25*U, 356, 1*U),
                (65, 8.25*U, 356, 1*U),
                (66, 9.25*U, 356, 1*U),
                (67, 10.25*U, 356, 1*U),
                (68, 11.25*U, 356, 1*U),
                (69, 12.25*U, 356, 2.75*U),
                (70, 0*U, 300, 1.25*U),
                (71, 1.25*U, 300, 1.25*U),
                (72, 2.5*U, 300, 1.25*U),
                (73, 3.75*U, 300, 6.25*U),
                (74, 10*U, 300, 1.25*U),
                (75, 11.25*U, 300, 1.25*U),
                (76, 12.5*U, 300, 1.25*U),
                (77, 13.75*U, 300, 1.25*U),
                (78, 19*U, 524, 1*U),
                (79, 19*U, 468, 1*U),
                (80, 20*U, 524, 1*U),
                (81, 20*U, 468, 1*U),
                (82, 21*U, 524, 1*U),
                (83, 21*U, 468, 1*U),
                (84, 20*U, 356, 1*U),
                (85, 19*U, 300, 1*U),
                (86, 20*U, 300, 1*U),
                (87, 21*U, 300, 1*U),
                (88, 22*U, 524, 1*U),
                (89, 23*U, 524, 1*U),
                (90, 24*U, 524, 1*U),
                (91, 25*U, 524, 1*U),
                (92, 22*U, 468, 1*U),
                (93, 23*U, 468, 1*U),
                (94, 24*U, 468, 1*U),
                (95, 25*U, 468, 1*U),
                (96, 22*U, 412, 1*U),
                (97, 23*U, 412, 1*U),
                (98, 24*U, 412, 1*U),
                (99, 22*U, 356, 1*U),
                (100, 23*U, 356, 1*U),
                (101, 24*U, 356, 1*U),
                (102, 22*U, 300, 2*U),
                (103, 24*U, 300, 1*U),
                (104, 25*U, 300, 1*U)
            };

            // Uniform 6px horizontal gap (matching the 6px vertical row gap): scale every key's x and
            // width from the 50px unit to a 56px column step, then trim 6px off each width. Because the
            // original layout had 0px gaps between flush keys, this yields exactly 6px gaps everywhere
            // while preserving the exact relative positions (no rearrangement, no overlap).
            // 统一 6px 横向间隙（与纵向 6px 行隙一致）：把每个键的 x 与宽从 50px 基准缩放到 56px 列步进，再各减 6px。
            // 原布局紧贴键之间为 0 间隙，故缩放后处处得到恰好 6px 间隙，且相对位置完全不变（不重排、不重叠）。
            const float colStep = 56f;   // 50px key + 6px gap / 键宽50 + 间隙6
            const float gap = 6f;
            // Pull the right cluster left so its left edge hugs the main block's right edge (same role
            // as the old 4U shift, now expressed in the 56px column step). / 把右簇左拉，使其左缘贴住主键区右缘（等价于原 4U 左移，现按 56px 列步进换算）。
            const float rightClusterShift = 4f * colStep;
            const float rowStep = 56f; // vertical distance between key rows / 按键行间距
            foreach (var s in slots)
            {
                float x = (float)s.x * colStep / U - (s.idx >= 78 ? rightClusterShift : 0f);
                if (s.idx == 95 || s.idx == 104)
                {
                    // Numpad "+" (95) and Enter (104) are tall vertical keys, each spanning two rows so the
                    // right column is fully filled (no gaps). / 小键盘「+」(95)与「回车」(104)为竖排高键，各跨两行，右列恰好占满无空隙。
                    float w = colStep - gap; // matches a normal key's 50px width / 与普通键 50px 同宽
                    // height = two key rows (rowStep=56) so the tall key's top/bottom exactly meet the
                    // neighbouring keys' edges (no stray 3px gap, looks the same height as two stacked
                    // normal keys). / 高键取两行距 56px，使其顶/底正好贴合相邻两键边缘，视觉高度与两枚普通键叠放一致。
                    float h = rowStep + 50f;
                    // "+" (95) sits on the 6-key (y=412) and 3-key (y=356) rows; Enter (104) sits on the
                    // 0-key (y=300) and 3-key (y=356) rows. Center each at the midpoint of its two rows. /
                    // 「+」以 6键/3键 中点(384) 为中心；回车以 0键/3键 中点(328) 为中心。
                    float y = (s.idx == 95 ? (468f + 412f) : (300f + 356f)) * 0.5f;
                    Keys[s.idx] = CreateKey(s.idx, x, y - yShift, w, -1, false, false, h);
                }
                else
                {
                    float w = (float)s.w * colStep / U - gap;
                    Keys[s.idx] = CreateKey(s.idx, x, (float)s.y - yShift, w, -1, false, false);
                }
            }
            // Optional KPS / Total boxes, placed at user-set normalized positions / 可选 KPS/Total，位置由用户归一化坐标决定
            if (Settings.Data.FullKeyboardShowKpsTotal)
            {
                float ktSize = Settings.Data.FullKeyboardKpsTotalSize;
                Kps = CreateKey(-1, Settings.Data.FullKpsPosition.x * CanvasWidth, Settings.Data.FullKpsPosition.y * 1080f, ktSize, -1, true, true);
                Total = CreateKey(-2, Settings.Data.FullTotalPosition.x * CanvasWidth, Settings.Data.FullTotalPosition.y * 1080f, ktSize, -1, true, true);
            }
            ApplyFullKeyboardKpsTotalPosition();
            ApplyFullKeyboardColors();
            CaptureFullKeyboardHome();
        }

        /// <summary>Snapshot the natural anchored positions of every full-keyboard key + KPS/Total / 记录全键盘各键及 KPS/Total 的自然锚点位置</summary>
        private void CaptureFullKeyboardHome()
        {
            if (Keys == null) { _fkHomeValid = false; return; }
            _fkHome = new Vector2[Keys.Length];
            for (int i = 0; i < Keys.Length; i++)
                _fkHome[i] = Keys[i] != null ? ((RectTransform)Keys[i].transform).anchoredPosition : Vector2.zero;
            _fkHomeValid = true;
        }

        /// <summary>Apply unified colors to all 108-key layout keys / 将统一配色应用到全部 108 键</summary>
        private void ApplyFullKeyboardColors()
        {
            if (Keys == null) return;
            var d = Settings.Data;
            bool unified = d.EnableFullKeyboardUnifiedColor;
            Color bg = unified ? d.FullKeyboardBackground : d.Background;
            Color bgC = unified ? d.FullKeyboardBackgroundClicked : d.BackgroundClicked;
            Color ol = unified ? d.FullKeyboardOutline : d.Outline;
            Color olC = unified ? d.FullKeyboardOutlineClicked : d.OutlineClicked;
            Color tx = unified ? d.FullKeyboardText : d.Text;
            Color txC = unified ? d.FullKeyboardTextClicked : d.TextClicked;
            for (int i = 0; i < Keys.Length; i++)
            {
                if (Keys[i] == null) continue;
                bool pressed = Keys[i].isPressed;
                Keys[i].background.color = pressed ? bgC : bg;
                Keys[i].outline.color = pressed ? olC : ol;
                Keys[i].text.color = pressed ? txC : tx;
                if (Keys[i].value != null) Keys[i].value.color = pressed ? txC : tx;
            }
            ApplyKpsTotalColors();
        }

        /// <summary>Apply user-set normalized positions to the KPS / Total boxes (full keyboard only) / 将用户设置的归一化位置套用到 KPS/Total 框（仅全键盘）</summary>
        private void ApplyFullKeyboardKpsTotalPosition()
        {
            if (Kps != null)
                SetKeyPosition(-1, Settings.Data.FullKpsPosition.x * CanvasWidth, Settings.Data.FullKpsPosition.y * 1080f);
            if (Total != null)
                SetKeyPosition(-2, Settings.Data.FullTotalPosition.x * CanvasWidth, Settings.Data.FullTotalPosition.y * 1080f);
        }

        /// <summary>Redirect back-row rain containers to front-row ones for layouts with non-standard key widths.
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

        /// <summary>Translate the whole 108-key block by the normalized custom-position offset / 按归一化自定义位置整体平移 108 键</summary>
        private void RepositionFullKeyboard()
        {
            if (!_fkHomeValid || _fkHome == null || Keys == null) return;
            Vector2 norm = Settings.Data.MainKeyViewerPosition;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < _fkHome.Length; i++)
            {
                Vector2 p = _fkHome[i];
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }
            // Edge-pinned normalized placement (Y grows upward; 1080 = top, 0 = bottom).
            // norm.x=0 -> left edge at screen left;  norm.x=1 -> right edge at screen right.
            // norm.y=0 -> bottom edge at screen bottom; norm.y=1 -> top edge at screen top.
            // DownLocation is already baked into the captured home positions, so no extra shift here.
            // 归一化贴边定位（Y 向上，1080=顶，0=底）。norm.x=0 左缘贴左，=1 右缘贴右；
            // norm.y=0 底缘贴底，=1 顶缘贴顶。DownLocation 已含在 home 中，不再额外偏移。
            float dxLeft = -minX;
            float dxRight = CanvasWidth - maxX;
            float dx = Mathf.Lerp(dxLeft, dxRight, norm.x);
            float dyBottom = 25f - minY;        // bottom edge (minY - 25) -> y = 0
            float dyTop = (1080f - 25f) - maxY; // top edge (maxY + 25) -> y = 1080
            float dy = Mathf.Lerp(dyTop, dyBottom, norm.y); // Y inverted to match the standard layout (norm.y=1 -> bottom) / Y 反向，与标准布局一致（norm.y=1 贴底）
            for (int i = 0; i < Keys.Length; i++)
            {
                if (Keys[i] == null) continue;
                ((RectTransform)Keys[i].transform).anchoredPosition = _fkHome[i] + new Vector2(dx, dy);
            }
            // KPS / Total keep their own independent normalized positions (not shifted by the main block).
            // KPS/Total 保持各自独立的归一化位置，不随主键盘自定义位置移动。
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
        private Key CreateKey(int i, float x, float y, float sizeX, int raining, bool slim = false, bool count = true, float sizeY = 0f)
        {
            if (i >= 0 && i < FootKeyBase && Settings.Data.HideMainKeyCount)
                count = false;
            // KPS / Total boxes: in centered mode (flat / slim designs only) the label and value merge into
            // one centered text box (value box not created, so "KPS 123" grows and recenters as number widens).
            // Stacked (non-slim) KPS/Total keep the separate top-label / bottom-number layout.
            // 居中模式仅对扁平（slim）设计的 KPS/Total 生效：标签与数值合并为单个居中文本框，数值变长时整行加宽并重新居中。
            // 堆叠（非 slim）的 KPS/Total 保持上文本下数值的分离布局。
            bool centeredText = (i == -1 || i == -2) && KpsTotalCenteredApplies();
            GameObject obj = new("Key " + i);
            KeyViewerSettings settings = Settings;
            float h = sizeY > 0f ? sizeY : (slim ? 30f : 50f);
            RectTransform transform = obj.AddComponent<RectTransform>();
            transform.SetParent(KeyViewerSizeObject.transform);
            transform.sizeDelta = new Vector2(sizeX, h);
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
            vrt.sizeDelta = new Vector2(sizeX, h);
            vrt.anchorMin = vrt.anchorMax = new Vector2(0, 0.5f);
            vrt.pivot = new Vector2(0.5f, 0.5f);
            vrt.anchoredPosition = new Vector2(sizeX * 0.5f, 0);
            vrt.localScale = Vector3.one;
            key.visuals = visuals.transform;
            key.background = CreateImage(visuals, "Background", sizeX, h, keyBackgroundSprite, settings.Data.Background);
            key.outline = CreateImage(visuals, "Outline", sizeX, h, keyOutlineSprite, settings.Data.Outline);
            key.text = CreateKeyText(visuals, sizeX, slim, count, settings, centeredText);
            if (count && !centeredText)
                key.value = CreateCountText(visuals, sizeX, slim, settings);
            UpdateKeyText(key, i);
            SetupRainContainer(key, obj, sizeX, raining);
            ApplyKeyColors(key, i, raining);
            return key;
        }

        private static Image CreateImage(GameObject parent, string name, float sizeX, float sizeY, Sprite sprite, Color color)
        {
            GameObject go = new(name);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent.transform);
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(sizeX * 2, sizeY * 2);
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

        private TextMeshProUGUI CreateKeyText(GameObject parent, float sizeX, bool slim, bool count, KeyViewerSettings settings, bool centered = false)
        {
            GameObject go = new("KeyText");
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent.transform);
            if (centered)
            {
                // Single merged, full-width, centered box for the combined "label value" string.
                // 合并用的整宽居中框，承载 "标签 数值" 整体。
                rt.sizeDelta = new Vector2(sizeX - 4, 32);
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
            }
            else if (slim)
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
            TextAlignmentOptions align = centered ? TextAlignmentOptions.Center : (slim ? TextAlignmentOptions.Left : TextAlignmentOptions.Center);
            return ConfigureText(go, settings, align);
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
            TextAlignmentOptions align = slim ? TextAlignmentOptions.Right : TextAlignmentOptions.Top;
            return ConfigureText(go, settings, align);
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
            if (IsFullKeyboard) return; // colors set by ApplyFullKeyboardColors after creation
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
        /// Set the KPS / Total display. In centered mode the label and value merge into one centered
        /// string ("KPS 123") that recenters as the number widens; otherwise they stay separate.
        /// 设置 KPS/Total 显示。居中模式下标签与数值合并为单个居中字符串（"KPS 123"），数值变宽时整体重新居中；否则保持分开。
        /// </summary>
        private static void SetKpsTotalDisplay(Key key, string label, string valueStr)
        {
            if (key == null) return;
            if (KpsTotalCenteredApplies())
            {
                if (key.value != null) key.value.gameObject.SetActive(false);
                key.text.text = label + " " + valueStr;
            }
            else
            {
                if (key.value != null)
                {
                    key.value.gameObject.SetActive(true);
                    key.value.text = valueStr;
                }
                key.text.text = label;
            }
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
                SetKpsTotalDisplay(key, "KPS", "0");
                return;
            }
            if (i == -2)
            {
                SetKpsTotalDisplay(key, "Total", FormatCount(Settings.Data.TotalCount));
                return;
            }
            if (IsFullKeyboard)
            {
                // Full keyboard: text comes straight from the key code (no per-key custom text).
                // 全键盘：文本直接来自按键代码（无每键自定义文本）。
                KeyCode[] keyCodes = GetKeyCode();
                if (keyCodes != null && i >= 0 && i < keyCodes.Length)
                {
                    key.text.text = KeyToString(keyCodes[i]);
                }
            }
            else if (i < FootKeyBase)
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
            if (IsFullKeyboard) { RepositionFullKeyboard(); return; }
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
            if (IsFullKeyboard) return; // full keyboard has no foot keys / 全键盘无脚键
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

        // Full-keyboard custom-position support: captured home (natural) anchored positions so the
        // whole 108K block can be translated by the normalized offset. / 全键盘自定义位置：记录每个键的自然位置，整体平移。
        private Vector2[] _fkHome;
        private bool _fkHomeValid;

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
            if (IsFullKeyboard) { ApplyFullKeyboardColors(); return; }
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
                if (IsFullKeyboard && Settings.Data.EnableFullKeyboardUnifiedColor)
                {
                    k.background.color = Settings.Data.FullKeyboardBackground;
                    k.outline.color = Settings.Data.FullKeyboardOutline;
                    k.text.color = Settings.Data.FullKeyboardText;
                    if (k.value != null) k.value.color = Settings.Data.FullKeyboardText;
                }
                else
                {
                    k.background.color = Settings.Data.KpsBackground;
                    k.outline.color = Settings.Data.KpsOutline;
                    k.text.color = Settings.Data.KpsText;
                    if (k.value != null) k.value.color = Settings.Data.KpsText;
                }
            }
            else if (pi == Keys.Length + 1)
            {
                if (IsFullKeyboard && Settings.Data.EnableFullKeyboardUnifiedColor)
                {
                    k.background.color = Settings.Data.FullKeyboardBackground;
                    k.outline.color = Settings.Data.FullKeyboardOutline;
                    k.text.color = Settings.Data.FullKeyboardText;
                    if (k.value != null) k.value.color = Settings.Data.FullKeyboardText;
                }
                else
                {
                    k.background.color = Settings.Data.TotalBackground;
                    k.outline.color = Settings.Data.TotalOutline;
                    k.text.color = Settings.Data.TotalText;
                    if (k.value != null) k.value.color = Settings.Data.TotalText;
                }
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
                // Destroy EVERY child under the size object (main keys, foot keys, KPS/Total boxes,
                // any leaks) so no stale key survives a layout switch. Relying on the Keys array length
                // alone misses foot keys when switching to the full keyboard (different array size).
                // 销毁 SizeObject 下全部子物体（主键/脚键/KPS/Total/任何残留），
                // 不依赖数组长度——切到全键盘时数组长度变化会漏掉脚键。
                rainSystem.ClearActiveDrops(Keys);
                if (KeyViewerSizeObject != null)
                {
                    var children = KeyViewerSizeObject.transform;
                    for (int c = children.childCount - 1; c >= 0; c--)
                        Object.Destroy(children.GetChild(c).gameObject);
                }
                Total = null;
                Kps = null;
            }
            // Array length must match the target layout (40 standard / 105 full keyboard).
            // 数组长度必须匹配目标布局（标准40/全键盘105）。
            Keys = new Key[GetKeyCount()];
            Total = null;
            Kps = null;
            rainSystem.ClearPool();
            InitializeMainKeys(GetLayout(Settings.Data.KeyViewerStyle));
            // Rebuild foot keys too: ResetKeyViewer now destroys every child (including foot keys)
            // to avoid leaking them when switching to the full keyboard, so they must be recreated here.
            // 同时重建脚键：ResetKeyViewer 现在销毁全部子物体（含脚键）以避免切到全键盘时残留，故需在此重建。
            ResetFootKeyViewer();
            if (Settings.Data.StreamerMode && !IsFullKeyboard)
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
            if (IsFullKeyboard) return; // full keyboard has no foot keys / 全键盘无脚键
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
                KeyviewerStyle.Full108 => Settings.Data.key108,
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
                KeyviewerStyle.Full108 => new KeyCode[0],
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
            if (Total != null)
                SetKpsTotalDisplay(Total, "Total", FormatCount(Settings.Data.TotalCount));
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





