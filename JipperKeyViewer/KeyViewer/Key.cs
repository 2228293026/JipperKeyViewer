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
        /// <summary>RainSystem that manages this key's rain objects / 管理此按键雨滴对象的 RainSystem</summary>
        public RainSystem rainSystem;
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
        /// <summary>Queue of newly triggered rain drops awaiting Rain component assignment / 新触发的雨滴队列，等待分配 Rain 组件</summary>
        public Queue<RawRain> rawRainQueue = new Queue<RawRain>();
        /// <summary>Whether this key is currently pressed / 当前是否被按下</summary>
        public bool isPressed;

        /// <summary>
        /// Process the rain queue: assign Rain components from pool to pending RawRain data / 处理雨滴队列：从对象池分配 Rain 组件给待处理的 RawRain 数据
        /// Called each frame after input processing / 每次处理输入后每帧调用
        /// </summary>
        public void ProcessRainQueue()
        {
            while (rawRainQueue.Count > 0)
            {
                RawRain rawRain = rawRainQueue.Dequeue();
                Rain rainComponent = rainSystem.GetRainFromPool(rain.transform);
                rainComponent.rawRain = rawRain;
                rawRain.rainComponent = rainComponent;
                rainComponent.image.color = rainColor;
            }
        }

        /// <summary>
        /// Clean up rain queues when this key is destroyed / 按键销毁时清理雨滴队列
        /// </summary>
        private void OnDestroy()
        {
            while (rawRainQueue.Count > 0)
                rainSystem?.ReturnRawRain(rawRainQueue.Dequeue());
            foreach (RawRain rawRain in rainList)
            {
                rawRain.removed = true;
            }
            rainList.Clear();
        }
    }
}
