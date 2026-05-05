using UnityEngine;
using UnityEngine.UI;

namespace JipperKeyViewer.KeyViewer
{
    public class Rain : MonoBehaviour
    {
        public Image image;
        public new RectTransform transform;
        public RawRain rawRain;

        private void Awake()
        {
            transform = GetComponent<RectTransform>();
            image = gameObject.AddComponent<Image>();
        }

        public void Init(Transform parent)
        {
            gameObject.SetActive(true);
            transform.SetParent(parent);
            transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 1);
            transform.anchoredPosition = Vector2.zero;
            transform.sizeDelta = Vector2.zero;
            transform.localScale = Vector3.one;
        }

        public void Update()
        {
            if (rawRain.removed)
            {
                rawRain = null;
                KeyViewer.instance.ReturnRain(this);
                return;
            }
            if (rawRain.sizeDelta != null)
            {
                transform.sizeDelta = rawRain.sizeDelta.Value;
                rawRain.sizeDelta = null;
            }
            if (rawRain.anchoredPosition != null)
            {
                transform.anchoredPosition = rawRain.anchoredPosition.Value;
                rawRain.anchoredPosition = null;
            }
        }
    }
}
