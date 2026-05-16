// Internationalization (i18n) system / 国际化（i18n）系统
// Supports English and Chinese with optional external lang.json override / 支持英文和中文，可选外部 lang.json 覆盖

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    /// <summary>
    /// Static i18n helper providing translated strings / 静态国际化辅助类，提供翻译字符串
    /// Loads default translations from hardcoded dictionaries, then attempts to merge from lang.json / 从硬编码字典加载默认翻译，然后尝试从 lang.json 合并
    /// </summary>
    public static class I18n
    {
        /// <summary>Current language code ("en" or "zh") / 当前语言代码</summary>
        public static string Lang { get; set; } = "en";

        /// <summary>English translations dictionary / 英文字典</summary>
        static readonly Dictionary<string, string> en = new()
        {
            ["key_display_on"] = "Key Display ON",
            ["font_style"] = "Font Style",
            ["place_below"] = "Place Below",
            ["custom_pos"] = "Custom Position",
            ["main_key_pos"] = "Main Key Position",
            ["foot_key_pos"] = "Foot Key Position",
            ["reset_pos"] = "Reset Position",
            ["key_layout"] = "Key Layout",
            ["foot_keys"] = "Foot Keys",
            ["size"] = "Size",
            ["rain_effect"] = "Enable Rain Effect",
            ["rain_rows"] = "Rain Rows",
            ["rain_row1"] = "Row 1",
            ["rain_row2"] = "Row 2",
            ["rain_row3"] = "Row 3",
            ["rain_height"] = "Rain Height",
            ["rain_speed"] = "Rain Speed",
            ["rain_fade"] = "Rain Fade-out",

            ["key_change"] = "Key Change",
            ["text_change"] = "Text Change",
            ["colors"] = "Colors",
            ["save"] = "Save Settings",
            ["row1_keys"] = "Row 1 Keys",
            ["row2_keys"] = "Row 2 Keys",
            ["row3_keys"] = "Row 3 Keys",
            ["foot_keys_list"] = "Foot Keys",
            ["press_new_key"] = "Press a new key...",
            ["row1_text"] = "Row 1 Text",
            ["row2_text"] = "Row 2 Text",
            ["row3_text"] = "Row 3 Text",
            ["input_text"] = "Input Text",
            ["reset"] = "Reset",
            ["save_btn"] = "Save",
            ["color_bg"] = "Background",
            ["color_bg_clicked"] = "Background (Pressed)",
            ["color_outline"] = "Outline",
            ["color_outline_clicked"] = "Outline (Pressed)",
            ["color_text"] = "Text",
            ["color_text_clicked"] = "Text (Pressed)",
            ["color_rain1"] = "Rain Color Row 1",
            ["color_rain2"] = "Rain Color Row 2",
            ["color_rain3"] = "Rain Color Row 3",
            ["preview"] = "Preview",
            ["reset_default"] = "Reset",
            ["language"] = "Language",
            ["no_fonts_found"] = "(No fonts available)",
            ["custom_font_tip"] = "Custom Fonts: Put .ttf/.otf files into the CustomFont folder, then restart the game.",
            ["reset_counts"] = "Reset Counts",
            ["count_formatting"] = "Use 1,234 for large numbers",
            ["foot_keys_text"] = "Foot Key Text",
            ["hide_main_count"] = "Hide Main Key Count",
            ["per_key_colors"] = "Per-Key Colors",
            ["per_key_color_reset"] = "Reset All to Default",
            ["auto_rainbow"] = "Auto Rainbow KV",
        };

        /// <summary>Chinese translations dictionary / 中文字典</summary>
        static readonly Dictionary<string, string> zh = new()
        {
            ["key_display_on"] = "按键显示已开启",
            ["font_style"] = "字体样式",
            ["place_below"] = "放在下方",
            ["custom_pos"] = "自定义位置",
            ["main_key_pos"] = "主按键位置",
            ["foot_key_pos"] = "脚键位置",
            ["reset_pos"] = "重置位置到默认",
            ["key_layout"] = "按键布局",
            ["foot_keys"] = "脚键",
            ["size"] = "大小",
            ["rain_effect"] = "启用雨线效果",
            ["rain_rows"] = "启用雨线的行",
            ["rain_row1"] = "第1排",
            ["rain_row2"] = "第2排",
            ["rain_row3"] = "第3排",
            ["rain_height"] = "雨滴高度",
            ["rain_speed"] = "雨线速度",
            ["rain_fade"] = "雨滴松开淡出",

            ["key_change"] = "按键更改",
            ["text_change"] = "文本更改",
            ["colors"] = "颜色",
            ["save"] = "保存设置",
            ["row1_keys"] = "第1排按键",
            ["row2_keys"] = "第2排按键",
            ["row3_keys"] = "第3排按键",
            ["foot_keys_list"] = "脚键",
            ["press_new_key"] = "请按下新的按键...",
            ["row1_text"] = "第1排按键文本",
            ["row2_text"] = "第2排按键文本",
            ["row3_text"] = "第3排按键文本",
            ["input_text"] = "输入文本",
            ["reset"] = "重置",
            ["save_btn"] = "保存",
            ["color_bg"] = "背景颜色",
            ["color_bg_clicked"] = "按下时背景颜色",
            ["color_outline"] = "轮廓颜色",
            ["color_outline_clicked"] = "按下时轮廓颜色",
            ["color_text"] = "文本颜色",
            ["color_text_clicked"] = "按下时文本颜色",
            ["color_rain1"] = "第一排雨滴颜色",
            ["color_rain2"] = "第二排雨滴颜色",
            ["color_rain3"] = "第三排雨滴颜色",
            ["preview"] = "预览",
            ["reset_default"] = "重置默认",
            ["language"] = "语言",
            ["no_fonts_found"] = "（无可用字体）",
            ["custom_font_tip"] = "使用自定义字体 将 .ttf/.otf 字体文件放入 CustomFont 文件夹，重启游戏生效。",
            ["reset_counts"] = "重置计数",
            ["count_formatting"] = "大数字千分位显示 1,234",
            ["foot_keys_text"] = "脚键文本",
            ["hide_main_count"] = "隐藏主按键计数",
            ["per_key_colors"] = "每键独立颜色",
            ["per_key_color_reset"] = "全部重置为默认",
            ["auto_rainbow"] = "自动彩色KV",
        };

        /// <summary>Korean translations dictionary / 韩文字典</summary>
        static readonly Dictionary<string, string> ko = new()
        {
            ["key_display_on"] = "키 표시 켜짐",
            ["font_style"] = "글꼴 스타일",
            ["place_below"] = "아래에 배치",
            ["custom_pos"] = "사용자 위치",
            ["main_key_pos"] = "메인 키 위치",
            ["foot_key_pos"] = "발판 키 위치",
            ["reset_pos"] = "위치 초기화",
            ["key_layout"] = "키 배치",
            ["foot_keys"] = "발판 키",
            ["size"] = "크기",
            ["rain_effect"] = "빗줄 효과 켜기",
            ["rain_rows"] = "빗줄 행",
            ["rain_row1"] = "1열",
            ["rain_row2"] = "2열",
            ["rain_row3"] = "3열",
            ["rain_height"] = "빗줄 높이",
            ["rain_speed"] = "빗줄 속도",
            ["rain_fade"] = "빗줄 페이드아웃",

            ["key_change"] = "키 변경",
            ["text_change"] = "텍스트 변경",
            ["colors"] = "색상",
            ["save"] = "설정 저장",
            ["row1_keys"] = "1열 키",
            ["row2_keys"] = "2열 키",
            ["row3_keys"] = "3열 키",
            ["foot_keys_list"] = "발판 키",
            ["press_new_key"] = "새 키를 누르세요...",
            ["row1_text"] = "1열 텍스트",
            ["row2_text"] = "2열 텍스트",
            ["row3_text"] = "3열 텍스트",
            ["input_text"] = "텍스트 입력",
            ["reset"] = "초기화",
            ["save_btn"] = "저장",
            ["color_bg"] = "배경",
            ["color_bg_clicked"] = "배경 (눌림)",
            ["color_outline"] = "테두리",
            ["color_outline_clicked"] = "테두리 (눌림)",
            ["color_text"] = "텍스트",
            ["color_text_clicked"] = "텍스트 (눌림)",
            ["color_rain1"] = "1열 빗줄 색상",
            ["color_rain2"] = "2열 빗줄 색상",
            ["color_rain3"] = "3열 빗줄 색상",
            ["preview"] = "미리보기",
            ["reset_default"] = "기본값으로 초기화",
            ["language"] = "언어",
            ["no_fonts_found"] = "(사용 가능한 글꼴 없음)",
            ["custom_font_tip"] = "사용자 글꼴: .ttf/.otf 파일을 CustomFont 폴더에 넣은 후 게임을 다시 시작하세요.",
            ["reset_counts"] = "카운트 초기화",
            ["count_formatting"] = "큰 숫자에 1,234 형식 사용",
            ["foot_keys_text"] = "발판 키 텍스트",
            ["hide_main_count"] = "메인 키 카운트 숨기기",
            ["per_key_colors"] = "키별 색상",
            ["per_key_color_reset"] = "모두 기본값으로 초기화",
            ["auto_rainbow"] = "자동 무지개 KV",
        };

        /// <summary>Path to the lang.json override file / lang.json 覆盖文件路径</summary>
        static string FilePath
        {
            get
            {
                string modPath = Path.GetDirectoryName(Main.Mod?.Path);
                return Path.Combine(modPath ?? ".", "lang", "lang.json");
            }
        }

        [Serializable]
        private class LangEntry
        {
            public string key = null;
            public string en = null;
            public string zh = null;
            public string ko = null;
        }

        [Serializable]
        private class LangFile
        {
            public LangEntry[] entries = null;
        }

        /// <summary>
        /// Load translations from lang.json if present, merging into the built-in dictionaries / 从 lang.json 加载翻译（如果存在），合并到内置字典中
        /// </summary>
        public static void Load()
        {
            string path = FilePath;
            if (!File.Exists(path))
            {
                Debug.Log($"I18n: no lang.json at {path}, using defaults");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var langFile = JsonUtility.FromJson<LangFile>(json);
                if (langFile?.entries == null)
                {
                    Debug.LogError("I18n: lang.json has no entries array");
                    return;
                }
                int count = 0;
                foreach (var entry in langFile.entries)
                {
                    if (string.IsNullOrEmpty(entry.key)) continue;
                    if (!string.IsNullOrEmpty(entry.en)) en[entry.key] = entry.en;
                    if (!string.IsNullOrEmpty(entry.zh)) zh[entry.key] = entry.zh;
                    if (!string.IsNullOrEmpty(entry.ko)) ko[entry.key] = entry.ko;
                    count++;
                }
                Debug.Log($"I18n: loaded {count} entries from lang.json");
            }
            catch (Exception e)
            {
                Debug.LogError($"I18n: failed to parse lang.json: {e.Message}");
            }
        }

        /// <summary>Returns the currently active translation dictionary / 返回当前活跃的翻译字典</summary>
        static Dictionary<string, string> current => Lang == "en" ? en : Lang == "zh" ? zh : ko;

        /// <summary>
        /// Translate a key string to the current language / 将键翻译为当前语言
        /// Returns the key itself if no translation is found / 如果找不到翻译则返回键本身
        /// </summary>
        public static string Tr(string key)
        {
            return current.TryGetValue(key, out var val) ? val : key;
        }
    }
}
