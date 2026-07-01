using UnityEngine;
using UnityEngine.UI;

namespace JipperKeyViewer.KeyViewer
{
    public class Rain : MonoBehaviour
    {
        public RainGraphic graphic;
        public Image ghostImage;
        public new RectTransform transform;
        public RawRain rawRain;
        public bool fadingOut;
        public float fadeTimer;

        private void Awake()
        {
            transform = GetComponent<RectTransform>();
            graphic = gameObject.AddComponent<RainGraphic>();
            graphic.raycastTarget = false;

            var imgGo = new GameObject("GhostImage");
            var imgRt = imgGo.AddComponent<RectTransform>();
            imgRt.SetParent(transform, false);
            imgRt.anchorMin = Vector2.zero;
            imgRt.anchorMax = Vector2.one;
            imgRt.offsetMin = Vector2.zero;
            imgRt.offsetMax = Vector2.zero;
            ghostImage = imgGo.AddComponent<Image>();
            ghostImage.enabled = false;
            ghostImage.raycastTarget = false;
        }

        public void Init(Transform parent)
        {
            gameObject.SetActive(true);
            transform.SetParent(parent);
            transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 1);
            transform.anchoredPosition = Vector2.zero;
            transform.sizeDelta = Vector2.zero;
            transform.localScale = Vector3.one;
            fadingOut = false;
            fadeTimer = 0f;
            graphic.enabled = true;
            graphic.renderMain = true;
            ghostImage.enabled = false;
        }

        public void Init(Transform parent, Sprite sprite, bool isTiled)
        {
            gameObject.SetActive(true);
            transform.SetParent(parent);
            transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 1);
            transform.anchoredPosition = Vector2.zero;
            transform.sizeDelta = Vector2.zero;
            transform.localScale = Vector3.one;
            fadingOut = false;
            fadeTimer = 0f;
            graphic.enabled = true;   // enabled for shadow/outline support
            graphic.renderMain = false; // ghost Image draws the main sprite
            ghostImage.enabled = true;
            ghostImage.sprite = sprite;
            ghostImage.type = isTiled ? Image.Type.Tiled : Image.Type.Simple;
        }

        public void StartFadeOut(float duration)
        {
            fadingOut = true;
            fadeTimer = 0f;
        }
    }
}
