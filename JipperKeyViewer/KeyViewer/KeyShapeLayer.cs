// Merged key-box rendering / 按键框合并渲染
// One Graphic per sprite (background / outline) draws every key box into a single mesh, replacing
// the old per-key Background/Outline Image hierarchy. Slot state (rect / colors / press scale /
// visibility) is owned by the background layer and shared by the outline layer, so a color or press
// change rebuilds one small mesh instead of re-batching hundreds of CanvasRenderers.
// 每张贴图一个 Graphic（背景/描边），把所有按键框画进单一 mesh，取代旧的每键 Background/Outline
// Image 层级。槽位状态（矩形/颜色/按压缩放/可见性）由背景层持有并与描边层共享，颜色或按压变化
// 只重建一个小 mesh，不再全画布重批。

using UnityEngine;
using UnityEngine.UI;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>
    /// Draws all key boxes (9-sliced background or outline sprite) into one mesh / 将所有按键框（九宫格背景或描边贴图）绘制进单一 mesh
    /// </summary>
    public class KeyShapeLayer : MaskableGraphic
    {
        // Legacy CreateImage drew at 2x sizeDelta with 0.5 localScale, halving the effective 9-slice
        // border on screen. Reproduce that exactly so visuals are pixel-identical.
        // 旧 CreateImage 以 2 倍 sizeDelta + 0.5 缩放绘制，九宫格边框在屏幕上减半。精确复刻以保证视觉一致。
        private const float BorderScale = 0.5f;

        // --- Slot state (owner = background layer) / 槽位状态（持有者为背景层） ---
        private Rect[] rects;
        private Color[] bgColors;
        private Color[] outlineColors;
        private float[] scales;
        private bool[] visibles;
        private int count;

        /// <summary>State owner; null on the background layer itself / 状态持有者；背景层自身为 null</summary>
        private KeyShapeLayer owner;
        /// <summary>Outline layer sharing this layer's state (owner side) / 共享本层状态的描边层（持有方）</summary>
        private KeyShapeLayer outlineLayer;
        /// <summary>This layer draws the outline color set / 本层是否绘制描边颜色组</summary>
        private bool isOutline;

        /// <summary>Bumped on every Init so stale press-animation coroutines can detect a rebuild / 每次 Init 递增，供按压动画协程检测重建</summary>
        public int Generation { get; private set; }

        /// <summary>This layer's sprite (null = plain colored quad) / 本层贴图（null = 纯色矩形）</summary>
        public Sprite Sprite { get; set; }

        public override Texture mainTexture => Sprite != null ? Sprite.texture : base.mainTexture;

        public KeyShapeLayer()
        {
            raycastTarget = false;
        }

        /// <summary>Allocate slot arrays (owner only); all slots start hidden / 分配槽位数组（仅持有层）；所有槽位初始隐藏</summary>
        public void Init(int slotCount)
        {
            count = slotCount;
            rects = new Rect[slotCount];
            bgColors = new Color[slotCount];
            outlineColors = new Color[slotCount];
            scales = new float[slotCount];
            visibles = new bool[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                bgColors[i] = Color.white;
                outlineColors[i] = Color.white;
                scales[i] = 1f;
            }
            Generation++;
            MarkDirty();
        }

        /// <summary>Wire the outline layer to this (owner) layer's state / 将描边层接到本（持有）层的状态上</summary>
        public void AttachOutlineLayer(KeyShapeLayer outline)
        {
            outlineLayer = outline;
            outline.owner = this;
            outline.isOutline = true;
            MarkDirty();
        }

        /// <summary>Set both layers' sprites (call on the owner) / 设置两层的贴图（在持有层上调用）</summary>
        public void SetSprites(Sprite background, Sprite outline)
        {
            Sprite = background;
            if (outlineLayer != null) outlineLayer.Sprite = outline;
            // Texture changed — the material must rebind or the renderer keeps the white default
            // (Image.sprite's setter does SetAllDirty for this reason) / 贴图变了必须重绑材质，
            // 否则渲染器一直用白色默认贴图（Image.sprite 的 setter 调 SetAllDirty 的原因）
            SetVerticesDirty();
            SetMaterialDirty();
            if (outlineLayer != null)
            {
                outlineLayer.SetVerticesDirty();
                outlineLayer.SetMaterialDirty();
            }
        }

        /// <summary>Update a slot's rect without touching its visibility (repositioning must not
        /// reveal streamer-hidden KPS/Total boxes; CreateKey shows new slots explicitly) /
        /// 更新槽位矩形但不改变可见性（重新定位不得重新显示主播模式隐藏的 KPS/Total；新槽位由 CreateKey 显式显示）</summary>
        public void SetRect(int slot, float x, float y, float w, float h)
        {
            if (slot < 0 || slot >= count) return;
            rects[slot] = new Rect(x, y, w, h);
            MarkDirty();
        }

        public void SetColors(int slot, Color background, Color outline)
        {
            if (slot < 0 || slot >= count) return;
            if (bgColors[slot] == background && outlineColors[slot] == outline) return;
            bgColors[slot] = background;
            outlineColors[slot] = outline;
            MarkDirty();
        }

        public void SetScale(int slot, float scale)
        {
            if (slot < 0 || slot >= count) return;
            if (scales[slot] == scale) return;
            scales[slot] = scale;
            MarkDirty();
        }

        public void SetVisible(int slot, bool visible)
        {
            if (slot < 0 || slot >= count) return;
            visibles[slot] = visible;
            MarkDirty();
        }

        private void MarkDirty()
        {
            SetVerticesDirty();
            if (outlineLayer != null) outlineLayer.SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            KeyShapeLayer src = owner ?? this;
            if (src.rects == null) return;
            Color[] colors = isOutline ? src.outlineColors : src.bgColors;
            for (int i = 0; i < src.count; i++)
            {
                if (!src.visibles[i]) continue;
                Rect r = src.rects[i];
                if (r.width <= 0f || r.height <= 0f) continue;
                float s = src.scales[i];
                if (s != 1f)
                {
                    float cx = r.center.x, cy = r.center.y;
                    r = new Rect(cx - r.width * s * 0.5f, cy - r.height * s * 0.5f, r.width * s, r.height * s);
                }
                DrawSliced(vh, r, colors[i], Sprite);
            }
        }

        /// <summary>
        /// Draw one 9-sliced quad, matching uGUI Image.Type.Sliced geometry: border in sprite pixels,
        /// squeezed proportionally when the rect is smaller than the combined borders; UV splits use
        /// the sprite's inner UV (border fraction of the sprite, independent of on-screen size).
        /// 绘制单个九宫格，几何与 uGUI Sliced 一致：边框按贴图像素，矩形过小时等比收缩；
        /// UV 切分用贴图 innerUV（按贴图边框比例，与屏幕尺寸无关）。
        /// </summary>
        // Scratch arrays reused across DrawSliced calls: meshes rebuild on every press/release, so
        // per-call float[4] allocations would be steady GC garbage on the input hot path.
        // DrawSliced 调用间复用的暂存数组：每次按下/松开都会重建 mesh，逐调用分配 float[4] 会在输入热路径持续产生 GC 垃圾。
        private static readonly float[] scratchX = new float[4];
        private static readonly float[] scratchY = new float[4];
        private static readonly float[] scratchU = new float[4];
        private static readonly float[] scratchV = new float[4];

        private static void DrawSliced(VertexHelper vh, Rect r, Color color, Sprite sprite)
        {
            if (sprite == null)
            {
                AddQuad(vh, r.xMin, r.xMax, r.yMin, r.yMax, 0f, 1f, 0f, 1f, color);
                return;
            }
            // UV rects computed from textureRect (outer) and border (inner) — this Unity version has
            // no Sprite.outerUV/innerUV properties / UV 矩形由 textureRect（外）与 border（内）换算，
            // 该 Unity 版本无 Sprite.outerUV/innerUV 属性
            Rect tr = sprite.textureRect;
            Texture tex = sprite.texture;
            float tw = tex.width, th = tex.height;
            Rect o = new Rect(tr.x / tw, tr.y / th, tr.width / tw, tr.height / th);
            // uGUI scales the on-screen border by ppu/100 (border / multipliedPixelsPerUnit); UV
            // splits stay raw texture fractions. Inert at ppu 100 — guards non-default imports.
            // uGUI 按公式 ppu/100 缩放屏幕边框（border / multipliedPixelsPerUnit）；UV 分割仍为
            // 纹理像素比例。ppu=100 时无变化——防御非默认导入设置。
            float ppuf = sprite.pixelsPerUnit > 0f ? sprite.pixelsPerUnit / 100f : 1f;
            Vector4 spriteBorder = sprite.border * ppuf;
            if (sprite.border == Vector4.zero)
            {
                AddQuad(vh, r.xMin, r.xMax, r.yMin, r.yMax, o.xMin, o.xMax, o.yMin, o.yMax, color);
                return;
            }
            Rect n = new Rect((tr.x + spriteBorder.x) / tw, (tr.y + spriteBorder.y) / th,
                (tr.width - spriteBorder.x - spriteBorder.z) / tw, (tr.height - spriteBorder.y - spriteBorder.w) / th);
            Vector4 border = spriteBorder * BorderScale;
            if (r.width < border.x + border.z)
            {
                float k = r.width / (border.x + border.z);
                border.x *= k;
                border.z *= k;
            }
            if (r.height < border.y + border.w)
            {
                float k = r.height / (border.y + border.w);
                border.y *= k;
                border.w *= k;
            }
            float[] xs = scratchX, ys = scratchY, us = scratchU, vs = scratchV;
            xs[0] = r.xMin; xs[1] = r.xMin + border.x; xs[2] = r.xMax - border.z; xs[3] = r.xMax;
            ys[0] = r.yMin; ys[1] = r.yMin + border.y; ys[2] = r.yMax - border.w; ys[3] = r.yMax;
            us[0] = o.xMin; us[1] = n.xMin; us[2] = n.xMax; us[3] = o.xMax;
            vs[0] = o.yMin; vs[1] = n.yMin; vs[2] = n.yMax; vs[3] = o.yMax;
            for (int yi = 0; yi < 3; yi++)
            {
                for (int xi = 0; xi < 3; xi++)
                {
                    if (xs[xi + 1] - xs[xi] <= 0f || ys[yi + 1] - ys[yi] <= 0f) continue;
                    AddQuad(vh, xs[xi], xs[xi + 1], ys[yi], ys[yi + 1], us[xi], us[xi + 1], vs[yi], vs[yi + 1], color);
                }
            }
        }

        private static void AddQuad(VertexHelper vh, float x0, float x1, float y0, float y1, float u0, float u1, float v0, float v1, Color color)
        {
            int i = vh.currentVertCount;
            UIVertex vert = UIVertex.simpleVert;
            vert.color = color;
            vert.position = new Vector3(x0, y0, 0f);
            vert.uv0 = new Vector4(u0, v0, 0f, 0f);
            vh.AddVert(vert);
            vert.position = new Vector3(x1, y0, 0f);
            vert.uv0 = new Vector4(u1, v0, 0f, 0f);
            vh.AddVert(vert);
            vert.position = new Vector3(x1, y1, 0f);
            vert.uv0 = new Vector4(u1, v1, 0f, 0f);
            vh.AddVert(vert);
            vert.position = new Vector3(x0, y1, 0f);
            vert.uv0 = new Vector4(u0, v1, 0f, 0f);
            vh.AddVert(vert);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
