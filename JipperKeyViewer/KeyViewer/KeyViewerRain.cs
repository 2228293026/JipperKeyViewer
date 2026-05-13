// Rain effect system with object pooling / 带对象池的雨滴效果系统
// Manages creation, updating, and cleanup of rain drop objects / 管理雨滴对象的创建、更新和清理

using UnityEngine;
using Object = UnityEngine.Object;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>
    /// Rain effect: visual trail animation triggered on key press / 雨滴效果：按键按下时触发的拖尾动画
    /// Uses RawRain (data-only) + Rain (component from pool) pattern to minimize GC allocation / 使用 RawRain（纯数据）+ Rain（对象池组件）模式最小化 GC 分配
    /// </summary>
    public partial class KeyViewer : MonoBehaviour
    {
        /// <summary>Maximum number of Rain components kept in the pool / 对象池中保留的最大 Rain 组件数量</summary>
        const int MAX_POOL_SIZE = 30;

        /// <summary>
        /// Update all active rain drops: advance position, shrink at end of travel, remove expired / 更新所有活跃雨滴：推进位置，在行程末端缩小，移除过期雨滴
        /// Uses reverse iteration to allow safe removal from lists / 使用反向迭代以安全地从列表中移除
        /// </summary>
        private void UpdateRainEffects()
        {
            if (!Settings.EnableRainEffect) return;
            if (Keys == null || Keys.Length == 0) return;

            // Recompute cached row values only when settings change / 仅当设置变更时重算缓存的行值
            if (cachedRainSpeed1 != Settings.RainSpeedRow1 || cachedRainSpeed2 != Settings.RainSpeedRow2 ||
                cachedRainSpeed3 != Settings.RainSpeedRow3 || cachedRainHeight1 != Settings.RainHeightRow1 ||
                cachedRainHeight2 != Settings.RainHeightRow2 || cachedRainHeight3 != Settings.RainHeightRow3)
            {
                rowSpeeds[0] = Settings.RainSpeedRow1 / 300f;
                rowSpeeds[1] = Settings.RainSpeedRow2 / 300f;
                rowSpeeds[2] = Settings.RainSpeedRow3 / 300f;
                rowHeights[0] = Settings.RainHeightRow1;
                rowHeights[1] = Settings.RainHeightRow2;
                rowHeights[2] = Settings.RainHeightRow3;
                cachedRainSpeed1 = Settings.RainSpeedRow1;
                cachedRainSpeed2 = Settings.RainSpeedRow2;
                cachedRainSpeed3 = Settings.RainSpeedRow3;
                cachedRainHeight1 = Settings.RainHeightRow1;
                cachedRainHeight2 = Settings.RainHeightRow2;
                cachedRainHeight3 = Settings.RainHeightRow3;
            }

            bool enableFade = Settings.EnableRainFade;
            double now = Time.unscaledTimeAsDouble;

            for (int i = 0; i < rainActiveKeys.Count; i++)
            {
                int ki = rainActiveKeys[i];
                Key key = Keys[ki];
                if (key == null) { rainActiveKeys.RemoveAt(i--); continue; }
                if (key.rainList.Count == 0) { rainActiveKeys.RemoveAt(i--); continue; }

                int row = ki < 8 ? 0 : (ki < 16 ? 1 : 2);

                // Reverse iteration avoids O(n²) from repeated RemoveAt / 反向迭代避免重复 RemoveAt 导致的 O(n²)
                for (int j = key.rainList.Count - 1; j >= 0; j--)
                {
                    RawRain rain = key.rainList[j];
                    if (rain.removed) continue;

                    // Only update size for the newest drop when key is held (gives continuous trail effect) / 仅在按键按住时更新最新雨滴的大小（产生连续拖尾效果）
                    bool updateSize = key.isPressed && j == key.rainList.Count - 1;

                    float oldAlpha = rain.alpha;
                    if (!rain.UpdateLocation(updateSize, rowSpeeds[row], rowHeights[row], enableFade, now))
                    {
                        rain.removed = true;
                        key.rainList.RemoveAt(j);
                        continue;
                    }

                    // Sync Rain visual component immediately / 立即同步 Rain 可视化组件
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

        /// <summary>
        /// Trigger a rain drop on key press / 按键按下时触发一个雨滴
        /// Checks if the rain effect is enabled for the corresponding row / 检查对应排的雨滴效果是否启用
        /// </summary>
        private void TriggerRainEffect(int keyIndex, Key key)
        {
            if (!Settings.EnableRainEffect || key == null || !IsRainEnabledForKey(keyIndex))
                return;

            CreateRainDropForKey(keyIndex, key);
        }

        /// <summary>
        /// Get a RawRain data object from pool, or create new / 从对象池获取 RawRain 数据对象，或新建
        /// </summary>
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

        /// <summary>
        /// Return a RawRain data object to the pool / 将 RawRain 数据对象归还对象池
        /// </summary>
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

        /// <summary>
        /// Create a RawRain data entry and enqueue it for Rain component assignment / 创建 RawRain 数据条目并排队等待 Rain 组件分配
        /// </summary>
        private void CreateRainDropForKey(int keyIndex, Key key)
        {
            if (key == null || key.rain == null) return;

            RawRain rawRain = GetRawRain(key.rain.transform, key.color);
            key.rawRainQueue.Enqueue(rawRain);
            key.rainList.Add(rawRain);
            if (key.rainList.Count == 1)
                rainActiveKeys.Add(keyIndex);
        }

        /// <summary>
        /// Mark all rain drops as removed (called on scene load) / 标记所有雨滴为已移除（场景加载时调用）
        /// </summary>
        private void ClearAllRainDrops()
        {
            if (Keys == null) return;
            rainActiveKeys.Clear();
            foreach (var key in Keys)
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

        /// <summary>
        /// Force-clear all rains including pooled objects / 强制清除所有雨滴，包括池中对象
        /// </summary>
        private void ClearAllRains()
        {
            ClearAllRainDrops();
            rainActiveKeys.Clear();
            while (rainPool.Count > 0)
                Object.Destroy(rainPool.Pop().gameObject);
        }

        /// <summary>Check if rain effect is enabled for the given key's row / 检查指定按键所在排的雨滴效果是否启用</summary>
        private bool IsRainEnabledForKey(int keyIndex)
        {
            if (keyIndex < 8) return Settings.EnableRainForRow1;
            if (keyIndex < 16) return Settings.EnableRainForRow2;
            if (keyIndex < 20) return Settings.EnableRainForRow3;
            return false;
        }

        /// <summary>
        /// Get a Rain component from the object pool, or create one if the pool is empty / 从对象池获取 Rain 组件，如果池为空则创建新的
        /// </summary>
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

        /// <summary>
        /// Return a Rain component to the pool for reuse / 将 Rain 组件归还到对象池以便复用
        /// Destroys the object if the pool is full / 如果池已满则销毁对象
        /// </summary>
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
    }
}
