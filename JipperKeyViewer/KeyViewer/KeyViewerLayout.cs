using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>
    /// Layout and positioning: creating, initializing, positioning key elements / 布局和定位：创建、初始化、定位按键元素
    /// </summary>
    public partial class KeyViewer : MonoBehaviour
    {
        /// <summary>
        /// Create the canvas overlay and initialize all keys / 创建画布覆盖层并初始化所有按键
        /// Called when the mod is toggled on or when settings require rebuilding / 在 Mod 打开或设置需要重建时调用
        /// </summary>
        private void EnableKeyViewer()
        {
            if (KeyViewerObject != null || !Settings.Enabled) return;
            if (!TryLoadResources())
            {
                Main.Mod.Logger.Error("KeyViewer: Cannot load AssetBundle, please check assets/ directory");
                return;
            }
            // Create ScreenSpaceOverlay canvas (independent of game UI) / 创建 ScreenSpaceOverlay 画布（独立于游戏 UI）
            KeyViewerObject = new GameObject("JipperResourcePack KeyViewer");
            Canvas = KeyViewerObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = Canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f; // Match height so vertical positions are resolution-independent / 匹配高度使垂直位置不受分辨率影响
            Canvas.gameObject.AddComponent<GraphicRaycaster>();
            // SizeObject applies the Size scale and serves as parent for all keys / SizeObject 应用大小缩放并作为所有按键的父级
            KeyViewerSizeObject = new GameObject("SizeObject");
            RectTransform rectTransform = KeyViewerSizeObject.AddComponent<RectTransform>();
            rectTransform.SetParent(KeyViewerObject.transform);
            rectTransform.localScale = new Vector3(Settings.Size, Settings.Size, 1);
            // Fill full canvas with bottom-left pivot so localScale doesn't shift child positions / 填满画布，左下角轴心，使缩放不改变子元素位置
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = Vector2.zero;
            rectTransform.offsetMin = rectTransform.offsetMax = Vector2.zero;
            // Initialize main keys based on selected layout / 根据选中的布局初始化主按键
            Keys = new Key[36];
            switch (Settings.KeyViewerStyle)
            {
                case KeyviewerStyle.Key12: Initialize12KeyViewer(); break;
                case KeyviewerStyle.Key16: Initialize16KeyViewer(); break;
                case KeyviewerStyle.Key20: Initialize20KeyViewer(); break;
                case KeyviewerStyle.Key8:  Initialize8KeyViewer();  break;
                case KeyviewerStyle.Key10: Initialize10KeyViewer(); break;
            }
            // Initialize foot keys based on selected layout / 根据选中的布局初始化脚键
            switch (Settings.FootKeyViewerStyle)
            {
                case FootKeyviewerStyle.Key2:  InitializeFootKeyViewer(2);  break;
                case FootKeyviewerStyle.Key4:  InitializeFootKeyViewer(4);  break;
                case FootKeyviewerStyle.Key6:  InitializeFootKeyViewer(6);  break;
                case FootKeyviewerStyle.Key8:  InitializeFootKeyViewer(8);  break;
                case FootKeyviewerStyle.Key10: InitializeFootKeyViewer(10); break;
                case FootKeyviewerStyle.Key12: InitializeFootKeyViewer(12); break;
                case FootKeyviewerStyle.Key14: InitializeFootKeyViewer(14); break;
                case FootKeyviewerStyle.Key16: InitializeFootKeyViewer(16); break;
            }
            // Persist the overlay across scene loads / 使覆盖层在场景加载中持久化
            Object.DontDestroyOnLoad(KeyViewerObject);
            PressTimes = new Queue<long>();
            Stopwatch = System.Diagnostics.Stopwatch.StartNew();
        }

        /// <summary>
        /// Destroy the canvas overlay and clean up all resources / 销毁画布覆盖层并清理所有资源
        /// </summary>
        private void DisableKeyViewer()
        {
            if (KeyViewerObject == null) return;
            Object.Destroy(KeyViewerObject);
            KeyViewerObject = null;
            KeyViewerSizeObject = null;
            // Return all rain objects to pool / 将所有雨滴对象归还池
            while (rainPool.Count > 0) Object.Destroy(rainPool.Pop().gameObject);
            activeRains.Clear();
            // Destroy shadow materials / 销毁阴影材质
            foreach (var mat in shadowMaterials.Values)
                Object.Destroy(mat);
            shadowMaterials.Clear();
            Canvas = null;
            Keys = null;
            PressTimes = null;
            Stopwatch = null;
            lastFrameMs = 0;
        }

        /// <summary>
        /// Check each main key and foot key for state changes (press/release) every frame / 每帧检查每个主键和脚键的状态变化（按下/释放）
        /// </summary>
        private void ProcessMainAndFootKeysInUpdate(long elapsedMilliseconds)
        {
            // Lazy cache refresh for key arrays (avoid redundant GetKeyCode/GetFootKeyCode calls) / 懒缓存刷新按键数组（避免重复调用）
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
            // Update total count display if changed / 总计数变化时更新显示
            if (Total != null && Total.value != null && lastTotal != Settings.TotalCount)
            {
                lastTotal = Settings.TotalCount;
                Total.value.text = lastTotal.ToString();
            }
        }

        /// <summary>
        /// Process rain queues: assign Rain components from pool to pending RawRain data / 处理雨滴队列：从对象池分配 Rain 组件给待处理的 RawRain 数据
        /// </summary>
        private void ProcessKeyRainQueues()
        {
            if (Keys == null) return;
            for (int i = 0; i < Keys.Length; i++)
            {
                if (Keys[i] != null)
                    Keys[i].ProcessRainQueue();
            }
        }

        /// <summary>
        /// Process a group of keys for input state changes / 处理一组按键的输入状态变化
        /// </summary>
        /// <param name="keyCodes">Key bindings for this group / 该组的按键绑定</param>
        /// <param name="baseIndex">Starting index in the Keys array / 在 Keys 数组中的起始索引</param>
        /// <param name="elapsedMs">Current elapsed time for KPS tracking / 当前经过时间，用于 KPS 跟踪</param>
        private void ProcessKeyGroup(KeyCode[] keyCodes, int baseIndex, long elapsedMs)
        {
            for (int i = 0; i < keyCodes.Length; i++)
            {
                int idx = baseIndex + i;
                if (idx >= Keys.Length || Keys[idx] == null) continue;
                bool current = Input.GetKey(keyCodes[i]);
                if (current != Keys[idx].isPressed)
                {
                    UpdateKeyColors(idx, current);
                    Keys[idx].isPressed = current;
                    if (current)
                    {
                        // Increment counter and update display / 递增计数并更新显示
                        Settings.Count[idx]++;
                        Settings.TotalCount++;
                        if (Keys[idx].value != null)
                            Keys[idx].value.text = Settings.Count[idx].ToString();
                        PressTimes.Enqueue(elapsedMs);
                        // Trigger rain effect on key press / 按键按下时触发雨滴效果
                        if (Settings.EnableRainEffect)
                            TriggerRainEffect(idx, Keys[idx]);
                    }
                }
            }
        }

        /// <summary>
        /// Calculate KPS by removing presses older than 1 second / 通过移除超过 1 秒的按下记录计算 KPS
        /// </summary>
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

        /// <summary>
        /// Update key visual colors based on press state / 根据按下状态更新按键视觉颜色
        /// </summary>
        private void UpdateKeyColors(int i, bool pressed)
        {
            if (Keys == null || i >= Keys.Length || Keys[i] == null) return;
            Key key = Keys[i];
            key.background.color = pressed ? Settings.BackgroundClicked : Settings.Background;
            key.outline.color = pressed ? Settings.OutlineClicked : Settings.Outline;
            key.text.color = pressed ? Settings.TextClicked : Settings.Text;
            if (key.value != null) key.value.color = key.text.color;
        }

        /// <summary>
        /// Initialize 12-key layout (8 front + 4 back) / 初始化 12 键布局（8 前排 + 4 后排）
        /// </summary>
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

        /// <summary>
        /// Initialize 16-key layout (8 front + 8 back) / 初始化 16 键布局（8 前排 + 8 后排）
        /// </summary>
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

        /// <summary>
        /// Initialize 20-key layout (8 front + 8 back + 4 third row) / 初始化 20 键布局（8 前排 + 8 后排 + 4 第三排）
        /// </summary>
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

        /// <summary>
        /// Initialize 8-key layout (single row) / 初始化 8 键布局（单排）
        /// </summary>
        private void Initialize8KeyViewer()
        {
            int remove = Settings.DownLocation ? 200 : 0;
            for (int i = 0; i < 8; i++) Keys[i] = CreateKey(i, 54 * i, 279 - remove, 50, 0);
            Kps = CreateKey(-1, 0, 233 - remove, 212, -1, true);
            Total = CreateKey(-2, 216, 233 - remove, 212, -1, true);
        }

        /// <summary>
        /// Initialize 10-key layout (8 front + 2 back) / 初始化 10 键布局（8 前排 + 2 后排）
        /// </summary>
        private void Initialize10KeyViewer()
        {
            int remove = Settings.DownLocation ? 200 : 0;
            for (int i = 0; i < 8; i++) Keys[i] = CreateKey(i, 54 * i, 279 - remove, 50, 0);
            Keys[8] = CreateKey(8, 81, 225 - remove, 129, 1);
            Keys[9] = CreateKey(9, 54 * 4, 225 - remove, 129, 1);
            Kps = CreateKey(-1, 0, 225 - remove, 77, -1);
            Total = CreateKey(-2, 81 + 54 * 5, 225 - remove, 77, -1);
        }

        /// <summary>
        /// Initialize foot keys starting at Keys[20] / 初始化从 Keys[20] 开始的脚键
        /// Supports 2-16 keys, automatically arranging in 1 or 2 rows / 支持 2-16 个键，自动排列为 1 或 2 排
        /// </summary>
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
                // Center the second row under the first when not full / 第二排不满时居中于第一排下方
                if (size > 8 && row == 1)
                    x += (8 - (size - 8)) * 17;
                int y = baseY - row * 34;
                Keys[i] = CreateKey(i, x, y, 30, -1, true, false);
            }
        }

        /// <summary>
        /// Create a single key GameObject with background, outline, text, count, and optional rain container / 创建单个按键 GameObject，包含背景、轮廓、文本、计数和可选的雨滴容器
        /// </summary>
        /// <param name="i">Key index (-1=KPS, -2=Total, 0-35=keys) / 按键索引（-1=KPS，-2=Total，0-35=按键）</param>
        /// <param name="x">X position in canvas reference coordinates / 画布参考坐标系中的 X 位置</param>
        /// <param name="y">Y position in canvas reference coordinates / 画布参考坐标系中的 Y 位置</param>
        /// <param name="sizeX">Key width / 按键宽度</param>
        /// <param name="raining">Rain row index (-1=no rain, 0=row1, 1=row2, 3=row3) / 雨滴行索引（-1=无雨滴，0=第1排，1=第2排，3=第3排）</param>
        /// <param name="slim">Use slim style (for KPS/Total display) / 使用窄样式（用于 KPS/Total 显示）</param>
        /// <param name="count">Show press count text / 显示按下计数文本</param>
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
            // Press count text / 按键计数文本
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
                text.color = settings.Text;
                key.value = text;
            }
            // Set initial text content / 设置初始文本内容
            UpdateKeyText(key, i);
            // Rain effect container (one per key, shared across all rain drops) / 雨滴效果容器（每个按键一个，所有雨滴共享）
            if (raining >= 0)
            {
                if (key.rain == null)
                {
                    key.rain = new GameObject("RainLine");
                    transform = key.rain.AddComponent<RectTransform>();
                    transform.SetParent(obj.transform);
                    transform.sizeDelta = new Vector2(sizeX, 275);
                    transform.anchorMin = transform.anchorMax = transform.pivot = Vector2.zero;
                    // Position rain container below the key / 将雨滴容器放在按键下方
                    transform.anchoredPosition = new Vector2(0, raining switch
                    {
                        0 => -223,  // Row 1 offset / 第1排偏移
                        3 => -115,  // Row 3 offset / 第3排偏移
                        _ => -169   // Row 2 offset / 第2排偏移
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

        /// <summary>
        /// Set the display text for a key based on its index and current bindings / 根据按键索引和当前绑定设置显示文本
        /// Special indices: -1=KPS, -2=Total / 特殊索引：-1=KPS，-2=Total
        /// </summary>
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
        /// Lowest key bottom edge Y for normalized positioning / 归一化定位中最低按键底边的 Y 值
        /// </summary>
        private float GetMinMainKeyOffset()
        {
            bool dl = Settings.DownLocation;
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

        /// <summary>Total width of the main key layout in reference pixels / 主按键布局的总宽度（参考像素）</summary>
        private float GetMainLayoutRightmostOffset() => 428f;

        /// <summary>Topmost key top edge Y for normalized positioning / 归一化定位中最顶部按键顶边的 Y 值</summary>
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

        /// <summary>Width of the foot key section in reference pixels / 脚键区域的宽度（参考像素）</summary>
        private float GetFootLayoutRightmostOffset(int size)
        {
            int row0Cols = Mathf.Min(size, 8);
            return (row0Cols - 1) * 34 + 30;
        }

        /// <summary>
        /// Reposition main keys based on normalized (0-1) custom position / 基于归一化（0-1）自定义位置重新定位主按键
        /// Maps (0,0) to screen top-left and (1,1) to screen bottom-right / (0,0) 映射到屏幕左上角，(1,1) 映射到屏幕右下角
        /// </summary>
        private void ResetKeyViewerPosition()
        {
            if (Keys == null || !Settings.CustomPositionEnabled) return;
            Vector2 norm = Settings.MainKeyViewerPosition;
            // Convert normalized (X: 0=left 1=right, Y: 0=top 1=bottom) to reference pixel offsets from bottom-left.
            // X: interpolate so X=0 = left edge at screen left, X=1 = right edge at screen right.
            // Y: subtract min layout offset so Y=1 puts the lowest key's bottom edge at screen bottom.
            float r = GetMainLayoutRightmostOffset();
            float baseX = norm.x * (CanvasWidth - r);
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
            float baseX = norm.x * (CanvasWidth - r);
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

        private float CanvasWidth => Screen.width * 1080f / Screen.height;

        private float ScreenCenterOffsetX => 0f;

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

        /// <summary>Get color setting by numeric index (0-8) for the color picker / 通过数字索引（0-8）获取颜色设置，用于颜色选择器</summary>
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

        /// <summary>Set color setting by numeric index (0-8) from the color picker / 通过数字索引（0-8）从颜色选择器设置颜色</summary>
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

        /// <summary>
        /// Apply current color settings to all key elements / 将当前颜色设置应用到所有按键元素
        /// </summary>
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

        /// <summary>
        /// Handle main key layout change / 处理主按键布局变化
        /// </summary>
        private void ChangeKeyViewer()
        {
            currentKeyViewerStyle = Settings.KeyViewerStyle;
            ResetKeyViewer();
        }

        /// <summary>
        /// Destroy and recreate main keys (for layout/style changes) / 销毁并重建主按键（用于布局/样式变化）
        /// </summary>
        private void ResetKeyViewer()
        {
            SelectedKey = -1;
            if (Keys != null)
            {
                for (int i = 0; i < 20; i++)
                {
                    if (Keys[i] != null && Keys[i].gameObject != null)
                        Object.DestroyImmediate(Keys[i].gameObject);
                }
                if (Total != null && Total.gameObject != null)
                    Object.DestroyImmediate(Total.gameObject);
                if (Kps != null && Kps.gameObject != null)
                    Object.DestroyImmediate(Kps.gameObject);
            }
            while (rainPool.Count > 0) Object.Destroy(rainPool.Pop().gameObject);
            activeRains.Clear();
            switch (Settings.KeyViewerStyle)
            {
                case KeyviewerStyle.Key12: Initialize12KeyViewer(); break;
                case KeyviewerStyle.Key16: Initialize16KeyViewer(); break;
                case KeyviewerStyle.Key20: Initialize20KeyViewer(); break;
                case KeyviewerStyle.Key8:  Initialize8KeyViewer();  break;
                case KeyviewerStyle.Key10: Initialize10KeyViewer(); break;
            }
            if (Settings.CustomPositionEnabled)
                ResetKeyViewerPosition();
        }

        /// <summary>
        /// Destroy and recreate foot keys (for layout/style changes) / 销毁并重建脚键（用于布局/样式变化）
        /// </summary>
        private void ResetFootKeyViewer()
        {
            if (Keys != null)
            {
                for (int i = 20; i < 36; i++)
                {
                    if (Keys[i] != null && Keys[i].gameObject != null)
                        Object.DestroyImmediate(Keys[i].gameObject);
                }
            }
            while (rainPool.Count > 0) Object.Destroy(rainPool.Pop().gameObject);
            activeRains.Clear();
            switch (Settings.FootKeyViewerStyle)
            {
                case FootKeyviewerStyle.Key2:  InitializeFootKeyViewer(2);  break;
                case FootKeyviewerStyle.Key4:  InitializeFootKeyViewer(4);  break;
                case FootKeyviewerStyle.Key6:  InitializeFootKeyViewer(6);  break;
                case FootKeyviewerStyle.Key8:  InitializeFootKeyViewer(8);  break;
                case FootKeyviewerStyle.Key10: InitializeFootKeyViewer(10); break;
                case FootKeyviewerStyle.Key12: InitializeFootKeyViewer(12); break;
                case FootKeyviewerStyle.Key14: InitializeFootKeyViewer(14); break;
                case FootKeyviewerStyle.Key16: InitializeFootKeyViewer(16); break;
            }
            if (Settings.CustomPositionEnabled)
                ResetFootKeyViewerPosition();
        }

        /// <summary>
        /// Get the key code array for the current main layout / 获取当前主布局的按键代码数组
        /// </summary>
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

        /// <summary>
        /// Get the foot key code array for the current foot layout / 获取当前脚键布局的按键代码数组
        /// </summary>
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

        /// <summary>
        /// Get the custom text labels for the current main layout / 获取当前主布局的自定义文本标签
        /// </summary>
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

        /// <summary>
        /// Get the back-row index mapping for the current main layout / 获取当前主布局的后排索引映射
        /// </summary>
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
