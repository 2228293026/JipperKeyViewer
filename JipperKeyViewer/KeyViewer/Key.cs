// Key MonoBehaviour: logical key / 按键 MonoBehaviour：逻辑按键
// Box geometry lives in the merged KeyShapeLayer; text lives under a per-key wrapper in the text
// canvas; rain drops live in the merged RainLayer reading key.rainList. This component stays on
// the key root, marking the key's position and holding state.
// 按键框几何在合并的 KeyShapeLayer 中；文本在文本画布的每键包裹层下；雨滴在合并 RainLayer 中读取
// key.rainList。本组件保留在按键根节点上，标记按键位置并持有状态。

using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>
    /// Represents a single on-screen key / 表示一个屏幕上的按键
    /// Composed of a text label, count text, a shape slot in the merged layer, and an optional rain container / 由文本标签、计数文本、合并图层中的形状槽位和可选的雨滴容器组成
    /// </summary>
    public class Key : MonoBehaviour
    {
        /// <summary>Key label text (e.g. "Tab", "A") / 按键标签文本（如 "Tab"、"A"）</summary>
        public TextMeshProUGUI text;
        /// <summary>Press count text / 按键计数文本</summary>
        public TextMeshProUGUI value;
        /// <summary>Slot index in the merged KeyShapeLayer (-1 = none) / 合并 KeyShapeLayer 中的槽位索引（-1 = 无）</summary>
        public int shapeSlot = -1;
        /// <summary>Key box size (width, height) for layer/text positioning / 按键框尺寸（宽，高），用于图层与文本定位</summary>
        public Vector2 keySize;
        /// <summary>Rain color index (0=row1, 1=row2, 3=row3) / 雨滴颜色索引（0=第1排，1=第2排，3=第3排）</summary>
        public byte color;
        /// <summary>Pre-computed rain color for this key / 预先计算的该键雨滴颜色</summary>
        public Color rainColor = Color.white;
        /// <summary>Active rain drops list / 活跃中的雨滴列表</summary>
        public List<RawRain> rainList = new List<RawRain>();
        /// <summary>Whether this key is currently pressed / 当前是否被按下</summary>
        public bool isPressed;
        /// <summary>Running press-animation coroutine (null if none) / 运行中的按键动画协程（无则为 null）</summary>
        public Coroutine currentAnim;
        /// <summary>Text wrapper in the text canvas — center pivot over the key box, scaled on press / 文本画布中的文本包裹层，轴心在按键框中心，按压时缩放</summary>
        public Transform visuals;
        /// <summary>X offset for rain container alignment (0 for standard keys) / 雨滴容器的 X 偏移（标准按键为 0）</summary>
        public float rainOffsetX;
        /// <summary>Rain column width (key width; 50 when redirected to a front column) / 雨滴列宽（按键宽度；重指向前列时为 50）</summary>
        public float rainWidth = 50f;
    }
}
