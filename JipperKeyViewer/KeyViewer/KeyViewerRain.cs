using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JipperKeyViewer.KeyViewer
{
    public partial class KeyViewer : MonoBehaviour
    {
        private void UpdateRainEffects()
        {
            if (!Settings.EnableRainEffect) return;

            if (Stopwatch == null)
            {
                Stopwatch = System.Diagnostics.Stopwatch.StartNew();
                lastFrameMs = 0;
                return;
            }
            if (Keys == null || Keys.Length == 0) return;

            long now = Stopwatch.ElapsedMilliseconds;
            float deltaMs = lastFrameMs == 0 ? 0 : Mathf.Min(MAX_DELTA_MS, now - lastFrameMs);
            lastFrameMs = now;

            // Update pre-allocated row parameter arrays (no GC allocation)
            rowSpeeds[0] = Settings.RainSpeedRow1;
            rowSpeeds[1] = Settings.RainSpeedRow2;
            rowSpeeds[2] = Settings.RainSpeedRow3;
            rowHeights[0] = Settings.RainHeightRow1;
            rowHeights[1] = Settings.RainHeightRow2;
            rowHeights[2] = Settings.RainHeightRow3;

            for (int i = 0; i < Keys.Length; i++)
            {
                Key key = Keys[i];
                if (key == null || key.rainList.Count == 0) continue;

                int row = i < 8 ? 0 : (i < 16 ? 1 : 2);

                // Reverse iteration avoids O(n²) from repeated RemoveAt
                for (int j = key.rainList.Count - 1; j >= 0; j--)
                {
                    RawRain rain = key.rainList[j];
                    if (rain.removed) continue;

                    bool updateSize = key.isPressed && j == key.rainList.Count - 1;

                    if (!rain.UpdateLocation(deltaMs, updateSize, rowSpeeds[row], rowHeights[row]))
                    {
                        rain.removed = true;
                        key.rainList.RemoveAt(j);
                    }
                }
            }
        }

        private void TriggerRainEffect(int keyIndex, Key key)
        {
            if (!Settings.EnableRainEffect || key == null || !IsRainEnabledForKey(keyIndex))
                return;

            CreateRainDropForKey(keyIndex, key);
        }

        private void CreateRainDropForKey(int keyIndex, Key key)
        {
            if (key == null || key.rain == null) return;

            RawRain rawRain = new RawRain(key.rain.transform, key.color);
            key.rawRainQueue.Enqueue(rawRain);
            key.rainList.Add(rawRain);
        }

        private void ClearAllRainDrops()
        {
            if (Keys == null) return;
            foreach (var key in Keys)
            {
                if (key == null) continue;
                foreach (var rain in key.rainList)
                {
                    rain.removed = true;
                }
                key.rainList.Clear();
            }
        }

        private void ClearAllRains()
        {
            if (Keys == null) return;
            foreach (var key in Keys)
            {
                if (key == null) continue;
                while (key.rawRainQueue.Count > 0)
                {
                    var rain = key.rawRainQueue.Dequeue();
                    if (rain != null && rain.transform != null && rain.transform.gameObject != null)
                        Destroy(rain.transform.gameObject);
                }
                foreach (var rain in key.rainList)
                    rain.removed = true;
                key.rainList.Clear();
            }
        }

        private bool IsRainEnabledForKey(int keyIndex)
        {
            if (keyIndex < 8) return Settings.EnableRainForRow1;
            if (keyIndex < 16) return Settings.EnableRainForRow2;
            if (keyIndex < 20) return Settings.EnableRainForRow3;
            return false;
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
            return r;
        }

        public void ReturnRain(Rain r)
        {
            r.gameObject.SetActive(false);
            r.rawRain = null;
            r.transform.SetParent(null);
            rainPool.Push(r);
        }
    }
}
