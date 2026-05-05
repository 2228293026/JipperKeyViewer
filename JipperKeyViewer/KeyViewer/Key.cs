using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace JipperKeyViewer.KeyViewer
{
    public class Key : MonoBehaviour
    {
        public TextMeshProUGUI text;
        public Image background;
        public Image outline;
        public TextMeshProUGUI value;
        public GameObject rain;
        public byte color;
        public List<RawRain> rainList = new List<RawRain>();
        public ConcurrentQueue<RawRain> rawRainQueue = new ConcurrentQueue<RawRain>();
        public bool isPressed;

        private void Update()
        {
            while (rawRainQueue.TryDequeue(out RawRain rawRain))
            {
                Rain rainComponent = KeyViewer.instance.GetRainFromPool(rain.transform);
                rainComponent.rawRain = rawRain;
                rainComponent.image.color = color switch
                {
                    0 => KeyViewer.Settings.RainColor,
                    3 => KeyViewer.Settings.RainColor3,
                    _ => KeyViewer.Settings.RainColor2
                };
                rainComponent.transform.SetSiblingIndex(color - 1);
            }
        }

        private void OnDestroy()
        {
            while (rawRainQueue.TryDequeue(out _)) { }
            foreach (RawRain rawRain in rainList)
            {
                rawRain.removed = true;
            }
            rainList.Clear();
        }
    }
}
