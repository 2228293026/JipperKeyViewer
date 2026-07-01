using UnityEngine;
using UnityEngine.UI;

namespace JipperKeyViewer.KeyViewer
{
    public class RainGraphic : MaskableGraphic
    {
        private float dNear;
        private float dFar;
        private float trackHeight;
        private float fadePx;
        private bool reverseFade;

        public bool shadowEnabled;
        public Color shadowColor;
        public float shadowOffsetX;
        public float shadowOffsetY;

        public bool outlineEnabled;
        public Color outlineColor;
        public float outlineWidth;

        /// <summary>
        /// When false, skips the main rain quad (used by ghost rain which renders via Image).
        /// Shadow/outline quads are still drawn.
        /// </summary>
        public bool renderMain = true;

        public void SetFadeParams(float dNear, float dFar, float trackHeight, float fadePx, bool reverse)
        {
            bool changed =
                this.dNear != dNear ||
                this.dFar != dFar ||
                this.trackHeight != trackHeight ||
                this.fadePx != fadePx ||
                this.reverseFade != reverse;
            this.dNear = dNear;
            this.dFar = dFar;
            this.trackHeight = trackHeight;
            this.fadePx = fadePx;
            this.reverseFade = reverse;
            if (changed) SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            if (r.width <= 0f || r.height <= 0f) return;

            float xL = r.xMin;
            float xR = r.xMax;
            float yB = r.yMin;
            float yT = r.yMax;
            float h = r.height;

            Color baseCol = color;
            float fade = fadePx;
            float trackH = trackHeight;
            float span = dFar - dNear;

            bool simple = fade <= 0.5f || trackH <= 0.5f || span <= 0.0001f;

            // Shadow quad (behind everything)
            if (shadowEnabled)
            {
                Color sc = shadowColor;
                sc.a *= baseCol.a;
                var sr = new Rect(xL + shadowOffsetX, yB + shadowOffsetY, xR - xL, yT - yB);
                AddQuad(vh, sr, sc, sc);
            }

            // Outline quad(s) (between shadow and main)
            if (outlineEnabled)
            {
                Color oc = outlineColor;
                oc.a *= baseCol.a;
                float ow = outlineWidth;
                DrawRainQuad(vh, xL - ow, xR + ow, yB - ow, yT + ow, h + ow * 2, oc,
                    dNear, dFar, trackH, fade, reverseFade, span, simple);
            }

            // Main quad(s) — skipped for ghost rain (renderMain=false)
            if (renderMain)
                DrawRainQuad(vh, xL, xR, yB, yT, h, baseCol,
                    dNear, dFar, trackH, fade, reverseFade, span, simple);
        }

        private static void DrawRainQuad(VertexHelper vh, float xL, float xR, float yB, float yT, float h, Color col,
            float dNear, float dFar, float trackH, float fade, bool reverse, float span, bool simple)
        {
            if (simple)
            {
                AddQuad(vh, new Rect(xL, yB, xR - xL, yT - yB), col, col);
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
                var fullRect = new Rect(xL, yB, xR - xL, yT - yB);
                if (reverse)
                    AddQuad(vh, fullRect, colFar, colNear);
                else
                    AddQuad(vh, fullRect, colNear, colFar);
                return;
            }

            float t = (fadeStartD - dNear) / span;
            if (reverse)
            {
                float yMid = yT - t * h;
                AddQuad(vh, new Rect(xL, yMid, xR - xL, yT - yMid), col, colNear);
                AddQuad(vh, new Rect(xL, yB, xR - xL, yMid - yB), colFar, col);
            }
            else
            {
                float yMid = yB + t * h;
                AddQuad(vh, new Rect(xL, yB, xR - xL, yMid - yB), colNear, col);
                AddQuad(vh, new Rect(xL, yMid, xR - xL, yT - yMid), col, colFar);
            }
        }

        private static float AlphaAtD(float d, float fadeStartD, float trackH, float fade)
        {
            if (d <= fadeStartD) return 1f;
            if (d >= trackH) return 0f;
            return (trackH - d) / fade;
        }

        private static void AddQuad(VertexHelper vh, Rect r, Color bot, Color top)
        {
            int i = vh.currentVertCount;
            UIVertex v = UIVertex.simpleVert;
            v.position = new Vector3(r.xMin, r.yMin, 0f); v.color = bot; vh.AddVert(v);
            v.position = new Vector3(r.xMax, r.yMin, 0f); v.color = bot; vh.AddVert(v);
            v.position = new Vector3(r.xMax, r.yMax, 0f); v.color = top; vh.AddVert(v);
            v.position = new Vector3(r.xMin, r.yMax, 0f); v.color = top; vh.AddVert(v);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
