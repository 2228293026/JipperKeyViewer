using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    public static class I18n
    {
        public static string Lang { get; set; } = "en";

        static Dictionary<string, string> en = new()
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
        };

        static Dictionary<string, string> zh = new()
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
        };

        static string FilePath
        {
            get
            {
                string modPath = Path.GetDirectoryName(Main.Mod?.Path);
                return Path.Combine(modPath ?? ".", "lang", "lang.json");
            }
        }

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
                // Simple manual parse: extract key/en/zh from each object
                var matches = Regex.Matches(json, "\"key\"\\s*:\\s*\"([^\"]+)\"\\s*,\\s*\"en\"\\s*:\\s*\"([^\"]+)\"\\s*,\\s*\"zh\"\\s*:\\s*\"([^\"]+)\"");
                foreach (Match m in matches)
                {
                    if (m.Groups.Count == 4)
                    {
                        string k = m.Groups[1].Value;
                        string e = m.Groups[2].Value;
                        string z = m.Groups[3].Value;
                        if (!string.IsNullOrEmpty(k))
                        {
                            en[k] = e;
                            zh[k] = z;
                        }
                    }
                }
                Debug.Log($"I18n: loaded {matches.Count} entries from lang.json");
            }
            catch (Exception e)
            {
                Debug.LogError($"I18n: failed to parse lang.json: {e.Message}");
            }
        }

        static Dictionary<string, string> current => Lang == "en" ? en : zh;

        public static string Tr(string key)
        {
            return current.TryGetValue(key, out var val) ? val : key;
        }
    }
}
