// Merged rain rendering / 雨滴合并渲染
// RainLayer draws every rain drop into one white-texture mesh: normal drop bodies plus the ghost
// drops' shadow/outline quads. GhostRainLayer draws the ghost sprite bodies, sharing the per-key
// press scales owned by RainLayer. Both read the RawRain state maintained by RainSystem, replacing
// the per-key RainLine canvases and the pooled Rain GameObjects — no per-drop RectTransform writes,
// no SetSiblingIndex, one rebuild per layer per frame while raining.
// RainLayer 把所有雨滴画进一个白贴图 mesh：普通雨滴本体 + 鬼雨的阴影/描边四边形。GhostRainLayer
// 画鬼雨贴图本体，与 RainLayer 共享每键按压缩放。两者读取 RainSystem 维护的 RawRain 状态，
// 取代每键 RainLine 画布与 Rain 对象池——无逐雨滴 RectTransform 写入、无 SetSiblingIndex，
// 下雨期间每层每帧只重建一次。

using UnityEngine;
using UnityEngine.UI;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>
    /// Solid-color rain quads: normal drop bodies + ghost shadow/outline / 纯色雨滴四边形：普通本体 + 鬼雨阴影/描边
    /// </summary>
    public class RainLayer : MaskableGraphic
    {
        /// <summary>Rain system providing the drop state / 提供雨滴状态的雨滴系统</summary>
        internal RainSystem System;
        /// <summary>Per-key rain press scale (EnablePressAnimationOnRain), shared with the ghost layer / 每键雨滴按压缩放（雨滴跟随动画），与鬼雨层共享</summary>
        private float[] rainScales;

        private GhostRainLayer ghostLayer;

        public RainLayer()
        {
            raycastTarget = false;
        }

        /// <summary>Reset per-key scales for a new key count (called on rebuilds) / 为新键数重置每键缩放（重建时调用）</summary>
        public void Init(int keyCount)
        {
            rainScales = new float[keyCount];
            for (int i = 0; i < keyCount; i++) rainScales[i] = 1f;
            MarkDirty();
        }

        public void AttachGhostLayer(GhostRainLayer ghost)
        {
            ghostLayer = ghost;
            MarkDirty();
        }

        /// <summary>Set the ghost layer's sprite (null = ghost bodies hidden) / 设置鬼雨层贴图（null = 鬼雨本体不显示）</summary>
        public void SetSprite(Sprite sprite)
        {
            // Single-quad UV tiling needs Repeat — only safe to flip on a standalone texture; a
            // packed/atlas sprite keeps its wrap mode and falls back to per-tile quads.
            // 单四边形 UV 平铺需要 Repeat——仅在独立贴图上翻转才安全；打包/图集贴图保持原 wrap，
            // 渲染时回退为逐平铺四边形。
            if (sprite != null && sprite.texture != null && IsStandaloneRect(sprite))
                sprite.texture.wrapMode = TextureWrapMode.Repeat;
            if (ghostLayer != null) ghostLayer.Sprite = sprite;
            SetVerticesDirty();
            SetMaterialDirty();
            if (ghostLayer != null)
            {
                ghostLayer.SetVerticesDirty();
                ghostLayer.SetMaterialDirty();
            }
        }

        /// <summary>Whether the sprite's rect covers its whole texture (no atlas packing) / 贴图矩形是否覆盖整张贴图（无图集打包）</summary>
        internal static bool IsStandaloneRect(Sprite sprite)
        {
            Rect tr = sprite.textureRect;
            Texture tex = sprite.texture;
            return tr.x <= 0.01f && tr.y <= 0.01f && tr.xMax >= tex.width - 0.01f && tr.yMax >= tex.height - 0.01f;
        }

        public void SetKeyScale(int keyIndex, float scale)
        {
            if (rainScales == null || keyIndex < 0 || keyIndex >= rainScales.Length) return;
            if (rainScales[keyIndex] == scale) return;
            rainScales[keyIndex] = scale;
            MarkDirty();
        }

        public float GetKeyScale(int keyIndex)
            => rainScales != null && keyIndex >= 0 && keyIndex < rainScales.Length ? rainScales[keyIndex] : 1f;

        public void MarkDirty()
        {
            SetVerticesDirty();
            if (ghostLayer != null) ghostLayer.SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Key[] keys = System != null ? System.Keys : null;
            if (keys == null) return;
            for (int i = 0; i < keys.Length; i++)
            {
                Key key = keys[i];
                if (key == null || key.rainList.Count == 0) continue;
                for (int d = 0; d < key.rainList.Count; d++)
                {
                    RawRain rain = key.rainList[d];
                    if (rain.removed || rain.isGhost && !rain.shadowEnabled && !rain.outlineEnabled) continue;
                    DrawDrop(vh, rain, drawMain: !rain.isGhost);
                }
            }
        }

        /// <summary>Emit one drop's shadow/outline/(main) quads with the trail gradient / 输出一滴雨的阴影/描边/(本体)四边形及轨迹渐变</summary>
        private static void DrawDrop(VertexHelper vh, RawRain rain, bool drawMain)
        {
            Rect r = rain.rect;
            if (r.width <= 0f || r.height <= 0f) return;
            // Old ghosts kept their RainGraphic white (only the Image body was tinted), so shadow /
            // outline alpha follows the fade only; normal drops tint everything with the rain color.
            // 旧鬼雨的 RainGraphic 保持白色（仅 Image 本体着色），阴影/描边透明度只受淡出影响；
            // 普通雨滴全部以雨色着色。
            Color baseCol = rain.isGhost
                ? new Color(1f, 1f, 1f, rain.alpha)
                : new Color(rain.mainColor.r, rain.mainColor.g, rain.mainColor.b, rain.mainColor.a * rain.alpha);
            float h = r.height;
            float span = rain.dFar - rain.dNear;
            bool simple = rain.fadePx <= 0.5f || rain.trackHeight <= 0.5f || span <= 0.0001f;
            float sf = rain.scaleF;

            if (rain.shadowEnabled)
            {
                Color sc = rain.shadowColor;
                sc.a *= baseCol.a;
                DrawRainQuad(vh, r.xMin + rain.shadowOffsetX * sf, r.xMax + rain.shadowOffsetX * sf,
                    r.yMin + rain.shadowOffsetY * sf, r.yMax + rain.shadowOffsetY * sf, h, sc,
                    rain.dNear, rain.dFar, rain.trackHeight, rain.fadePx, span, simple);
            }
            if (rain.outlineEnabled)
            {
                Color oc = rain.outlineColor;
                oc.a *= baseCol.a;
                float ow = rain.outlineWidth * sf;
                DrawRainQuad(vh, r.xMin - ow, r.xMax + ow, r.yMin - ow, r.yMax + ow, h + ow * 2, oc,
                    rain.dNear, rain.dFar, rain.trackHeight, rain.fadePx, span, simple);
            }
            if (drawMain)
                DrawRainQuad(vh, r.xMin, r.xMax, r.yMin, r.yMax, h, baseCol,
                    rain.dNear, rain.dFar, rain.trackHeight, rain.fadePx, span, simple);
        }

        /// <summary>Trail-gradient quad, ported verbatim from the old RainGraphic / 轨迹渐变四边形，自旧 RainGraphic 原样移植</summary>
        private static void DrawRainQuad(VertexHelper vh, float xL, float xR, float yB, float yT, float h, Color col,
            float dNear, float dFar, float trackH, float fade, float span, bool simple)
        {
            if (simple)
            {
                AddQuad(vh, xL, xR, yB, yT, 0f, 1f, 0f, 1f, col, col);
                return;
            }

            float fadeStartD = trackH - fade;
            float aNear = AlphaAtD(dNear, fadeStartD, trackH, fade);
            float aFar = AlphaAtD(dFar, fadeStartD, trackH, fade);
            Color colNear = col; colNear.a = col.a * aNear;
            Color colFar = col; colFar.a = col.a * aFar;

            bool crosses = dNear < fadeStartD && dFar > fadeStartD;
            if (!crosses)
            {
                // reverseFade was always false in the old renderer / 旧渲染器 reverseFade 恒为 false
                AddQuad(vh, xL, xR, yB, yT, 0f, 1f, 0f, 1f, colNear, colFar);
                return;
            }
            float t = (fadeStartD - dNear) / span;
            float yMid = yB + t * h;
            AddQuad(vh, xL, xR, yB, yMid, 0f, 1f, 0f, 0.5f, colNear, col);
            AddQuad(vh, xL, xR, yMid, yT, 0f, 1f, 0.5f, 1f, col, colFar);
        }

        private static float AlphaAtD(float d, float fadeStartD, float trackH, float fade)
        {
            if (d <= fadeStartD) return 1f;
            if (d >= trackH) return 0f;
            return (trackH - d) / fade;
        }

        internal static void AddQuad(VertexHelper vh, float x0, float x1, float y0, float y1,
            float u0, float u1, float v0, float v1, Color bot, Color top)
        {
            int i = vh.currentVertCount;
            UIVertex vert = UIVertex.simpleVert;
            vert.position = new Vector3(x0, y0, 0f);
            vert.uv0 = new Vector4(u0, v0, 0f, 0f);
            vert.color = bot;
            vh.AddVert(vert);
            vert.position = new Vector3(x1, y0, 0f);
            vert.uv0 = new Vector4(u1, v0, 0f, 0f);
            vh.AddVert(vert);
            vert.position = new Vector3(x1, y1, 0f);
            vert.uv0 = new Vector4(u1, v1, 0f, 0f);
            vert.color = top;
            vh.AddVert(vert);
            vert.position = new Vector3(x0, y1, 0f);
            vert.uv0 = new Vector4(u0, v1, 0f, 0f);
            vh.AddVert(vert);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }

    /// <summary>
    /// Ghost rain sprite bodies — reproduced as uGUI Image.Type.Tiled did: with a bordered sprite
    /// (the 11px 9-slice GhostRain border) the inner region and border strips tile separately;
    /// borderless standalone textures use a single Repeat-wrapped UV quad / 鬼雨贴图本体——按
    /// uGUI Image.Type.Tiled 的行为复刻：带边框贴图（GhostRain 的 11px 九宫格）内区与边框条
    /// 分别平铺；无边框独立贴图用 Repeat wrap 的单四边形 UV 平铺
    /// </summary>
    public class GhostRainLayer : MaskableGraphic
    {
        internal RainSystem System;

        public Sprite Sprite { get; set; }

        public override Texture mainTexture => Sprite != null ? Sprite.texture : base.mainTexture;

        public GhostRainLayer()
        {
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Sprite == null) return;
            Key[] keys = System != null ? System.Keys : null;
            if (keys == null) return;
            Rect tr = Sprite.textureRect;
            Texture tex = Sprite.texture;
            float tw = tex.width;
            float th = tex.height;
            Vector4 border = Sprite.border;
            // The GhostRain sprite carries an 11px 9-slice border (matching the Unity import
            // settings), and the old ghost Image used Image.Type.Tiled — which for a bordered
            // sprite tiles the INNER region and the border strips separately (per-tile quads),
            // not the whole texture. Port that layout here; whole-texture tiling (single Repeat
            // quad or per-tile quads) only applies to borderless sprites.
            // GhostRain 贴图带 11px 九宫格边框（与 Unity 导入设置一致），旧版鬼雨 Image 是
            // Image.Type.Tiled——带边框的 sprite 只平铺内侧区域，边框条单独按方向平铺
            //（逐四边形），而不是整张贴图重复。此处移植该布局；整图平铺（Repeat 单四边形
            // 或逐块四边形）仅适用于无边框贴图。
            bool hasBorder = border.x > 0f || border.y > 0f || border.z > 0f || border.w > 0f;
            // Outer = textureRect, inner = inset by border (uGUI DataUtility outer/inner UV) /
            // 外圈 = textureRect，内圈 = 内缩边框（uGUI DataUtility 的 outer/inner UV）
            float oL = tr.x / tw, oR = tr.xMax / tw, oB = tr.y / th, oT = tr.yMax / th;
            float iL = (tr.x + border.x) / tw, iR = (tr.xMax - border.z) / tw;
            float iB = (tr.y + border.y) / th, iT = (tr.yMax - border.w) / th;
            float tileW = Mathf.Max(1f, tr.width - border.x - border.z);
            float tileH = Mathf.Max(1f, tr.height - border.y - border.w);
            bool standalone = !hasBorder && RainLayer.IsStandaloneRect(Sprite);
            for (int i = 0; i < keys.Length; i++)
            {
                Key key = keys[i];
                if (key == null || key.rainList.Count == 0) continue;
                for (int d = 0; d < key.rainList.Count; d++)
                {
                    RawRain rain = key.rainList[d];
                    if (rain.removed || !rain.isGhost) continue;
                    Rect r = rain.rect;
                    if (r.width <= 0f || r.height <= 0f) continue;
                    Color c = rain.mainColor;
                    c.a *= rain.alpha;
                    if (hasBorder)
                        DrawTiledWithBorder(vh, r, c, border.x, border.z, border.y, border.w,
                            tileW, tileH, oL, iL, iR, oR, oB, iB, iT, oT);
                    else if (standalone)
                    {
                        // UV anchored bottom-left, tiling per tile size — matches the old tiled Image /
                        // UV 锚定左下，按贴图尺寸平铺——与旧平铺 Image 一致
                        RainLayer.AddQuad(vh, r.xMin, r.xMax, r.yMin, r.yMax,
                            0f, r.width / tileW, 0f, r.height / tileH, c, c);
                    }
                    else
                    {
                        // Packed borderless sprite: per-tile quads keep UVs inside the sprite rect
                        // (no atlas bleed, no Repeat needed) / 打包无边框贴图：逐平铺四边形把
                        // UV 限制在贴图矩形内（不越界采样图集，也无需 Repeat）
                        AddTiled(vh, r, tr, tw, th, tileW, tileH, c);
                    }
                }
            }
        }

        /// <summary>
        /// Port of uGUI Image.Type.Tiled for a bordered sprite (GenerateTiledSpriteToVertexBuffer):
        /// the border strips tile only along their own direction — sampled from the inner band —
        /// and the center region tiles as whole quads clipped at the edge. Borders squeeze
        /// proportionally when the rect is smaller than the combined borders (GetAdjustedBorders).
        /// uGUI Image.Type.Tiled 带边框 sprite 的移植（GenerateTiledSpriteToVertexBuffer）：
        /// 边框条只沿自身方向平铺——取样自内侧带——中心区域整块平铺并在边缘裁剪。
        /// 矩形容不下两侧边框时按比例压缩（GetAdjustedBorders）。
        /// </summary>
        private static void DrawTiledWithBorder(VertexHelper vh, Rect r, Color c,
            float bx, float bz, float by, float bw, float tileW, float tileH,
            float oL, float iL, float iR, float oR, float oB, float iB, float iT, float oT)
        {
            float cbx = bx + bz;
            if (r.width < cbx && cbx > 0f) { float k = r.width / cbx; bx *= k; bz *= k; }
            float cby = by + bw;
            if (r.height < cby && cby > 0f) { float k = r.height / cby; by *= k; bw *= k; }
            float xMin = bx, xMax = r.width - bz, yMin = by, yMax = r.height - bw;
            if (xMax < xMin) xMax = xMin;
            if (yMax < yMin) yMax = yMin;
            long nTilesW = (long)Mathf.Ceil((xMax - xMin) / tileW);
            long nTilesH = (long)Mathf.Ceil((yMax - yMin) / tileH);
            float px = r.xMin, py = r.yMin;

            // Center tiles / 中心平铺
            for (long j = 0; j < nTilesH; j++)
            {
                float y1 = yMin + j * tileH, y2 = y1 + tileH;
                float vClip = iT;
                if (y2 > yMax) { vClip = iB + (iT - iB) * (yMax - y1) / (y2 - y1); y2 = yMax; }
                for (long i = 0; i < nTilesW; i++)
                {
                    float x1 = xMin + i * tileW, x2 = x1 + tileW;
                    float uClip = iR;
                    if (x2 > xMax) { uClip = iL + (iR - iL) * (xMax - x1) / (x2 - x1); x2 = xMax; }
                    RainLayer.AddQuad(vh, px + x1, px + x2, py + y1, py + y2, iL, uClip, iB, vClip, c, c);
                }
            }
            // Left and right border columns, tiled vertically from the inner band / 左右边框列，
            // 取样内侧带纵向平铺
            for (long j = 0; j < nTilesH; j++)
            {
                float y1 = yMin + j * tileH, y2 = y1 + tileH;
                float vClip = iT;
                if (y2 > yMax) { vClip = iB + (iT - iB) * (yMax - y1) / (y2 - y1); y2 = yMax; }
                RainLayer.AddQuad(vh, px, px + xMin, py + y1, py + y2, oL, iL, iB, vClip, c, c);
                RainLayer.AddQuad(vh, px + xMax, px + r.width, py + y1, py + y2, iR, oR, iB, vClip, c, c);
            }
            // Bottom and top border rows, tiled horizontally from the inner band / 上下边框行，
            // 取样内侧带横向平铺
            for (long i = 0; i < nTilesW; i++)
            {
                float x1 = xMin + i * tileW, x2 = x1 + tileW;
                float uClip = iR;
                if (x2 > xMax) { uClip = iL + (iR - iL) * (xMax - x1) / (x2 - x1); x2 = xMax; }
                RainLayer.AddQuad(vh, px + x1, px + x2, py, py + yMin, iL, uClip, oB, iB, c, c);
                RainLayer.AddQuad(vh, px + x1, px + x2, py + yMax, py + r.height, iL, uClip, iT, oT, c, c);
            }
            // Corners / 四角
            RainLayer.AddQuad(vh, px, px + xMin, py, py + yMin, oL, iL, oB, iB, c, c);
            RainLayer.AddQuad(vh, px + xMax, px + r.width, py, py + yMin, iR, oR, oB, iB, c, c);
            RainLayer.AddQuad(vh, px, px + xMin, py + yMax, py + r.height, oL, iL, iT, oT, c, c);
            RainLayer.AddQuad(vh, px + xMax, px + r.width, py + yMax, py + r.height, iR, oR, iT, oT, c, c);
        }

        private static void AddTiled(VertexHelper vh, Rect r, Rect tr, float texW, float texH, float tileW, float tileH, Color c)
        {
            float u0 = tr.x / texW, u1 = tr.xMax / texW;
            float v0 = tr.y / texH, v1 = tr.yMax / texH;
            float x = r.xMin;
            while (x < r.xMax - 0.01f)
            {
                float wTile = Mathf.Min(tileW, r.xMax - x);
                float uu1 = u0 + (u1 - u0) * (wTile / tileW);
                float y = r.yMin;
                while (y < r.yMax - 0.01f)
                {
                    float hTile = Mathf.Min(tileH, r.yMax - y);
                    float vv1 = v0 + (v1 - v0) * (hTile / tileH);
                    RainLayer.AddQuad(vh, x, x + wTile, y, y + hTile, u0, uu1, v0, vv1, c, c);
                    y += hTile;
                }
                x += wTile;
            }
        }
    }
}
