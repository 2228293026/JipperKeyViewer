using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
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
        public Rain rainComponent;

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
                    0 => KeyViewer.Settings.Data.RainWidthRow1,
                    3 => KeyViewer.Settings.Data.RainWidthRow3,
                    _ => KeyViewer.Settings.Data.RainWidthRow2
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
