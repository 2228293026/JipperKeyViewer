// Key MonoBehaviour: visual key on canvas / 按键 MonoBehaviour：画布上的可视按键
// Manages text, background, outline, count display and rain effect queue / 管理文本、背景、轮廓、计数显示和雨滴效果队列

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>
    /// Represents a single on-screen key / 表示一个屏幕上的按键
    /// Composed of a text label, background image, outline image, count text, and optional rain container / 由文本标签、背景图、轮廓图、计数文本和可选的雨滴容器组成
    /// </summary>
    public class Key : MonoBehaviour
    {
        /// <summary>Key label text (e.g. "Tab", "A") / 按键标签文本（如 "Tab"、"A"）</summary>
        public TextMeshProUGUI text;
        /// <summary>Background image / 背景图片</summary>
        public Image background;
        /// <summary>Outline image / 轮廓图片</summary>
        public Image outline;
        /// <summary>Press count text / 按键计数文本</summary>
        public TextMeshProUGUI value;
        /// <summary>Rain effect container GameObject / 雨滴效果容器 GameObject</summary>
        public GameObject rain;
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
        /// <summary>Visuals wrapper — scaled on press so rain outside it stays unaffected / 视觉包裹层，按下时缩放此层，雨滴容器在外面不受影响</summary>
        public Transform visuals;
    }
}
