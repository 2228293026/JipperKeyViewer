using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JipperKeyViewer.KeyViewer
{
    public class RainSystem
    {
        private readonly KeyViewerSettings settings;

        private readonly Stack<Rain> rainPool = new Stack<Rain>();
        private readonly Stack<RawRain> rawRainPool = new Stack<RawRain>();
        private readonly List<int> rainActiveKeys = new List<int>();
        private readonly HashSet<int> rainActiveSet = new HashSet<int>();

        private const int MAX_RAWRAIN_POOL_SIZE = 60;
        private const int MAX_POOL_SIZE = 30;

        public Sprite GhostRainSprite { get; set; }

        private readonly float[] rowSpeeds = new float[3];
        private readonly float[] rowHeights = new float[3];
        private readonly float[] ghostRowSpeeds = new float[3];
        private readonly float[] ghostRowHeights = new float[3];
        private float cachedRainSpeed1, cachedRainSpeed2, cachedRainSpeed3;
        private float cachedRainHeight1, cachedRainHeight2, cachedRainHeight3;
        private float cachedGhostSpeed1, cachedGhostSpeed2, cachedGhostSpeed3;
        private float cachedGhostHeight1, cachedGhostHeight2, cachedGhostHeight3;

        public RainSystem(KeyViewerSettings settings)
        {
            this.settings = settings;
        }

        public void UpdateEffects(Key[] keys)
        {
            if (keys == null || keys.Length == 0) return;
            if (rainActiveKeys.Count == 0) return;

            SyncCachedSpeeds();
            float dtSec = Time.unscaledDeltaTime;

            for (int i = 0; i < rainActiveKeys.Count; i++)
            {
                int ki = rainActiveKeys[i];
                Key key = keys[ki];
                if (key == null || key.rainList.Count == 0)
                {
                    rainActiveSet.Remove(ki);
                    rainActiveKeys[i] = rainActiveKeys[rainActiveKeys.Count - 1];
                    rainActiveKeys.RemoveAt(rainActiveKeys.Count - 1);
                    i--;
                    continue;
                }

                int row = ki < 8 ? 0 : (ki < 16 ? 1 : 2);
                for (int j = key.rainList.Count - 1; j >= 0; j--)
                    UpdateSingleRainDrop(key.rainList[j], key, j, row, dtSec);
            }
        }

        private void SyncCachedSpeeds()
        {
            if (cachedRainSpeed1 == settings.Data.RainSpeedRow1 && cachedRainSpeed2 == settings.Data.RainSpeedRow2 &&
                cachedRainSpeed3 == settings.Data.RainSpeedRow3 && cachedRainHeight1 == settings.Data.RainHeightRow1 &&
                cachedRainHeight2 == settings.Data.RainHeightRow2 && cachedRainHeight3 == settings.Data.RainHeightRow3 &&
                cachedGhostSpeed1 == settings.Data.GhostRainSpeedRow1 && cachedGhostSpeed2 == settings.Data.GhostRainSpeedRow2 &&
                cachedGhostSpeed3 == settings.Data.GhostRainSpeedRow3 && cachedGhostHeight1 == settings.Data.GhostRainHeightRow1 &&
                cachedGhostHeight2 == settings.Data.GhostRainHeightRow2 && cachedGhostHeight3 == settings.Data.GhostRainHeightRow3)
                return;
            rowSpeeds[0] = settings.Data.RainSpeedRow1 / 300f;
            rowSpeeds[1] = settings.Data.RainSpeedRow2 / 300f;
            rowSpeeds[2] = settings.Data.RainSpeedRow3 / 300f;
            rowHeights[0] = settings.Data.RainHeightRow1;
            rowHeights[1] = settings.Data.RainHeightRow2;
            rowHeights[2] = settings.Data.RainHeightRow3;
            ghostRowSpeeds[0] = settings.Data.GhostRainSpeedRow1 / 300f;
            ghostRowSpeeds[1] = settings.Data.GhostRainSpeedRow2 / 300f;
            ghostRowSpeeds[2] = settings.Data.GhostRainSpeedRow3 / 300f;
            ghostRowHeights[0] = settings.Data.GhostRainHeightRow1;
            ghostRowHeights[1] = settings.Data.GhostRainHeightRow2;
            ghostRowHeights[2] = settings.Data.GhostRainHeightRow3;
            cachedRainSpeed1 = settings.Data.RainSpeedRow1;
            cachedRainSpeed2 = settings.Data.RainSpeedRow2;
            cachedRainSpeed3 = settings.Data.RainSpeedRow3;
            cachedRainHeight1 = settings.Data.RainHeightRow1;
            cachedRainHeight2 = settings.Data.RainHeightRow2;
            cachedRainHeight3 = settings.Data.RainHeightRow3;
            cachedGhostSpeed1 = settings.Data.GhostRainSpeedRow1;
            cachedGhostSpeed2 = settings.Data.GhostRainSpeedRow2;
            cachedGhostSpeed3 = settings.Data.GhostRainSpeedRow3;
            cachedGhostHeight1 = settings.Data.GhostRainHeightRow1;
            cachedGhostHeight2 = settings.Data.GhostRainHeightRow2;
            cachedGhostHeight3 = settings.Data.GhostRainHeightRow3;
        }

        private void UpdateSingleRainDrop(RawRain rain, Key key, int j, int row, float dtSec)
        {
            if (rain.removed) return;

            float speed = rain.isGhost ? ghostRowSpeeds[row] : rowSpeeds[row];
            float height = rain.isGhost ? ghostRowHeights[row] : rowHeights[row];
            float dt = dtSec * 1000f;
            if (!rain.UpdateLocation(rain.growing, speed, height, dt))
            {
                ReturnRainAndRawRain(rain, key, j);
                return;
            }

            Rain r = rain.rainComponent;
            if (r == null) return;

            ApplyRainTransforms(rain, r);
            UpdateFadeOut(r, dtSec, rain, key, j);
            UpdateTrailEdge(rain, r, speed, height);
        }

        private static void ApplyRainTransforms(RawRain rain, Rain r)
        {
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
        }

        private void UpdateFadeOut(Rain r, float dtSec, RawRain rain, Key key, int j)
        {
            if (!r.fadingOut) return;
            r.fadeTimer += dtSec;
            float t = Mathf.Clamp01(r.fadeTimer / settings.Data.RainFadeDuration);
            float a = 1f - (t * (2f - t));
            var c = r.graphic.color;
            c.a = a;
            r.graphic.color = c;
            if (t >= 1f)
                ReturnRainAndRawRain(rain, key, j);
        }

        private void UpdateTrailEdge(RawRain rain, Rain r, float speed, float height)
        {
            float trailEdgeDist = rain.elapsedMs * speed;
            float drawH = trailEdgeDist > height
                ? rain.FinalSize.y - trailEdgeDist + height
                : (rain.growing ? trailEdgeDist : rain.FinalSize.y);
            trailEdgeDist = Mathf.Min(trailEdgeDist, height);

            float dFar = trailEdgeDist;
            float dNear = dFar - drawH;
            float fp = settings.Data.EnableRainGradient && !rain.isGhost ? settings.Data.RainFadePx : 0f;
            r.graphic.SetFadeParams(dNear, dFar, height, fp, false);
        }

        private void ReturnRainAndRawRain(RawRain rain, Key key, int listIndex)
        {
            rain.removed = true;
            if (rain.rainComponent != null)
            {
                ReturnRain(rain.rainComponent);
                rain.rainComponent = null;
            }
            ReturnRawRain(rain);
            key.rainList.RemoveAt(listIndex);
        }

        public void TriggerRainEffect(int keyIndex, Key key)
        {
            if (key == null || !IsRainEnabledForKey(keyIndex))
                return;
            CreateRainDropForKey(keyIndex, key);
        }

        public void ReleaseRainEffect(int keyIndex, Key key)
        {
            if (key == null || key.rainList.Count == 0) return;
            for (int i = key.rainList.Count - 1; i >= 0; i--)
            {
                if (key.rainList[i].isGhost) continue;
                key.rainList[i].growing = false;
                if (settings.Data.EnableRainFade && key.rainList[i].rainComponent != null)
                    key.rainList[i].rainComponent.StartFadeOut(settings.Data.RainFadeDuration);
                break;
            }
        }

        public void TriggerGhostRain(int keyIndex, Key key)
        {
            if (key == null || !IsRainEnabledForKey(keyIndex)) return;
            CreateRainDropForKey(keyIndex, key, isGhost: true);
        }

        public void ReleaseGhostRain(int keyIndex, Key key)
        {
            if (key == null || key.rainList.Count == 0) return;
            for (int i = key.rainList.Count - 1; i >= 0; i--)
            {
                if (key.rainList[i].isGhost)
                {
                    key.rainList[i].growing = false;
                    break;
                }
            }
        }

        public void ClearActiveDrops(Key[] keys)
        {
            if (keys == null) return;
            rainActiveKeys.Clear();
            rainActiveSet.Clear();
            foreach (var key in keys)
            {
                if (key == null) continue;
                foreach (var rain in key.rainList)
                {
                    if (rain.rainComponent != null)
                    {
                        ReturnRain(rain.rainComponent);
                        rain.rainComponent = null;
                    }
                    ReturnRawRain(rain);
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

        public Color GetRainColor(byte color) => RainColor(color, false);

        public Color GetGhostRainColor(byte color) => RainColor(color, true);

        private Color RainColor(byte color, bool ghost)
        {
            return color switch
            {
                0 => ghost ? settings.Data.GhostRainColor : settings.Data.RainColor,
                3 => ghost ? settings.Data.GhostRainColor3 : settings.Data.RainColor3,
                _ => ghost ? settings.Data.GhostRainColor2 : settings.Data.RainColor2
            };
        }

        public Rain GetRainFromPool(Transform parent)
        {
            return GetRainFromPool(parent, null, false);
        }

        public Rain GetRainFromPool(Transform parent, Sprite sprite, bool isTiled)
        {
            Rain r;
            if (rainPool.Count > 0)
            {
                r = rainPool.Pop();
            }
            else
            {
                GameObject go = new GameObject("Rain");
                go.AddComponent<RectTransform>();
                r = go.AddComponent<Rain>();
            }
            if (sprite == null)
                r.Init(parent);
            else
                r.Init(parent, sprite, isTiled);
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

        private RawRain GetRawRain(byte color)
        {
            RawRain r;
            if (rawRainPool.Count > 0)
            {
                r = rawRainPool.Pop();
                r.color = color;
                r.removed = false;
                r.elapsedMs = 0f;
                r.startY = 0f;
                r.sizeDelta = null;
                r.anchoredPosition = null;
                r.rainComponent = null;
                r.isGhost = false;
                r.growing = false;
                r.FinalSize = default;
            }
            else
            {
                r = new RawRain(color);
            }
            return r;
        }

        public void ReturnRawRain(RawRain r)
        {
            if (rawRainPool.Count >= MAX_RAWRAIN_POOL_SIZE) return;
            r.removed = false;
            r.sizeDelta = null;
            r.anchoredPosition = null;
            r.rainComponent = null;
            r.isGhost = false;
            r.growing = false;
            rawRainPool.Push(r);
        }

        private void CreateRainDropForKey(int keyIndex, Key key, bool isGhost = false)
        {
            if (key == null || key.rain == null) return;

            int row = keyIndex < 8 ? 1 : (keyIndex < 16 ? 2 : 3);

            RawRain rawRain = GetRawRain(key.color);
            float baseY = row == 1 ? settings.Data.RainStartYRow1 : row == 2 ? settings.Data.RainStartYRow2 : settings.Data.RainStartYRow3;
            rawRain.startY = isGhost
                ? (row == 1 ? settings.Data.GhostRainStartYRow1 : row == 2 ? settings.Data.GhostRainStartYRow2 : settings.Data.GhostRainStartYRow3) - baseY
                : 0f;
            Rain rainComponent;
            if (isGhost)
            {
                rainComponent = GetRainFromPool(key.rain.transform, GhostRainSprite, true);
                rainComponent.ghostImage.color = settings.Data.EnablePerKeyColors
                    ? settings.Data.PerKeyGhostRainColor[keyIndex]
                    : GetGhostRainColor(key.color);
            }
            else
            {
                rainComponent = GetRainFromPool(key.rain.transform);
                rainComponent.graphic.color = key.rainColor;
            }

            // Ghost-specific or normal-specific shadow/outline settings
            if (isGhost)
            {
                bool hasShadow = row == 1 ? settings.Data.EnableGhostRainShadowRow1
                    : row == 2 ? settings.Data.EnableGhostRainShadowRow2
                    : settings.Data.EnableGhostRainShadowRow3;
                bool hasOutline = row == 1 ? settings.Data.EnableGhostRainOutlineRow1
                    : row == 2 ? settings.Data.EnableGhostRainOutlineRow2
                    : settings.Data.EnableGhostRainOutlineRow3;
                rainComponent.graphic.enabled = hasShadow || hasOutline;
                rainComponent.graphic.shadowEnabled = hasShadow;
                rainComponent.graphic.shadowColor = row == 1 ? settings.Data.GhostRainShadowColorRow1
                    : row == 2 ? settings.Data.GhostRainShadowColorRow2
                    : settings.Data.GhostRainShadowColorRow3;
                rainComponent.graphic.shadowOffsetX = row == 1 ? settings.Data.GhostRainShadowOffsetXRow1
                    : row == 2 ? settings.Data.GhostRainShadowOffsetXRow2
                    : settings.Data.GhostRainShadowOffsetXRow3;
                rainComponent.graphic.shadowOffsetY = row == 1 ? settings.Data.GhostRainShadowOffsetYRow1
                    : row == 2 ? settings.Data.GhostRainShadowOffsetYRow2
                    : settings.Data.GhostRainShadowOffsetYRow3;

                rainComponent.graphic.outlineEnabled = hasOutline;
                rainComponent.graphic.outlineColor = row == 1 ? settings.Data.GhostRainOutlineColorRow1
                    : row == 2 ? settings.Data.GhostRainOutlineColorRow2
                    : settings.Data.GhostRainOutlineColorRow3;
                rainComponent.graphic.outlineWidth = row == 1 ? settings.Data.GhostRainOutlineWidthRow1
                    : row == 2 ? settings.Data.GhostRainOutlineWidthRow2
                    : settings.Data.GhostRainOutlineWidthRow3;
            }
            else
            {
                rainComponent.graphic.shadowEnabled = row == 1 ? settings.Data.EnableRainShadowRow1
                    : row == 2 ? settings.Data.EnableRainShadowRow2
                    : settings.Data.EnableRainShadowRow3;
                rainComponent.graphic.shadowColor = row == 1 ? settings.Data.RainShadowColorRow1
                    : row == 2 ? settings.Data.RainShadowColorRow2
                    : settings.Data.RainShadowColorRow3;
                rainComponent.graphic.shadowOffsetX = row == 1 ? settings.Data.RainShadowOffsetXRow1
                    : row == 2 ? settings.Data.RainShadowOffsetXRow2
                    : settings.Data.RainShadowOffsetXRow3;
                rainComponent.graphic.shadowOffsetY = row == 1 ? settings.Data.RainShadowOffsetYRow1
                    : row == 2 ? settings.Data.RainShadowOffsetYRow2
                    : settings.Data.RainShadowOffsetYRow3;

                rainComponent.graphic.outlineEnabled = row == 1 ? settings.Data.EnableRainOutlineRow1
                    : row == 2 ? settings.Data.EnableRainOutlineRow2
                    : settings.Data.EnableRainOutlineRow3;
                rainComponent.graphic.outlineColor = row == 1 ? settings.Data.RainOutlineColorRow1
                    : row == 2 ? settings.Data.RainOutlineColorRow2
                    : settings.Data.RainOutlineColorRow3;
                rainComponent.graphic.outlineWidth = row == 1 ? settings.Data.RainOutlineWidthRow1
                    : row == 2 ? settings.Data.RainOutlineWidthRow2
                    : settings.Data.RainOutlineWidthRow3;
            }

            // Set render order: front row behind, middle row in front, third row on top
            // (1,3,5 for ghost rain so ghost is always above normal within the same row)
            rainComponent.transform.SetSiblingIndex((row - 1) * 2 + (isGhost ? 1 : 0));

            rainComponent.rawRain = rawRain;
            rawRain.rainComponent = rainComponent;
            rawRain.isGhost = isGhost;
            rawRain.growing = true;

            key.rainList.Add(rawRain);

            if (!rainActiveSet.Contains(keyIndex))
            {
                rainActiveSet.Add(keyIndex);
                rainActiveKeys.Add(keyIndex);
            }
        }

        private bool IsRainEnabledForKey(int keyIndex)
        {
            if (keyIndex < 8) return settings.Data.EnableRainForRow1;
            if (keyIndex < 16) return settings.Data.EnableRainForRow2;
            if (keyIndex < KeyViewer.FootKeyBase) return settings.Data.EnableRainForRow3;
            return false;
        }
    }
}
