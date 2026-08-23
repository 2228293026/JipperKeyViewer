using System.Collections.Generic;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>
    /// Rain effect state machine. Drops are pooled RawRain records; rendering happens in the merged
    /// RainLayer / GhostRainLayer meshes that read this state every frame — no per-drop GameObjects.
    /// 雨滴效果状态机。雨滴为对象池 RawRain 记录；渲染由每帧读取本状态的合并 RainLayer /
    /// GhostRainLayer mesh 完成——无逐雨滴 GameObject。
    /// </summary>
    public class RainSystem
    {
        private readonly KeyViewerSettings settings;

        /// <summary>Current key array (kept in sync by UpdateEffects) for the render layers / 当前键数组（由 UpdateEffects 保持同步），供渲染层读取</summary>
        internal Key[] Keys;
        /// <summary>Merged rain render layers / 合并雨滴渲染层</summary>
        internal RainLayer Layer;
        internal GhostRainLayer GhostLayer;

        private readonly Stack<RawRain> rawRainPool = new Stack<RawRain>();
        private readonly List<int> rainActiveKeys = new List<int>();
        private readonly HashSet<int> rainActiveSet = new HashSet<int>();

        private const int MAX_RAWRAIN_POOL_SIZE = 60;
        /// <summary>Height of the old per-key rain container; drops measured their top from it / 旧每键雨滴容器的高度；雨滴顶边以此为基准</summary>
        private const float RainContainerHeight = 275f;

        private readonly float[] rowSpeeds = new float[3];
        private readonly float[] rowHeights = new float[3];
        private readonly float[] ghostRowSpeeds = new float[3];
        private readonly float[] ghostRowHeights = new float[3];
        private readonly float[] rowStartYs = new float[3];
        private readonly float[] ghostRowStartYs = new float[3];
        private float cachedRainSpeed1, cachedRainSpeed2, cachedRainSpeed3;
        private float cachedRainHeight1, cachedRainHeight2, cachedRainHeight3;
        private float cachedGhostSpeed1, cachedGhostSpeed2, cachedGhostSpeed3;
        private float cachedGhostHeight1, cachedGhostHeight2, cachedGhostHeight3;
        private float cachedStartY1, cachedStartY2, cachedStartY3;
        private float cachedGhostStartY1, cachedGhostStartY2, cachedGhostStartY3;

        public RainSystem(KeyViewerSettings settings)
        {
            this.settings = settings;
        }

        public void AttachLayers(RainLayer layer, GhostRainLayer ghostLayer)
        {
            Layer = layer;
            GhostLayer = ghostLayer;
            if (layer != null) layer.System = this;
            if (ghostLayer != null) ghostLayer.System = this;
        }

        public void DetachLayers()
        {
            Layer = null;
            GhostLayer = null;
            Keys = null;
        }

        public void UpdateEffects(Key[] keys)
        {
            if (keys == null || keys.Length == 0)
            {
                Keys = keys;
                return;
            }
            Keys = keys;
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

                RectTransform keyRt = (RectTransform)key.transform;
                Vector2 keyPos = keyRt.anchoredPosition;
                int row = ki < 8 ? 0 : (ki < 16 ? 1 : 2);
                for (int j = key.rainList.Count - 1; j >= 0; j--)
                    UpdateSingleRainDrop(key.rainList[j], key, ki, keyPos, j, row, dtSec);
            }

            // One mesh rebuild per frame while anything is active / 只要有活跃雨滴，每帧重建一次 mesh
            if (Layer != null) Layer.MarkDirty();
        }

        private void SyncCachedSpeeds()
        {
            if (cachedRainSpeed1 == settings.Data.RainSpeedRow1 && cachedRainSpeed2 == settings.Data.RainSpeedRow2 &&
                cachedRainSpeed3 == settings.Data.RainSpeedRow3 && cachedRainHeight1 == settings.Data.RainHeightRow1 &&
                cachedRainHeight2 == settings.Data.RainHeightRow2 && cachedRainHeight3 == settings.Data.RainHeightRow3 &&
                cachedGhostSpeed1 == settings.Data.GhostRainSpeedRow1 && cachedGhostSpeed2 == settings.Data.GhostRainSpeedRow2 &&
                cachedGhostSpeed3 == settings.Data.GhostRainSpeedRow3 && cachedGhostHeight1 == settings.Data.GhostRainHeightRow1 &&
                cachedGhostHeight2 == settings.Data.GhostRainHeightRow2 && cachedGhostHeight3 == settings.Data.GhostRainHeightRow3 &&
                cachedStartY1 == settings.Data.RainStartYRow1 && cachedStartY2 == settings.Data.RainStartYRow2 &&
                cachedStartY3 == settings.Data.RainStartYRow3 && cachedGhostStartY1 == settings.Data.GhostRainStartYRow1 &&
                cachedGhostStartY2 == settings.Data.GhostRainStartYRow2 && cachedGhostStartY3 == settings.Data.GhostRainStartYRow3)
                return;
            // Floor speeds and heights: typed values are stored unclamped, and a zero/negative
            // speed never lifts the drop past the track top (y stays <= height forever) — with
            // fade disabled the drop is then NEVER recycled and rainList grows without bound
            // (invisible but a steady per-frame/memory leak). A tiny positive speed keeps the
            // "nearly frozen" look while guaranteeing eventual off-track recycling; heights are
            // floored at 1 so the off-track branch math stays well-defined. The floor is 1px/s
            // (1e-3 px/ms): a zero-speed drop recycles within minutes at typical heights instead
            // of the hours a smaller floor implied.
            // 速度与高度取下限:键入值不钳制存储,零/负速度永远无法把雨滴抬出轨道顶
            //(y 恒 <= height)——若淡出关闭,该雨滴永不回收,rainList 无界增长(不可见但
            // 内存与逐帧遍历成本持续泄漏)。极小正速度保留"近乎冻结"的观感,同时保证最终
            // 落出轨道被回收;高度下限 1 保证离轨分支数学良定义。下限取 1px/s(1e-3 px/ms):
            // 零速雨滴在典型高度下数分钟内回收,而不是更小下限意味着的小时级。
            const float minSpeedFactor = 1e-3f;
            rowSpeeds[0] = Mathf.Max(settings.Data.RainSpeedRow1 / 300f, minSpeedFactor);
            rowSpeeds[1] = Mathf.Max(settings.Data.RainSpeedRow2 / 300f, minSpeedFactor);
            rowSpeeds[2] = Mathf.Max(settings.Data.RainSpeedRow3 / 300f, minSpeedFactor);
            rowHeights[0] = Mathf.Max(settings.Data.RainHeightRow1, 1f);
            rowHeights[1] = Mathf.Max(settings.Data.RainHeightRow2, 1f);
            rowHeights[2] = Mathf.Max(settings.Data.RainHeightRow3, 1f);
            ghostRowSpeeds[0] = Mathf.Max(settings.Data.GhostRainSpeedRow1 / 300f, minSpeedFactor);
            ghostRowSpeeds[1] = Mathf.Max(settings.Data.GhostRainSpeedRow2 / 300f, minSpeedFactor);
            ghostRowSpeeds[2] = Mathf.Max(settings.Data.GhostRainSpeedRow3 / 300f, minSpeedFactor);
            ghostRowHeights[0] = Mathf.Max(settings.Data.GhostRainHeightRow1, 1f);
            ghostRowHeights[1] = Mathf.Max(settings.Data.GhostRainHeightRow2, 1f);
            ghostRowHeights[2] = Mathf.Max(settings.Data.GhostRainHeightRow3, 1f);
            rowStartYs[0] = settings.Data.RainStartYRow1;
            rowStartYs[1] = settings.Data.RainStartYRow2;
            rowStartYs[2] = settings.Data.RainStartYRow3;
            ghostRowStartYs[0] = settings.Data.GhostRainStartYRow1;
            ghostRowStartYs[1] = settings.Data.GhostRainStartYRow2;
            ghostRowStartYs[2] = settings.Data.GhostRainStartYRow3;
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
            cachedStartY1 = settings.Data.RainStartYRow1;
            cachedStartY2 = settings.Data.RainStartYRow2;
            cachedStartY3 = settings.Data.RainStartYRow3;
            cachedGhostStartY1 = settings.Data.GhostRainStartYRow1;
            cachedGhostStartY2 = settings.Data.GhostRainStartYRow2;
            cachedGhostStartY3 = settings.Data.GhostRainStartYRow3;
        }

        private void UpdateSingleRainDrop(RawRain rain, Key key, int keyIndex, Vector2 keyPos, int j, int row, float dtSec)
        {
            if (rain.removed) return;

            float speed = rain.isGhost ? ghostRowSpeeds[row] : rowSpeeds[row];
            float height = rain.isGhost ? ghostRowHeights[row] : rowHeights[row];
            float dt = dtSec * 1000f;
            if (!rain.UpdateLocation(rain.growing, speed, height, dt))
            {
                ReturnRawRainAndRemove(rain, key, j);
                return;
            }

            // Rect first, fade last: a drop removed by fade-out must not be written to again
            // (ReturnRawRain can hand the record to a new drop in the same frame).
            // 先算矩形后处理淡出：被淡出移除的雨滴不能再被写入（ReturnRawRain 可能在同帧
            // 就把记录发给了新雨滴）。
            UpdateRectAndTrail(rain, key, keyIndex, keyPos, speed, height);
            UpdateFade(rain, dtSec, key, j);
        }

        private void UpdateFade(RawRain rain, float dtSec, Key key, int j)
        {
            if (!rain.fadingOut) return;
            rain.fadeTimer += dtSec;
            // Zero/negative duration (typed values are unclamped): treat as instant fade — avoids a
            // 0/0 NaN alpha on the first tick. / 零/负时长（键入值不钳制）：按立即淡出处理——
            // 避免首帧 0/0 得到 NaN 透明度。
            float t = settings.Data.RainFadeDuration > 0f
                ? Mathf.Clamp01(rain.fadeTimer / settings.Data.RainFadeDuration)
                : 1f;
            rain.alpha = 1f - (t * (2f - t));
            if (t >= 1f)
                ReturnRawRainAndRemove(rain, key, j);
        }

        /// <summary>
        /// Compute the drop's layer-space rect (including the affect-rain press scale) and trail
        /// gradient params. Start-Y and ghost offsets are read live from settings, so the GUI sliders
        /// update existing drops without any container bookkeeping.
        /// 计算雨滴的图层空间矩形（含雨滴跟随按压缩放）与轨迹渐变参数。起始 Y 与鬼雨偏移实时读取
        /// 设置，GUI 滑块直接作用于现有雨滴，无需容器簿记。
        /// </summary>
        private void UpdateRectAndTrail(RawRain rain, Key key, int keyIndex, Vector2 keyPos, float speed, float height)
        {
            float trailEdgeDist = rain.elapsedMs * speed;
            float drawH = trailEdgeDist > height
                ? rain.FinalSize.y - trailEdgeDist + height
                : (rain.growing ? trailEdgeDist : rain.FinalSize.y);
            rain.dFar = Mathf.Min(trailEdgeDist, height);
            rain.dNear = rain.dFar - drawH;
            rain.trackHeight = height;
            rain.fadePx = settings.Data.EnableRainGradient && !rain.isGhost ? settings.Data.RainFadePx : 0f;

            float w = rain.sizeDelta?.x ?? rain.FinalSize.x;
            float h = rain.sizeDelta?.y ?? rain.FinalSize.y;
            // Old layout: the 275px container anchored to the key rect's BOTTOM-LEFT corner, and the
            // drop hung on the container's top-center anchor — so X centers on the container column,
            // Y measures from (key bottom + start-Y + container height). Start-Y (normal and ghost)
            // is read live here; the baked startY is subtracted back out.
            // 旧布局：275px 容器锚在按键矩形左下角，雨滴挂在容器顶中锚点上——X 以容器列居中，
            // Y 从（键底边 + 起始 Y + 容器高）起算。起始 Y（普通与鬼雨）在此实时读取，
            // 创建时烙入的 startY 被减回。
            int ri = rain.color == 0 ? 0 : rain.color == 3 ? 2 : 1;
            float baseStart = rain.isGhost ? ghostRowStartYs[ri] : rowStartYs[ri];
            float travel = rain.anchoredPosition.Value.y - rain.startY;
            float cx = keyPos.x + key.rainOffsetX + key.rainWidth * 0.5f;
            float topY = keyPos.y - key.keySize.y * 0.5f + baseStart + RainContainerHeight + travel;

            float s = Layer != null ? Layer.GetKeyScale(keyIndex) : 1f;
            rain.scaleF = s;
            if (s != 1f)
            {
                // Scale around the key box center — what the old root-scale + compensation achieved /
                // 围绕按键框中心缩放——与旧根缩放 + 位置补偿等效
                float kcx = keyPos.x + key.keySize.x * 0.5f;
                cx = kcx + (cx - kcx) * s;
                w *= s;
                topY = keyPos.y + (topY - keyPos.y) * s;
                h *= s;
            }
            rain.rect = new Rect(cx - w * 0.5f, topY - h, w, h);
        }

        private void ReturnRawRainAndRemove(RawRain rain, Key key, int listIndex)
        {
            rain.removed = true;
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
                if (settings.Data.EnableRainFade)
                {
                    key.rainList[i].fadingOut = true;
                    key.rainList[i].fadeTimer = 0f;
                }
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
                    ReturnRawRain(rain);
                key.rainList.Clear();
            }
            if (Layer != null) Layer.MarkDirty();
        }

        public void ClearAll(Key[] keys)
        {
            ClearActiveDrops(keys);
            rawRainPool.Clear();
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
                r.isGhost = false;
                r.growing = false;
                r.FinalSize = default;
                r.rect = default;
                r.mainColor = Color.white;
                r.alpha = 1f;
                r.scaleF = 1f;
                r.fadingOut = false;
                r.fadeTimer = 0f;
                r.dNear = r.dFar = r.trackHeight = r.fadePx = 0f;
                r.shadowEnabled = false;
                r.shadowColor = default;
                r.shadowOffsetX = r.shadowOffsetY = 0f;
                r.outlineEnabled = false;
                r.outlineColor = default;
                r.outlineWidth = 0f;
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
            r.isGhost = false;
            r.growing = false;
            rawRainPool.Push(r);
        }

        private void CreateRainDropForKey(int keyIndex, Key key, bool isGhost = false)
        {
            if (KeyViewer.IsFullKeyboard) return;

            int row = keyIndex < 8 ? 1 : (keyIndex < 16 ? 2 : 3);

            RawRain rawRain = GetRawRain(key.color);
            float baseY = row == 1 ? settings.Data.RainStartYRow1 : row == 2 ? settings.Data.RainStartYRow2 : settings.Data.RainStartYRow3;
            rawRain.startY = isGhost
                ? (row == 1 ? settings.Data.GhostRainStartYRow1 : row == 2 ? settings.Data.GhostRainStartYRow2 : settings.Data.GhostRainStartYRow3) - baseY
                : 0f;

            if (isGhost)
            {
                rawRain.mainColor = settings.Data.EnablePerKeyColors
                    ? settings.Data.PerKeyGhostRainColor[keyIndex]
                    : GetGhostRainColor(key.color);

                rawRain.shadowEnabled = row == 1 ? settings.Data.EnableGhostRainShadowRow1
                    : row == 2 ? settings.Data.EnableGhostRainShadowRow2
                    : settings.Data.EnableGhostRainShadowRow3;
                rawRain.shadowColor = row == 1 ? settings.Data.GhostRainShadowColorRow1
                    : row == 2 ? settings.Data.GhostRainShadowColorRow2
                    : settings.Data.GhostRainShadowColorRow3;
                rawRain.shadowOffsetX = row == 1 ? settings.Data.GhostRainShadowOffsetXRow1
                    : row == 2 ? settings.Data.GhostRainShadowOffsetXRow2
                    : settings.Data.GhostRainShadowOffsetXRow3;
                rawRain.shadowOffsetY = row == 1 ? settings.Data.GhostRainShadowOffsetYRow1
                    : row == 2 ? settings.Data.GhostRainShadowOffsetYRow2
                    : settings.Data.GhostRainShadowOffsetYRow3;
                rawRain.outlineEnabled = row == 1 ? settings.Data.EnableGhostRainOutlineRow1
                    : row == 2 ? settings.Data.EnableGhostRainOutlineRow2
                    : settings.Data.EnableGhostRainOutlineRow3;
                rawRain.outlineColor = row == 1 ? settings.Data.GhostRainOutlineColorRow1
                    : row == 2 ? settings.Data.GhostRainOutlineColorRow2
                    : settings.Data.GhostRainOutlineColorRow3;
                rawRain.outlineWidth = row == 1 ? settings.Data.GhostRainOutlineWidthRow1
                    : row == 2 ? settings.Data.GhostRainOutlineWidthRow2
                    : settings.Data.GhostRainOutlineWidthRow3;
            }
            else
            {
                rawRain.mainColor = key.rainColor;

                rawRain.shadowEnabled = row == 1 ? settings.Data.EnableRainShadowRow1
                    : row == 2 ? settings.Data.EnableRainShadowRow2
                    : settings.Data.EnableRainShadowRow3;
                rawRain.shadowColor = row == 1 ? settings.Data.RainShadowColorRow1
                    : row == 2 ? settings.Data.RainShadowColorRow2
                    : settings.Data.RainShadowColorRow3;
                rawRain.shadowOffsetX = row == 1 ? settings.Data.RainShadowOffsetXRow1
                    : row == 2 ? settings.Data.RainShadowOffsetXRow2
                    : settings.Data.RainShadowOffsetXRow3;
                rawRain.shadowOffsetY = row == 1 ? settings.Data.RainShadowOffsetYRow1
                    : row == 2 ? settings.Data.RainShadowOffsetYRow2
                    : settings.Data.RainShadowOffsetYRow3;
                rawRain.outlineEnabled = row == 1 ? settings.Data.EnableRainOutlineRow1
                    : row == 2 ? settings.Data.EnableRainOutlineRow2
                    : settings.Data.EnableRainOutlineRow3;
                rawRain.outlineColor = row == 1 ? settings.Data.RainOutlineColorRow1
                    : row == 2 ? settings.Data.RainOutlineColorRow2
                    : settings.Data.RainOutlineColorRow3;
                rawRain.outlineWidth = row == 1 ? settings.Data.RainOutlineWidthRow1
                    : row == 2 ? settings.Data.RainOutlineWidthRow2
                    : settings.Data.RainOutlineWidthRow3;
            }

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
            if (KeyViewer.IsFullKeyboard) return false;
            // Row mapping must match the speed/height/start-Y rows: 0-7 = row 1, 8-15 = row 2,
            // 16+ = row 3. (The released version gated rows 1+2 together on Row2, leaving the
            // row-1 toggle dead.) / 排映射须与速度/高度/起始 Y 一致：0-7 第1排，8-15 第2排，16+ 第3排。
            // （旧版本把第 1、2 排一起挂在 Row2 开关上，第 1 排开关无效。）
            if (keyIndex < 8) return settings.Data.EnableRainForRow1;
            if (keyIndex < 16) return settings.Data.EnableRainForRow2;
            if (keyIndex < KeyViewer.FootKeyBase) return settings.Data.EnableRainForRow3;
            return false;
        }

        /// <summary>Return the active drops of one rain row (0/1/2) to the pool / 将某一雨滴排（0/1/2）的活跃雨滴回收到池</summary>
        public void ClearRowDrops(Key[] keys, int row)
        {
            if (keys == null) return;
            int start = row * 8;
            int end = start + 8;
            for (int i = start; i < end && i < keys.Length; i++)
            {
                Key key = keys[i];
                if (key == null) continue;
                foreach (var rain in key.rainList)
                    ReturnRawRain(rain);
                key.rainList.Clear();
            }
            for (int i = rainActiveKeys.Count - 1; i >= 0; i--)
            {
                if (rainActiveKeys[i] >= start && rainActiveKeys[i] < end)
                {
                    rainActiveSet.Remove(rainActiveKeys[i]);
                    rainActiveKeys.RemoveAt(i);
                }
            }
            if (Layer != null) Layer.MarkDirty();
        }
    }
}
