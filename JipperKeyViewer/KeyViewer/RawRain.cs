using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>
    /// Pooled rain-drop state: motion data updated by RainSystem, render data consumed by RainLayer.
    /// No GameObject — the merged layers read this directly every frame.
    /// 对象池雨滴状态：运动数据由 RainSystem 更新，渲染数据由 RainLayer 消费。
    /// 无 GameObject——合并图层每帧直接读取本状态。
    /// </summary>
    public class RawRain
    {
        public float elapsedMs;
        public float startY;
        public byte color;
        public Vector2 FinalSize;
        public Vector2? sizeDelta;
        public Vector2? anchoredPosition;
        public bool removed;
        public bool isGhost;
        public bool growing;

        // --- Render state / 渲染状态 ---
        /// <summary>Layer-space rect of the drop (computed each frame in RainSystem) / 雨滴在图层空间的矩形（RainSystem 每帧计算）</summary>
        public Rect rect;
        /// <summary>Drop color (rain color or ghost color) / 雨滴颜色（普通雨色或鬼雨色）</summary>
        public Color mainColor = Color.white;
        /// <summary>Fade-out alpha (1 = opaque) / 淡出透明度（1 = 不透明）</summary>
        public float alpha = 1f;
        /// <summary>Per-key press scale applied when this rect was computed (scales shadow/outline offsets at emit) / 计算此矩形时的每键按压缩放（发射时缩放阴影/描边偏移）</summary>
        public float scaleF = 1f;
        public bool fadingOut;
        public float fadeTimer;
        // Trail gradient params / 轨迹渐变参数
        public float dNear, dFar, trackHeight, fadePx;
        // Shadow / outline params (per drop, from row settings at creation) / 阴影/描边参数（创建时取自行设置）
        public bool shadowEnabled;
        public Color shadowColor;
        public float shadowOffsetX, shadowOffsetY;
        public bool outlineEnabled;
        public Color outlineColor;
        public float outlineWidth;

        public RawRain(byte color)
        {
            this.color = color;
            elapsedMs = 0f;
        }

        public bool UpdateLocation(bool updateSize, float speedFactor, float height, float deltaMs)
        {
            elapsedMs += deltaMs;
            float y = elapsedMs * speedFactor;
            float dropY = startY + y;
            if (updateSize || FinalSize == default)
                FinalSize = new Vector2(color switch
                {
                    0 => isGhost ? KeyViewer.Settings.Data.GhostRainWidthRow1 : KeyViewer.Settings.Data.RainWidthRow1,
                    3 => isGhost ? KeyViewer.Settings.Data.GhostRainWidthRow3 : KeyViewer.Settings.Data.RainWidthRow3,
                    _ => isGhost ? KeyViewer.Settings.Data.GhostRainWidthRow2 : KeyViewer.Settings.Data.RainWidthRow2
                }, y);
            if (dropY > height)
            {
                float sizeY = FinalSize.y - dropY + height;
                if (sizeY < 0) return false;
                sizeDelta = new Vector2(FinalSize.x, sizeY);
                anchoredPosition = new Vector2(0, height);
            }
            else
            {
                if (updateSize) sizeDelta = FinalSize;
                anchoredPosition = new Vector2(0, dropY);
            }
            return true;
        }
    }
}
