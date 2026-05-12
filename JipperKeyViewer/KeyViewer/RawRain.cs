// Raw rain drop data object (no GameObject overhead) / 原始雨滴数据对象（无 GameObject 开销）
// Stores position, size, and timing for the rain effect / 存储雨滴效果的位置、大小和时间
// Position/size computed in UpdateLocation() and consumed by Rain.Update() via nullable dirty flags / 位置/大小在 UpdateLocation() 中计算，通过可空脏标志由 Rain.Update() 消费

using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>
    /// Pure data class for a single rain drop / 单个雨滴的纯数据类
    /// Avoids per-frame GameObject/Component overhead by separating data from rendering / 通过将数据与渲染分离，避免每帧的 GameObject/Component 开销
    /// </summary>
    public class RawRain
    {
        /// <summary>Parent transform (the Key's rain container) / 父级变换（按键的雨滴容器）</summary>
        public Transform transform;
        /// <summary>Accumulated simulated time in milliseconds / 累计模拟时间（毫秒）</summary>
        public float localTime;
        /// <summary>Color index matching the originating key's row / 颜色索引，与来源按键的行对应</summary>
        public byte color;
        /// <summary>Final size at full extension / 完全伸展时的最终大小</summary>
        public Vector2 FinalSize;
        /// <summary>Dirty flag: pending size delta to apply / 脏标志：待应用的大小</summary>
        public Vector2? sizeDelta;
        /// <summary>Dirty flag: pending anchored position to apply / 脏标志：待应用的锚定位置</summary>
        public Vector2? anchoredPosition;
        /// <summary>Whether this rain drop has expired and should be removed / 此雨滴是否已过期，应被移除</summary>
        public bool removed;

        /// <summary>
        /// Update position and size based on elapsed time / 根据经过时间更新位置和大小
        /// Returns false when the drop has moved beyond the allowed height and should be removed / 当雨滴超出允许高度时返回 false，应被移除
        /// </summary>
        /// <param name="deltaMs">Elapsed time in ms since last frame / 距离上一帧的毫秒数</param>
        /// <param name="updateSize">Whether to update the size this frame (only for the newest drop when key is held) / 是否在本帧更新大小（仅当按键被按住时对最新雨滴有效）</param>
        /// <param name="speedFactor">Speed multiplier (configured speed / 300) / 速度倍率（配置速度 / 300）</param>
        /// <param name="height">Maximum drop height in pixels / 最大雨滴高度（像素）</param>
        public bool UpdateLocation(float deltaMs, bool updateSize, float speedFactor, float height)
        {
            localTime += deltaMs;
            float y = localTime * speedFactor;
            if (updateSize || FinalSize == default)
                FinalSize = new Vector2(color switch
                {
                    0 => 50,
                    3 => 30,
                    _ => 40
                }, localTime * speedFactor);
            if (y > height)
            {
                float sizeY = FinalSize.y - y + height;
                if (sizeY < 0) return false;
                sizeDelta = new Vector2(FinalSize.x, sizeY);
                anchoredPosition = new Vector2(0, height);
            }
            else
            {
                if (updateSize) sizeDelta = FinalSize;
                anchoredPosition = new Vector2(0, y);
            }
            return true;
        }

        /// <summary>
        /// Create a new RawRain for the given key / 为指定按键创建新的 RawRain
        /// </summary>
        public RawRain(Transform transform, byte color)
        {
            this.transform = transform;
            this.color = color;
            localTime = 0;
        }
    }
}
