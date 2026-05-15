using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JipperKeyViewer.KeyViewer
{
    public class RainSystem
    {
        private readonly IRainSettings settings;

        private readonly Stack<Rain> rainPool = new Stack<Rain>();
        private readonly Stack<RawRain> rawRainPool = new Stack<RawRain>();
        private readonly List<int> rainActiveKeys = new List<int>();

        private const int MAX_RAWRAIN_POOL_SIZE = 60;
        private const int MAX_POOL_SIZE = 30;

        private readonly float[] rowSpeeds = new float[3];
        private readonly float[] rowHeights = new float[3];
        private float cachedRainSpeed1, cachedRainSpeed2, cachedRainSpeed3;
        private float cachedRainHeight1, cachedRainHeight2, cachedRainHeight3;

        public RainSystem(IRainSettings settings)
        {
            this.settings = settings;
        }

        public void ProcessKeyQueues(Key[] keys)
        {
            if (keys == null) return;
            for (int i = 0; i < keys.Length; i++)
            {
                Key key = keys[i];
                if (key != null) key.ProcessRainQueue();
            }
        }

        public void UpdateEffects(Key[] keys)
        {
            if (keys == null || keys.Length == 0) return;

            if (cachedRainSpeed1 != settings.RainSpeedRow1 || cachedRainSpeed2 != settings.RainSpeedRow2 ||
                cachedRainSpeed3 != settings.RainSpeedRow3 || cachedRainHeight1 != settings.RainHeightRow1 ||
                cachedRainHeight2 != settings.RainHeightRow2 || cachedRainHeight3 != settings.RainHeightRow3)
            {
                rowSpeeds[0] = settings.RainSpeedRow1 / 300f;
                rowSpeeds[1] = settings.RainSpeedRow2 / 300f;
                rowSpeeds[2] = settings.RainSpeedRow3 / 300f;
                rowHeights[0] = settings.RainHeightRow1;
                rowHeights[1] = settings.RainHeightRow2;
                rowHeights[2] = settings.RainHeightRow3;
                cachedRainSpeed1 = settings.RainSpeedRow1;
                cachedRainSpeed2 = settings.RainSpeedRow2;
                cachedRainSpeed3 = settings.RainSpeedRow3;
                cachedRainHeight1 = settings.RainHeightRow1;
                cachedRainHeight2 = settings.RainHeightRow2;
                cachedRainHeight3 = settings.RainHeightRow3;
            }

            bool enableFade = settings.EnableRainFade;
            double now = Time.unscaledTimeAsDouble;

            for (int i = 0; i < rainActiveKeys.Count; i++)
            {
                int ki = rainActiveKeys[i];
                Key key = keys[ki];
                if (key == null) { rainActiveKeys.RemoveAt(i--); continue; }
                if (key.rainList.Count == 0) { rainActiveKeys.RemoveAt(i--); continue; }

                int row = ki < 8 ? 0 : (ki < 16 ? 1 : 2);

                for (int j = key.rainList.Count - 1; j >= 0; j--)
                {
                    RawRain rain = key.rainList[j];
                    if (rain.removed) continue;

                    bool updateSize = key.isPressed && j == key.rainList.Count - 1;

                    float oldAlpha = rain.alpha;
                    if (!rain.UpdateLocation(updateSize, rowSpeeds[row], rowHeights[row], enableFade, now))
                    {
                        rain.removed = true;
                        key.rainList.RemoveAt(j);
                        continue;
                    }

                    Rain r = rain.rainComponent;
                    if (r == null) continue;

                    if (rain.sizeDelta != null)
                    {
                        r.transform.sizeDelta = rain.sizeDelta.Value;
                        rain.sizeDelta = null;
                    }
                    if (rain.anchoredPosition != null)
                    {
                        r.transform.anchoredPosition = rain.anchoredPosition.Value;
                        rain.anchoredPosition = null;
                    }
                    if (enableFade && rain.alpha != oldAlpha)
                    {
                        var c = r.image.color;
                        c.a = rain.alpha;
                        r.image.color = c;
                    }
                }
            }
        }

        public void TriggerRainEffect(int keyIndex, Key key)
        {
            if (key == null || !IsRainEnabledForKey(keyIndex))
                return;

            CreateRainDropForKey(keyIndex, key);
        }

        public void ClearActiveDrops(Key[] keys)
        {
            if (keys == null) return;
            rainActiveKeys.Clear();
            foreach (var key in keys)
            {
                if (key == null) continue;
                while (key.rawRainQueue.Count > 0)
                {
                    var rawRain = key.rawRainQueue.Dequeue();
                    ReturnRawRain(rawRain);
                }
                foreach (var rain in key.rainList)
                {
                    rain.removed = true;
                }
                key.rainList.Clear();
            }
        }

        public void ClearAll(Key[] keys)
        {
            ClearActiveDrops(keys);
            ClearPool();
        }

        public void ClearPool()
        {
            while (rainPool.Count > 0)
                Object.Destroy(rainPool.Pop().gameObject);
        }

        public Color GetRainColor(byte color)
        {
            return color switch
            {
                0 => settings.RainColor,
                3 => settings.RainColor3,
                _ => settings.RainColor2
            };
        }

        public Rain GetRainFromPool(Transform parent)
        {
            Rain r;
            if (rainPool.Count > 0)
            {
                r = rainPool.Pop();
                r.Init(parent);
            }
            else
            {
                GameObject go = new GameObject("Rain");
                go.AddComponent<RectTransform>();
                r = go.AddComponent<Rain>();
                r.Init(parent);
            }
            r.rainSystem = this;
            return r;
        }

        public void ReturnRain(Rain r)
        {
            r.gameObject.SetActive(false);
            r.rawRain = null;
            r.transform.SetParent(null);
            if (rainPool.Count < MAX_POOL_SIZE)
                rainPool.Push(r);
            else
                Object.Destroy(r.gameObject);
        }

        private RawRain GetRawRain(Transform transform, byte color)
        {
            RawRain r;
            if (rawRainPool.Count > 0)
            {
                r = rawRainPool.Pop();
                r.transform = transform;
                r.color = color;
                r.startTime = Time.unscaledTimeAsDouble;
                r.removed = false;
                r.alpha = 1f;
                r.fadeTimer = -1f;
                r.sizeDelta = null;
                r.anchoredPosition = null;
                r.rainComponent = null;
                r.FinalSize = default;
            }
            else
            {
                r = new RawRain(transform, color);
            }
            return r;
        }

        public void ReturnRawRain(RawRain r)
        {
            if (rawRainPool.Count >= MAX_RAWRAIN_POOL_SIZE) return;
            r.transform = null;
            r.removed = false;
            r.sizeDelta = null;
            r.anchoredPosition = null;
            r.rainComponent = null;
            rawRainPool.Push(r);
        }

        private void CreateRainDropForKey(int keyIndex, Key key)
        {
            if (key == null || key.rain == null) return;

            RawRain rawRain = GetRawRain(key.rain.transform, key.color);
            key.rawRainQueue.Enqueue(rawRain);
            key.rainList.Add(rawRain);
            if (key.rainList.Count == 1)
                rainActiveKeys.Add(keyIndex);
        }

        private bool IsRainEnabledForKey(int keyIndex)
        {
            if (keyIndex < 8) return settings.EnableRainForRow1;
            if (keyIndex < 16) return settings.EnableRainForRow2;
            if (keyIndex < 20) return settings.EnableRainForRow3;
            return false;
        }
    }

    internal class RainSettings : IRainSettings
    {
        private readonly KeyViewerSettings s;
        public RainSettings(KeyViewerSettings s) { this.s = s; }

        public Color RainColor => s.RainColor;
        public Color RainColor2 => s.RainColor2;
        public Color RainColor3 => s.RainColor3;
        public bool EnableRainForRow1 => s.EnableRainForRow1;
        public bool EnableRainForRow2 => s.EnableRainForRow2;
        public bool EnableRainForRow3 => s.EnableRainForRow3;
        public float RainSpeedRow1 => s.RainSpeedRow1;
        public float RainSpeedRow2 => s.RainSpeedRow2;
        public float RainSpeedRow3 => s.RainSpeedRow3;
        public float RainHeightRow1 => s.RainHeightRow1;
        public float RainHeightRow2 => s.RainHeightRow2;
        public float RainHeightRow3 => s.RainHeightRow3;
        public bool EnableRainFade => s.EnableRainFade;
    }
}
