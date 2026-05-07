using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace JipperKeyViewer.KeyViewer
{
    public partial class KeyViewer : MonoBehaviour
    {
        private void EnableKeyViewer()
        {
            if (KeyViewerObject != null || !Settings.Enabled) return;
            if (!TryLoadResources())
            {
                Main.Mod.Logger.Error("KeyViewer: \u65E0\u6CD5\u52A0\u8F7D AssetBundle\uFF0C\u8BF7\u68C0\u67E5 assets/ \u76EE\u5F55\u4E0B\u662F\u5426\u5B58\u5728\u5BF9\u5E94\u7248\u672C\u7684 AB \u6587\u4EF6");
                return;
            }
            KeyViewerObject = new GameObject("JipperResourcePack KeyViewer");
            Canvas = KeyViewerObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = Canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f; // match height so vertical positions are resolution-independent
            Canvas.gameObject.AddComponent<GraphicRaycaster>();
            KeyViewerSizeObject = new GameObject("SizeObject");
            RectTransform rectTransform = KeyViewerSizeObject.AddComponent<RectTransform>();
            rectTransform.SetParent(KeyViewerObject.transform);
            rectTransform.localScale = new Vector3(Settings.Size, Settings.Size, 1);
            // Fill the full canvas; pivot at bottom-left so localScale doesn't shift child positions
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = Vector2.zero;
            rectTransform.offsetMin = rectTransform.offsetMax = Vector2.zero;
            Keys = new Key[36];
            switch (Settings.KeyViewerStyle)
            {
                case KeyviewerStyle.Key12:
                    Initialize12KeyViewer();
                    break;
                case KeyviewerStyle.Key16:
                    Initialize16KeyViewer();
                    break;
                case KeyviewerStyle.Key20:
                    Initialize20KeyViewer();
                    break;
                case KeyviewerStyle.Key8:
                    Initialize8KeyViewer();
                    break;
                case KeyviewerStyle.Key10:
                    Initialize10KeyViewer();
                    break;
            }
            switch (Settings.FootKeyViewerStyle)
            {
                case FootKeyviewerStyle.Key2:
                    InitializeFootKeyViewer(2);
                    break;
                case FootKeyviewerStyle.Key4:
                    InitializeFootKeyViewer(4);
                    break;
                case FootKeyviewerStyle.Key6:
                    InitializeFootKeyViewer(6);
                    break;
                case FootKeyviewerStyle.Key8:
                    InitializeFootKeyViewer(8);
                    break;
                case FootKeyviewerStyle.Key10:
                    InitializeFootKeyViewer(10);
                    break;
                case FootKeyviewerStyle.Key12:
                    InitializeFootKeyViewer(12);
                    break;
                case FootKeyviewerStyle.Key14:
                    InitializeFootKeyViewer(14);
                    break;
                case FootKeyviewerStyle.Key16:
                    InitializeFootKeyViewer(16);
                    break;
            }
            Object.DontDestroyOnLoad(KeyViewerObject);
            PressTimes = new Queue<long>();
            Stopwatch = System.Diagnostics.Stopwatch.StartNew();
        }

        private void DisableKeyViewer()
        {
            if (KeyViewerObject == null) return;
            Object.Destroy(KeyViewerObject);
            KeyViewerObject = null;
            KeyViewerSizeObject = null;
            while (rainPool.Count > 0) Object.Destroy(rainPool.Pop().gameObject);
            foreach (var mat in shadowMaterials.Values)
                Object.Destroy(mat);
            shadowMaterials.Clear();
            Canvas = null;
            Keys = null;
            PressTimes = null;
            Stopwatch = null;
        }

        private void ProcessMainAndFootKeysInUpdate(long elapsedMilliseconds)
        {
            if (cachedKeyStyle != Settings.KeyViewerStyle)
            {
                cachedMainKeys = GetKeyCode();
                cachedKeyStyle = Settings.KeyViewerStyle;
            }
            if (cachedFootStyle != Settings.FootKeyViewerStyle)
            {
                cachedFootKeys = GetFootKeyCode();
                cachedFootStyle = Settings.FootKeyViewerStyle;
            }
            ProcessKeyGroup(cachedMainKeys, 0, elapsedMilliseconds);
            if (cachedFootKeys != null)
                ProcessKeyGroup(cachedFootKeys, 20, elapsedMilliseconds);
            if (Total != null && Total.value != null && lastTotal != Settings.TotalCount)
            {
                lastTotal = Settings.TotalCount;
                Total.value.text = lastTotal.ToString();
            }
        }

        private void ProcessKeyRainQueues()
        {
            if (Keys == null) return;
            for (int i = 0; i < Keys.Length; i++)
            {
                if (Keys[i] != null)
                    Keys[i].ProcessRainQueue();
            }
        }

        private void ProcessKeyGroup(KeyCode[] keyCodes, int baseIndex, long elapsedMs)
        {
            for (int i = 0; i < keyCodes.Length; i++)
            {
                int idx = baseIndex + i;
                if (idx >= Keys.Length || Keys[idx] == null) continue;
                bool current = Input.GetKey(keyCodes[i]);
                if (current != Keys[idx].isPressed)
                {
                    UpdateKey(idx, current);
                    Keys[idx].isPressed = current;
                    if (current)
                    {
                        Settings.Count[idx]++;
                        Settings.TotalCount++;
                        if (Keys[idx].value != null)
                            Keys[idx].value.text = Settings.Count[idx].ToString();
                        PressTimes.Enqueue(elapsedMs);
                        if (Settings.EnableRainEffect)
                            TriggerRainEffect(idx, Keys[idx]);
                    }
                }
            }
        }

        private void ProcessKpsInUpdate(long elapsedMilliseconds)
        {
            if (PressTimes != null)
            {
                while (PressTimes.Count > 0 && elapsedMilliseconds - PressTimes.Peek() > 1000)
                    PressTimes.Dequeue();
                int currentKps = PressTimes.Count;
                if (lastKps != currentKps)
                {
                    lastKps = currentKps;
                    if (Kps != null && Kps.value != null) Kps.value.text = currentKps.ToString();
                }
            }
        }

        private void UpdateKey(int i, bool pressed)
        {
            if (Keys == null || i >= Keys.Length || Keys[i] == null) return;
            Key key = Keys[i];
            key.background.color = pressed ? Settings.BackgroundClicked : Settings.Background;
            key.outline.color = pressed ? Settings.OutlineClicked : Settings.Outline;
            key.text.color = pressed ? Settings.TextClicked : Settings.Text;
            if (key.value != null) key.value.color = key.text.color;
        }

        private void Initialize12KeyViewer()
        {
            int remove = Settings.DownLocation ? 200 : 0;
            for (int i = 0; i < 8; i++) Keys[i] = CreateKey(i, 54 * i, 279 - remove, 50, 0);
            Keys[8] = CreateKey(8, 81 + 54, 225 - remove, 77, 1);
            Keys[9] = CreateKey(9, 81, 225 - remove, 50, 1);
            Keys[10] = CreateKey(10, 54 * 4, 225 - remove, 77, 1);
            Keys[11] = CreateKey(11, 54 * 4 + 81, 225 - remove, 50, 1);
            Kps = CreateKey(-1, 0, 225 - remove, 77, -1);
            Total = CreateKey(-2, 81 + 54 * 5, 225 - remove, 77, -1);
        }

        private void Initialize16KeyViewer()
        {
            int remove = Settings.DownLocation ? 200 : 0;
            for (int i = 0; i < 8; i++) Keys[i] = CreateKey(i, 54 * i, 320 - remove, 50, 0);
            for (int i = 0; i < 8; i++)
            {
                int j = BackSequence16[i];
                Keys[j] = CreateKey(j, 54 * i, 266 - remove, 50, 1);
            }
            Kps = CreateKey(-1, 0, 220 - remove, 212, -1, true);
            Total = CreateKey(-2, 216, 220 - remove, 212, -1, true);
        }

        private void Initialize20KeyViewer()
        {
            int remove = Settings.DownLocation ? 200 : 0;
            for (int i = 0; i < 8; i++) Keys[i] = CreateKey(i, 54 * i, 333 - remove, 50, 0);
            for (int i = 0; i < 8; i++)
            {
                int j = BackSequence20[i];
                Keys[j] = CreateKey(j, 54 * i, 279 - remove, 50, 1);
            }
            Keys[16] = CreateKey(16, 81 + 54, 225 - remove, 77, 3);
            Keys[17] = CreateKey(17, 81, 225 - remove, 50, 3);
            Keys[18] = CreateKey(18, 54 * 4, 225 - remove, 77, 3);
            Keys[19] = CreateKey(19, 54 * 4 + 81, 225 - remove, 50, 3);
            Kps = CreateKey(-1, 0, 225 - remove, 77, -1);
            Total = CreateKey(-2, 81 + 54 * 5, 225 - remove, 77, -1);
        }

        private void Initialize8KeyViewer()
        {
            int remove = Settings.DownLocation ? 200 : 0;
            for (int i = 0; i < 8; i++) Keys[i] = CreateKey(i, 54 * i, 279 - remove, 50, 0);
            Kps = CreateKey(-1, 0, 233 - remove, 212, -1, true);
            Total = CreateKey(-2, 216, 233 - remove, 212, -1, true);
        }

        private void Initialize10KeyViewer()
        {
            int remove = Settings.DownLocation ? 200 : 0;
            for (int i = 0; i < 8; i++) Keys[i] = CreateKey(i, 54 * i, 279 - remove, 50, 0);
            Keys[8] = CreateKey(8, 81, 225 - remove, 129, 1);
            Keys[9] = CreateKey(9, 54 * 4, 225 - remove, 129, 1);
            Kps = CreateKey(-1, 0, 225 - remove, 77, -1);
            Total = CreateKey(-2, 81 + 54 * 5, 225 - remove, 77, -1);
        }

        private void InitializeFootKeyViewer(int size)
        {
            for (int i = 20; i < 20 + size; i++)
            {
                int col;
                int row;
                if (size <= 8)
                {
                    col = i - 20;
                    row = 0;
                }
                else
                {
                    if (i - 20 < 8)
                    {
                        col = i - 20;
                        row = 0;
                    }
                    else
                    {
                        col = (i - 20) - 8;
                        row = 1;
                    }
                }
                int baseY = size > 8 ? 15 + 34 : 15;
                int x = 432 + col * 34;
                if (size > 8 && row == 1)
                    x += (8 - (size - 8)) * 17;
                int y = baseY - row * 34;
                Keys[i] = CreateKey(i, x, y, 30, -1, true, false);
            }
        }

        private Key CreateKey(int i, float x, float y, float sizeX, int raining, bool slim = false, bool count = true)
        {
            if (defaultFont == null)
                defaultFont = GetCurrentFont();
            GameObject obj = new("Key " + i);
            KeyViewerSettings settings = Settings;
            RectTransform transform = obj.AddComponent<RectTransform>();
            transform.SetParent(KeyViewerSizeObject.transform);
            transform.sizeDelta = new Vector2(sizeX, slim ? 30 : 50);
            transform.anchorMin = transform.anchorMax = Vector2.zero;
            transform.pivot = new Vector2(0, 0.5f);
            transform.anchoredPosition = new Vector2(x + ScreenCenterOffsetX, y);
            transform.localScale = Vector3.one;
            Key key = obj.AddComponent<Key>();
            key.isPressed = false;
            GameObject gameObject;
            Image image;
            TextMeshProUGUI text;
            // Background
            gameObject = new GameObject("Background");
            transform = gameObject.AddComponent<RectTransform>();
            transform.SetParent(obj.transform);
            transform.anchorMin = transform.anchorMax = transform.pivot = Vector2.zero;
            transform.anchoredPosition = Vector2.zero;
            transform.sizeDelta = new Vector2(sizeX * 2, (slim ? 30 : 50) * 2);
            transform.localScale = new Vector3(0.5f, 0.5f);
            image = gameObject.AddComponent<Image>();
            image.color = settings.Background;
            if (keyBackgroundSprite != null)
            {
                image.sprite = keyBackgroundSprite;
                image.type = Image.Type.Sliced;
            }
            image.raycastTarget = false;
            key.background = image;
            // Outline
            gameObject = new GameObject("Outline");
            transform = gameObject.AddComponent<RectTransform>();
            transform.SetParent(obj.transform);
            transform.anchorMin = transform.anchorMax = transform.pivot = Vector2.zero;
            transform.anchoredPosition = Vector2.zero;
            transform.sizeDelta = new Vector2(sizeX * 2, (slim ? 30 : 50) * 2);
            transform.localScale = new Vector3(0.5f, 0.5f);
            image = gameObject.AddComponent<Image>();
            image.color = settings.Outline;
            if (keyOutlineSprite != null)
            {
                image.sprite = keyOutlineSprite;
                image.type = Image.Type.Sliced;
            }
            image.raycastTarget = false;
            key.outline = image;
            // KeyText
            gameObject = new GameObject("KeyText");
            transform = gameObject.AddComponent<RectTransform>();
            transform.SetParent(obj.transform);
            if (slim)
            {
                transform.sizeDelta = new Vector2(sizeX / 2, 30);
                transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0, 0.5f);
                transform.anchoredPosition = new Vector2(count ? 10 : 7.5f, 0);
            }
            else
            {
                transform.sizeDelta = new Vector2(sizeX - 4, 32);
                transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 1);
                transform.anchoredPosition = new Vector2(0, 2);
            }
            transform.localScale = Vector3.one;
            text = gameObject.AddComponent<TextMeshProUGUI>();
            text.font = GetCurrentFont();
            if (text.font != null)
            {
                var mat = GetShadowMaterial(text.font);
                if (mat != null) text.fontMaterial = mat;
            }
            text.enableAutoSizing = true;
            text.fontSizeMin = 0;
            text.fontSizeMax = 20;
            text.alignment = slim ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
            text.color = settings.Text;
            text.raycastTarget = false;
            key.text = text;
            // CountText (if applicable)
            if (count)
            {
                gameObject = new GameObject("CountText");
                transform = gameObject.AddComponent<RectTransform>();
                transform.SetParent(obj.transform);
                if (slim)
                {
                    transform.sizeDelta = new Vector2(sizeX / 2, 30);
                    transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(1, 0.5f);
                    transform.anchoredPosition = new Vector2(-10, 0);
                }
                else
                {
                    transform.sizeDelta = new Vector2(sizeX - 4, 16);
                    transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 0);
                    transform.anchoredPosition = new Vector2(0, 2);
                }
                transform.localScale = Vector3.one;
                text = gameObject.AddComponent<TextMeshProUGUI>();
                text.font = GetCurrentFont();
                if (text.font != null)
                {
                    var mat = GetShadowMaterial(text.font);
                    if (mat != null) text.fontMaterial = mat;
                }
                text.enableAutoSizing = true;
                text.fontSizeMin = 0;
                text.fontSizeMax = 20;
                text.raycastTarget = false;
                text.alignment = slim ? TextAlignmentOptions.Right : TextAlignmentOptions.Top;
                key.value = text;
            }
            UpdateKeyText(key, i);
            if (raining >= 0)
            {
                if (key.rain == null)
                {
                    key.rain = new GameObject("RainLine");
                    transform = key.rain.AddComponent<RectTransform>();
                    transform.SetParent(obj.transform);
                    transform.sizeDelta = new Vector2(sizeX, 275);
                    transform.anchorMin = transform.anchorMax = transform.pivot = Vector2.zero;
                    transform.anchoredPosition = new Vector2(0, raining switch
                    {
                        0 => -223,
                        3 => -115,
                        _ => -169
                    });
                    transform.localScale = Vector3.one;
                }
                key.color = (byte)raining;
            }
            else
            {
                key.color = 1;
                key.rain?.SetActive(false);
                key.rain = null;
            }
            return key;
        }

        private static void UpdateKeyText(Key key, int i)
        {
            if (key == null) return;
            if (i == -1)
            {
                key.text.text = "KPS";
                key.value.text = 0f.ToString();
                return;
            }
            if (i == -2)
            {
                key.text.text = "Total";
                key.value.text = 0f.ToString();
                return;
            }
            if (i < 20)
            {
                KeyCode[] keyCodes = GetKeyCode();
                string[] keyTexts = GetKeyText();
                if (keyCodes != null && keyTexts != null && i < keyCodes.Length && i < keyTexts.Length)
                {
                    string displayText = !string.IsNullOrEmpty(keyTexts[i]) ? keyTexts[i] : KeyToString(keyCodes[i]);
                    key.text.text = displayText;
                    key.value.text = Settings.Count[i].ToString();
                }
            }
            else
            {
                KeyCode[] footKeyCodes = GetFootKeyCode();
                int footIndex = i - 20;
                if (footKeyCodes != null && footIndex >= 0 && footIndex < footKeyCodes.Length)
                {
                    key.text.text = KeyToString(footKeyCodes[footIndex]);
                }
            }
        }

        /// <summary>
        /// Returns the Y (from layout base) of the lowest key's BOTTOM EDGE.
        /// Accounts for both the center Y and the key height (pivot = left-center).
        /// Subtracted from baseY so Y=1 aligns the visual bottom with the screen edge.
        /// </summary>
        private float GetMinMainKeyOffset()
        {
            bool dl = Settings.DownLocation;
            // Half-heights: slim (KPS/Total in 8K,16K) = 15, regular = 25
            return Settings.KeyViewerStyle switch
            {
                KeyviewerStyle.Key8 => (dl ? 233 - 200 : 233) - 15,
                KeyviewerStyle.Key10 => (dl ? 225 - 200 : 225) - 25,
                KeyviewerStyle.Key12 => (dl ? 225 - 200 : 225) - 25,
                KeyviewerStyle.Key16 => (dl ? 220 - 200 : 220) - 15,
                KeyviewerStyle.Key20 => (dl ? 225 - 200 : 225) - 25,
                _ => (dl ? 220 - 200 : 220) - 15
            };
        }

        private float GetMainLayoutRightmostOffset() => 428f;

        /// <summary>
        /// Maximum Y offset of the topmost key's TOP EDGE from layout base (no DownLocation).
        /// Used so Y=0 (top of screen) aligns the top edge with the screen top.
        /// </summary>
        private float GetMaxMainKeyOffset()
        {
            return Settings.KeyViewerStyle switch
            {
                KeyviewerStyle.Key8 => 279 + 25,
                KeyviewerStyle.Key10 => 279 + 25,
                KeyviewerStyle.Key12 => 279 + 25,
                KeyviewerStyle.Key16 => 320 + 25,
                KeyviewerStyle.Key20 => 333 + 25,
                _ => 320 + 25
            };
        }

        private float GetFootLayoutRightmostOffset(int size)
        {
            int row0Cols = Mathf.Min(size, 8);
            return (row0Cols - 1) * 34 + 30;
        }

        private void ResetKeyViewerPosition()
        {
            if (Keys == null || !Settings.CustomPositionEnabled) return;
            Vector2 norm = Settings.MainKeyViewerPosition;
            // Convert normalized (X: 0=left 1=right, Y: 0=top 1=bottom) to reference pixel offsets from bottom-left.
            // X: interpolate so X=0 = left edge at screen left, X=1 = right edge at screen right.
            // Y: subtract min layout offset so Y=1 puts the lowest key's bottom edge at screen bottom.
            float r = GetMainLayoutRightmostOffset();
            float baseX = norm.x * (1920f - r);
            int remove = Settings.DownLocation ? 200 : 0;
            // Y: lerp so Y=0 = top edge at screen top, Y=1 = bottom edge at screen bottom
            float topBaseY = 1080f - GetMaxMainKeyOffset() + remove;
            float bottomBaseY = -GetMinMainKeyOffset();
            float baseY = Mathf.Lerp(bottomBaseY, topBaseY, 1f - norm.y);
            switch (Settings.KeyViewerStyle)
            {
                case KeyviewerStyle.Key12:
                    for (int i = 0; i < 8; i++)
                        SetKeyPosition(i, baseX + 54 * i, baseY + 279 - remove);
                    SetKeyPosition(8, baseX + 81 + 54, baseY + 225 - remove);
                    SetKeyPosition(9, baseX + 81, baseY + 225 - remove);
                    SetKeyPosition(10, baseX + 54 * 4, baseY + 225 - remove);
                    SetKeyPosition(11, baseX + 54 * 4 + 81, baseY + 225 - remove);
                    SetKeyPosition(-1, baseX + 0, baseY + 225 - remove);
                    SetKeyPosition(-2, baseX + 81 + 54 * 5, baseY + 225 - remove);
                    break;
                case KeyviewerStyle.Key16:
                    for (int i = 0; i < 8; i++)
                        SetKeyPosition(i, baseX + 54 * i, baseY + 320 - remove);
                    for (int i = 0; i < 8; i++)
                    {
                        int j = BackSequence16[i];
                        SetKeyPosition(j, baseX + 54 * i, baseY + 266 - remove);
                    }
                    SetKeyPosition(-1, baseX + 0, baseY + 220 - remove);
                    SetKeyPosition(-2, baseX + 216, baseY + 220 - remove);
                    break;
                case KeyviewerStyle.Key20:
                    for (int i = 0; i < 8; i++)
                        SetKeyPosition(i, baseX + 54 * i, baseY + 333 - remove);
                    for (int i = 0; i < 8; i++)
                    {
                        int j = BackSequence20[i];
                        SetKeyPosition(j, baseX + 54 * i, baseY + 279 - remove);
                    }
                    SetKeyPosition(16, baseX + 81 + 54, baseY + 225 - remove);
                    SetKeyPosition(17, baseX + 81, baseY + 225 - remove);
                    SetKeyPosition(18, baseX + 54 * 4, baseY + 225 - remove);
                    SetKeyPosition(19, baseX + 54 * 4 + 81, baseY + 225 - remove);
                    SetKeyPosition(-1, baseX + 0, baseY + 225 - remove);
                    SetKeyPosition(-2, baseX + 81 + 54 * 5, baseY + 225 - remove);
                    break;
                case KeyviewerStyle.Key8:
                    for (int i = 0; i < 8; i++)
                        SetKeyPosition(i, baseX + 54 * i, baseY + 279 - remove);
                    SetKeyPosition(-1, baseX + 0, baseY + 233 - remove);
                    SetKeyPosition(-2, baseX + 216, baseY + 233 - remove);
                    break;
                case KeyviewerStyle.Key10:
                    for (int i = 0; i < 8; i++)
                        SetKeyPosition(i, baseX + 54 * i, baseY + 279 - remove);
                    SetKeyPosition(8, baseX + 81, baseY + 225 - remove);
                    SetKeyPosition(9, baseX + 54 * 4, baseY + 225 - remove);
                    SetKeyPosition(-1, baseX + 0, baseY + 225 - remove);
                    SetKeyPosition(-2, baseX + 81 + 54 * 5, baseY + 225 - remove);
                    break;
            }
        }

        private void ResetFootKeyViewerPosition()
        {
            if (Keys == null || !Settings.CustomPositionEnabled) return;
            Vector2 norm = Settings.FootKeyViewerPosition;
            // Convert normalized (X: 0=left 1=right, Y: 0=top 1=bottom) to reference pixel offsets from bottom-left.
            // Foot keys are slim (h=30, half=15); offset so Y=1 aligns bottom edge with screen edge.
            int size = 0;
            switch (Settings.FootKeyViewerStyle)
            {
                case FootKeyviewerStyle.Key2: size = 2; break;
                case FootKeyviewerStyle.Key4: size = 4; break;
                case FootKeyviewerStyle.Key6: size = 6; break;
                case FootKeyviewerStyle.Key8: size = 8; break;
                case FootKeyviewerStyle.Key10: size = 10; break;
                case FootKeyviewerStyle.Key12: size = 12; break;
                case FootKeyviewerStyle.Key14: size = 14; break;
                case FootKeyviewerStyle.Key16: size = 16; break;
                default: return;
            }
            float r = GetFootLayoutRightmostOffset(size);
            float baseX = norm.x * (1920f - r);
            // Y: lerp so Y=0 = top edge at screen top, Y=1 = bottom edge at screen bottom
            // Single row (size≤8): top edge at baseY+15. Two rows: top+34, top edge at baseY+49.
            float footTopOffset = size <= 8 ? 15f : 49f;
            float topBaseY = 1080f - footTopOffset;
            float bottomBaseY = 15f; // lowest key center at baseY, bottom edge = baseY-15 → baseY=15
            float baseY = Mathf.Lerp(bottomBaseY, topBaseY, 1f - norm.y);
            int firstRowCount = size <= 8 ? size : 8;
            float yBase = size > 8 ? baseY + 34 : baseY;
            for (int i = 20; i < 20 + size; i++)
            {
                int offset = i - 20;
                if (offset < firstRowCount)
                {
                    SetKeyPosition(i, baseX + offset * 34, yBase);
                }
                else
                {
                    int col = offset - firstRowCount;
                    float x = baseX + col * 34 + (firstRowCount - (size - firstRowCount)) * 17;
                    SetKeyPosition(i, x, yBase - 34);
                }
            }
        }

        /// <summary>
        /// Horizontal centering offset: shifts the layout so it stays centered
        /// on non-16:9 screens when matchWidthOrHeight = 1 (match height).
        /// Equals 0 on native 1920x1080, positive on wider screens, negative on narrower.
        /// Expressed in reference-resolution (canvas) coordinates.
        /// </summary>
        private float ScreenCenterOffsetX =>
            Screen.width * 1080f / (2f * Screen.height) - 960f;

        private void SetKeyPosition(int keyIndex, float x, float y)
        {
            float cx = x + ScreenCenterOffsetX;
            if (keyIndex == -1 && Kps != null)
            {
                Kps.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(cx, y);
            }
            else if (keyIndex == -2 && Total != null)
            {
                Total.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(cx, y);
            }
            else if (keyIndex >= 0 && keyIndex < Keys.Length && Keys[keyIndex] != null)
            {
                Keys[keyIndex].transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(cx, y);
            }
        }

        private Color GetColorByIndex(int index)
        {
            return index switch
            {
                0 => Settings.Background,
                1 => Settings.BackgroundClicked,
                2 => Settings.Outline,
                3 => Settings.OutlineClicked,
                4 => Settings.Text,
                5 => Settings.TextClicked,
                6 => Settings.RainColor,
                7 => Settings.RainColor2,
                8 => Settings.RainColor3,
                _ => Color.white
            };
        }

        private void SetColorByIndex(int index, Color color)
        {
            switch (index)
            {
                case 0: Settings.Background = color; break;
                case 1: Settings.BackgroundClicked = color; break;
                case 2: Settings.Outline = color; break;
                case 3: Settings.OutlineClicked = color; break;
                case 4: Settings.Text = color; break;
                case 5: Settings.TextClicked = color; break;
                case 6: Settings.RainColor = color; break;
                case 7: Settings.RainColor2 = color; break;
                case 8: Settings.RainColor3 = color; break;
            }
        }

        private void UpdateAllKeyColors()
        {
            if (Keys == null) return;
            KeyCode[] keyCodes = GetKeyCode();
            KeyCode[] footKeyCodes = GetFootKeyCode();
            for (int i = 0; i < keyCodes.Length && i < Keys.Length; i++)
            {
                if (Keys[i] != null)
                {
                    Keys[i].background.color = Settings.Background;
                    Keys[i].outline.color = Settings.Outline;
                    Keys[i].text.color = Settings.Text;
                    if (Keys[i].value != null) Keys[i].value.color = Settings.Text;
                }
            }
            if (footKeyCodes != null)
            {
                for (int i = 0; i < footKeyCodes.Length; i++)
                {
                    int index = i + 20;
                    if (index < Keys.Length && Keys[index] != null)
                    {
                        Keys[index].background.color = Settings.Background;
                        Keys[index].outline.color = Settings.Outline;
                        Keys[index].text.color = Settings.Text;
                        if (Keys[index].value != null) Keys[index].value.color = Settings.Text;
                    }
                }
            }
            if (Kps != null)
            {
                Kps.background.color = Settings.Background;
                Kps.outline.color = Settings.Outline;
                Kps.text.color = Settings.Text;
                if (Kps.value != null) Kps.value.color = Settings.Text;
            }
            if (Total != null)
            {
                Total.background.color = Settings.Background;
                Total.outline.color = Settings.Outline;
                Total.text.color = Settings.Text;
                if (Total.value != null) Total.value.color = Settings.Text;
            }
        }

        private void ChangeKeyViewer()
        {
            currentKeyViewerStyle = Settings.KeyViewerStyle;
            ResetKeyViewer();
        }

        private void ResetKeyViewer()
        {
            SelectedKey = -1;
            if (Keys != null)
            {
                for (int i = 0; i < 20; i++)
                {
                    if (Keys[i] != null && Keys[i].gameObject != null)
                    {
                        Object.DestroyImmediate(Keys[i].gameObject);
                    }
                }
                if (Total != null && Total.gameObject != null)
                {
                    Object.DestroyImmediate(Total.gameObject);
                }
                if (Kps != null && Kps.gameObject != null)
                {
                    Object.DestroyImmediate(Kps.gameObject);
                }
            }
            switch (Settings.KeyViewerStyle)
            {
                case KeyviewerStyle.Key12:
                    Initialize12KeyViewer();
                    break;
                case KeyviewerStyle.Key16:
                    Initialize16KeyViewer();
                    break;
                case KeyviewerStyle.Key20:
                    Initialize20KeyViewer();
                    break;
                case KeyviewerStyle.Key8:
                    Initialize8KeyViewer();
                    break;
                case KeyviewerStyle.Key10:
                    Initialize10KeyViewer();
                    break;
            }
            if (Settings.CustomPositionEnabled)
            {
                ResetKeyViewerPosition();
            }
        }

        private void ResetFootKeyViewer()
        {
            if (Keys != null)
            {
                for (int i = 20; i < 36; i++)
                {
                    if (Keys[i] != null && Keys[i].gameObject != null)
                    {
                        Object.DestroyImmediate(Keys[i].gameObject);
                    }
                }
            }
            switch (Settings.FootKeyViewerStyle)
            {
                case FootKeyviewerStyle.Key2:
                    InitializeFootKeyViewer(2);
                    break;
                case FootKeyviewerStyle.Key4:
                    InitializeFootKeyViewer(4);
                    break;
                case FootKeyviewerStyle.Key6:
                    InitializeFootKeyViewer(6);
                    break;
                case FootKeyviewerStyle.Key8:
                    InitializeFootKeyViewer(8);
                    break;
                case FootKeyviewerStyle.Key10:
                    InitializeFootKeyViewer(10);
                    break;
                case FootKeyviewerStyle.Key12:
                    InitializeFootKeyViewer(12);
                    break;
                case FootKeyviewerStyle.Key14:
                    InitializeFootKeyViewer(14);
                    break;
                case FootKeyviewerStyle.Key16:
                    InitializeFootKeyViewer(16);
                    break;
            }
            if (Settings.CustomPositionEnabled)
            {
                ResetFootKeyViewerPosition();
            }
        }

        private static KeyCode[] GetKeyCode()
        {
            return Settings.KeyViewerStyle switch
            {
                KeyviewerStyle.Key8 => Settings.key8,
                KeyviewerStyle.Key12 => Settings.key12,
                KeyviewerStyle.Key16 => Settings.key16,
                KeyviewerStyle.Key20 => Settings.key20,
                KeyviewerStyle.Key10 => Settings.key10,
                _ => Settings.key16
            };
        }

        private static KeyCode[] GetFootKeyCode()
        {
            return Settings.FootKeyViewerStyle switch
            {
                FootKeyviewerStyle.Key2 => Settings.footkey2,
                FootKeyviewerStyle.Key4 => Settings.footkey4,
                FootKeyviewerStyle.Key6 => Settings.footkey6,
                FootKeyviewerStyle.Key8 => Settings.footkey8,
                FootKeyviewerStyle.Key10 => Settings.footkey10,
                FootKeyviewerStyle.Key12 => Settings.footkey12,
                FootKeyviewerStyle.Key14 => Settings.footkey14,
                FootKeyviewerStyle.Key16 => Settings.footkey16,
                _ => new KeyCode[0]
            };
        }

        private static string[] GetKeyText()
        {
            return Settings.KeyViewerStyle switch
            {
                KeyviewerStyle.Key8 => Settings.key8Text,
                KeyviewerStyle.Key12 => Settings.key12Text,
                KeyviewerStyle.Key16 => Settings.key16Text,
                KeyviewerStyle.Key20 => Settings.key20Text,
                KeyviewerStyle.Key10 => Settings.key10Text,
                _ => Settings.key16Text
            };
        }

        private static byte[] GetBackSequence()
        {
            return Settings.KeyViewerStyle switch
            {
                KeyviewerStyle.Key8 => BackSequence8,
                KeyviewerStyle.Key12 => BackSequence12,
                KeyviewerStyle.Key16 => BackSequence16,
                KeyviewerStyle.Key20 => BackSequence20,
                KeyviewerStyle.Key10 => BackSequence10,
                _ => BackSequence16
            };
        }
    }
}
