// Rain drop rendering and lifecycle / 雨滴渲染和生命周期
// Uses object pool pattern via KeyViewer.ReturnRain / 通过 KeyViewer.ReturnRain 使用对象池模式

using UnityEngine;
using UnityEngine.UI;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>
    /// Visual rain drop component / 可视化雨滴组件
    /// Each frame reads position/size updates from its RawRain data object / 每帧从其 RawRain 数据对象读取位置/大小更新
    /// Returns itself to the pool when the rain drop expires / 雨滴过期时将自己归还到对象池
    /// </summary>
    public class Rain : MonoBehaviour
    {
        /// <summary>Rain drop image / 雨滴图片</summary>
        public Image image;
        /// <summary>RectTransform (new keyword hides MonoBehaviour.transform) / RectTransform（new 隐藏了 MonoBehaviour.transform）</summary>
        public new RectTransform transform;
        /// <summary>Reference to the data object driving this rain drop / 驱动此雨滴的数据对象引用</summary>
        public RawRain rawRain;

        private void Awake()
        {
            transform = GetComponent<RectTransform>();
            image = gameObject.AddComponent<Image>();
            image.raycastTarget = false;
        }

        /// <summary>
        /// Initialize the rain drop from the pool and attach to a parent / 从对象池初始化雨滴并挂接到父对象
        /// </summary>
        public void Init(Transform parent)
        {
            gameObject.SetActive(true);
            transform.SetParent(parent);
            transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 1);
            transform.anchoredPosition = Vector2.zero;
            transform.sizeDelta = Vector2.zero;
            transform.localScale = Vector3.one;
            var c = image.color;
            c.a = 1f;
            image.color = c;
        }

        /// <summary>
        /// Each frame: sync RectTransform from RawRain data, or return to pool if removed / 每帧：从 RawRain 数据同步 RectTransform，如果已移除则归还对象池
        /// </summary>
        public void Update()
        {
            if (rawRain.removed)
            {
                KeyViewer.instance.ReturnRawRain(rawRain);
                rawRain = null;
                KeyViewer.instance.ReturnRain(this);
            }
        }
    }
}
