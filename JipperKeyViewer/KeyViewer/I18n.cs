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
            ["ghost_rain_color1"] = "Ghost Rain Color 1",
            ["ghost_rain_color2"] = "Ghost Rain Color 2",
            ["ghost_rain_color3"] = "Ghost Rain Color 3",
            ["rain_speed"] = "Rain Speed",
            ["rain_fade"] = "Release Fade",
            ["fade_duration"] = "Fade Duration",
            ["ghost_rain"] = "Ghost Rain",
            ["rain_gradient"] = "Edge Fade",
            ["gradient_percent"] = "Fade px",

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
            ["rain_width"] = "Rain Width",
            ["rain_width_row1"] = "Row 1 Width",
            ["rain_width_row2"] = "Row 2 Width",
            ["rain_width_row3"] = "Row 3 Width",
            ["preview"] = "Preview",
            ["reset_default"] = "Reset",
            ["language"] = "Language",
            ["no_fonts_found"] = "(No fonts available)",
            ["custom_font_tip"] = "Custom Fonts: Put .ttf/.otf files into the CustomFont folder, then restart the game.",
            ["reset_counts"] = "Reset Counts",
            ["count_formatting"] = "Use 1,234 for large numbers",
            ["foot_keys_text"] = "Foot Key Text",
            ["hide_main_count"] = "Hide Main Key Count",
            ["per_key_kps"] = "Per-Key KPS",
            ["per_key_colors"] = "Per-Key Colors",
            ["per_key_color_reset"] = "Reset All to Default",
            ["auto_rainbow"] = "Auto Rainbow KV",
            ["streamer_mode"] = "Hide KPS/Total",
            ["open_config_folder"] = "Open Config Folder",
            ["open_font_folder"] = "Open Font Folder",
            ["kps_colors"] = "KPS Colors",
            ["total_colors"] = "Total Colors",
            ["profile"] = "Profile",
            ["save_as"] = "Save As",
            ["rename"] = "Rename",
            ["delete"] = "Delete",

            ["rain_shadow"] = "Rain Shadow",
            ["rain_shadow_color"] = "Shadow Color",
            ["rain_shadow_offset"] = "Shadow Offset",
            ["rain_outline"] = "Rain Outline",
            ["rain_outline_color"] = "Outline Color",
            ["rain_outline_width"] = "Outline Width",
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
            ["fade_duration"] = "淡出时长",
            ["ghost_rain"] = "鬼键雨滴",
            ["ghost_rain_color1"] = "第一排鬼键雨滴颜色",
            ["ghost_rain_color2"] = "第二排鬼键雨滴颜色",
            ["ghost_rain_color3"] = "第三排鬼键雨滴颜色",
            ["rain_gradient"] = "顶部渐隐",
            ["gradient_percent"] = "渐隐像素",

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
            ["rain_width"] = "雨滴宽度",
            ["rain_width_row1"] = "第1排宽度",
            ["rain_width_row2"] = "第2排宽度",
            ["rain_width_row3"] = "第3排宽度",
            ["preview"] = "预览",
            ["reset_default"] = "重置默认",
            ["language"] = "语言",
            ["no_fonts_found"] = "（无可用字体）",
            ["custom_font_tip"] = "使用自定义字体 将 .ttf/.otf 字体文件放入 CustomFont 文件夹，重启游戏生效。",
            ["reset_counts"] = "重置计数",
            ["count_formatting"] = "大数字千分位显示 1,234",
            ["foot_keys_text"] = "脚键文本",
            ["hide_main_count"] = "隐藏主按键计数",
            ["per_key_kps"] = "每键KPS",
            ["per_key_colors"] = "每键独立颜色",
            ["per_key_color_reset"] = "全部重置为默认",
            ["auto_rainbow"] = "自动彩色KV",
            ["streamer_mode"] = "隐藏KPS/总计数",
            ["open_config_folder"] = "打开配置文件夹",
            ["open_font_folder"] = "打开字体文件夹",
            ["kps_colors"] = "KPS 颜色",
            ["total_colors"] = "Total 颜色",
            ["profile"] = "配置",
            ["save_as"] = "另存为",
            ["rename"] = "重命名",
            ["delete"] = "删除",

            ["rain_shadow"] = "雨滴阴影",
            ["rain_shadow_color"] = "阴影颜色",
            ["rain_shadow_offset"] = "阴影偏移",
            ["rain_outline"] = "雨滴描边",
            ["rain_outline_color"] = "描边颜色",
            ["rain_outline_width"] = "描边宽度",
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
            ["fade_duration"] = "페이드 시간",
            ["ghost_rain"] = "고스트 빗줄",
            ["ghost_rain_color1"] = "1열 고스트 빗줄 색상",
            ["ghost_rain_color2"] = "2열 고스트 빗줄 색상",
            ["ghost_rain_color3"] = "3열 고스트 빗줄 색상",
            ["rain_gradient"] = "끝 페이드",
            ["gradient_percent"] = "페이드 px",

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
            ["rain_width"] = "빗줄 너비",
            ["rain_width_row1"] = "1열 너비",
            ["rain_width_row2"] = "2열 너비",
            ["rain_width_row3"] = "3열 너비",
            ["preview"] = "미리보기",
            ["reset_default"] = "기본값으로 초기화",
            ["language"] = "언어",
            ["no_fonts_found"] = "(사용 가능한 글꼴 없음)",
            ["custom_font_tip"] = "사용자 글꼴: .ttf/.otf 파일을 CustomFont 폴더에 넣은 후 게임을 다시 시작하세요.",
            ["reset_counts"] = "카운트 초기화",
            ["count_formatting"] = "큰 숫자에 1,234 형식 사용",
            ["foot_keys_text"] = "발판 키 텍스트",
            ["hide_main_count"] = "메인 키 카운트 숨기기",
            ["per_key_kps"] = "키별 KPS",
            ["per_key_colors"] = "키별 색상",
            ["per_key_color_reset"] = "모두 기본값으로 초기화",
            ["auto_rainbow"] = "자동 무지개 KV",
            ["streamer_mode"] = "KPS/총계 숨기기",
            ["open_config_folder"] = "설정 폴더 열기",
            ["open_font_folder"] = "글꼴 폴더 열기",
            ["kps_colors"] = "KPS 색상",
            ["total_colors"] = "Total 색상",
            ["profile"] = "프로필",
            ["save_as"] = "다른 이름으로 저장",
            ["rename"] = "이름 바꾸기",
            ["delete"] = "삭제",

            ["rain_shadow"] = "빗줄 그림자",
            ["rain_shadow_color"] = "그림자 색상",
            ["rain_shadow_offset"] = "그림자 오프셋",
            ["rain_outline"] = "빗줄 아웃라인",
            ["rain_outline_color"] = "아웃라인 색상",
            ["rain_outline_width"] = "아웃라인 두께",
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

        /// <summary>
        /// Load translations from lang.json if present, merging into the built-in dictionaries / 从 lang.json 加载翻译（如果存在），合并到内置字典中
        /// </summary>
        public static void Load()
        {
            string path = FilePath;
            if (!File.Exists(path))
            {
                Main.Mod.Logger.Log($"I18n: no lang.json at {path}, using defaults");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                int count = ParseEntries(json);
                if (count < 0)
                {
                    Main.Mod.Logger.Error($"I18n: 'entries' array not found in lang.json");
                    return;
                }
                Main.Mod.Logger.Log($"I18n: loaded {count} entries from lang.json");
            }
            catch (Exception e)
            {
                Main.Mod.Logger.Error($"I18n: failed to parse lang.json: {e.Message}");
            }
        }

        private static int ParseEntries(string json)
        {
            int idx = json.IndexOf("\"entries\"", StringComparison.Ordinal);
            if (idx < 0) return -1;
            idx = json.IndexOf('[', idx);
            if (idx < 0) return -1;
            idx++;
            int count = 0;
            while (idx < json.Length)
            {
                char c = json[idx];
                if (c <= ' ') { idx++; continue; }
                if (c == ']') break;
                if (c != '{') { idx++; continue; }
                int end = SkipObject(json, idx);
                if (end < 0) break;
                if (ParseEntry(json, idx, end))
                    count++;
                idx = end + 1;
            }
            return count;
        }

        private static int SkipObject(string json, int start)
        {
            bool inStr = false;
            int depth = 0;
            for (int i = start; i < json.Length; i++)
            {
                char c = json[i];
                if (inStr)
                {
                    if (c == '\\') i++;
                    else if (c == '"') inStr = false;
                }
                else if (c == '"') inStr = true;
                else if (c == '{') depth++;
                else if (c == '}' && --depth == 0) return i;
            }
            return -1;
        }

        private static bool ParseEntry(string json, int start, int end)
        {
            string key = ExtractStr(json, start, end, "key");
            if (key == null) return false;
            string enVal = ExtractStr(json, start, end, "en");
            string zhVal = ExtractStr(json, start, end, "zh");
            string koVal = ExtractStr(json, start, end, "ko");
            if (!string.IsNullOrEmpty(enVal)) en[key] = enVal;
            if (!string.IsNullOrEmpty(zhVal)) zh[key] = zhVal;
            if (!string.IsNullOrEmpty(koVal)) ko[key] = koVal;
            return true;
        }

        private static string ExtractStr(string json, int start, int end, string key)
        {
            int idx = json.IndexOf("\"" + key + "\"", start, end - start, StringComparison.Ordinal);
            if (idx < 0) return null;
            idx = json.IndexOf('"', idx + key.Length + 2);
            if (idx < 0) return null;
            idx++;
            int valStart = idx;
            while (idx < end)
            {
                char c = json[idx];
                if (c == '\\') idx++;
                else if (c == '"') break;
                idx++;
            }
            if (idx >= end) return null;
            string val = json.Substring(valStart, idx - valStart);
            val = val.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n").Replace("\\t", "\t");
            return val;
        }

        /// <summary>Returns the currently active translation dictionary / 返回当前活跃的翻译字典</summary>
        static Dictionary<string, string> current => Lang == "ko" ? ko : Lang == "zh" ? zh : en;

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
