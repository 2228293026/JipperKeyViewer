# JipperKeyViewer
![C#](https://img.shields.io/badge/Lang-Csharp-c9c8e4.svg?&logo=c#)
![Visual Studio 2022](https://img.shields.io/badge/IDE-Visual%20Studio%202022-5C2D91?logo=visualstudio&logoColor=white)
[![Downloads](https://img.shields.io/github/downloads/2228293026/JipperKeyViewer/total)](https://github.com/2228293026/JipperKeyViewer/releases/latest)
[![Build](https://github.com/2228293026/JipperKeyViewer/actions/workflows/build.yml/badge.svg)](https://github.com/2228293026/JipperKeyViewer/actions/workflows/build.yml)

Keyboard overlay for **A Dance of Fire and Ice** — real-time key presses, KPS counter, and rain effects. Built with UnityModManager.

一款适用于 **冰与火之舞** 的按键显示 Mod，实时显示按键按下、KPS 统计和雨滴特效。

ADOFAI 키보드 오버레이 — 실시간 키 입력, KPS 카운터, 빗줄 효과를 표시합니다.

## Variants / 版本

| Variant | Description |
|---------|------------|
| **JipperKeyViewer** (AssetBundle) | Standard version, loads bundled resources from `keyviewer_resources` AssetBundle |
| **JipperKeyViewer-FileBased** | Loads sprites/fonts directly from PNG/OTF files, no AssetBundle needed |

Both build from the same solution (`JipperKeyViewer.slnx`) and share the same feature set.

两个版本从同一个 Solution 构建，功能完全一致。
두 버전 모두 동일한 솔루션에서 빌드되며 동일한 기능을 제공합니다.

## Features / 功能

- Real-time on-screen key display with press feedback / 实时按键显示，按下时颜色变化
- Multiple layouts: 8K, 10K, 12K, 14K, 16K, 20K, **24K** + foot keys 2K-16K / 多布局 + 脚键
- KPS counter & total key count / KPS 统计和总按键计数
- Per-key KPS display / 每键独立 KPS 显示
- Rain effect with smooth fade-out on key release / 雨滴特效，松开按键时平滑淡出
- Ghost rain: secondary keys that only trigger rain, no display / 鬼键雨滴：仅触发雨滴，无显示
- Per-row rain controls (speed, height, toggle) / 每排雨滴独立控制（速度、高度、开关）
- Per-key independent colors with Auto Rainbow KV / 每键独立颜色和自动彩虹KV
- KPS / Total independent colors / KPS 和 Total 独立颜色
- Hide main key count toggle / 隐藏主按键计数开关
- Streamer mode (hide KPS/Total) / 流媒体模式（隐藏 KPS/Total）
- Count formatting (1,234) / 大数字千分位格式化
- Fully customizable: colors, fonts, position, size / 完全自定义：颜色、字体、位置、大小
- **Standard Key Width**: normalize mixed-width back rows to uniform 50px (10K/12K/20K third row) / **标准按键宽度**：统一宽窄混排后排为 50px
- Font style options: Bold, Italic, Underline, etc. / 字体样式：粗体、斜体、下划线等
- Normalized custom positioning (0–1 range), auto-adapts to any resolution / 归一化自定义位置，自动适配任意分辨率
- i18n: English / Chinese / Korean / 中英韩三语
- Key rebinding & custom text labels / 按键绑定修改和自定义文本标签
- Object pooling for zero GC allocation on hot path / 对象池，热路径零 GC 分配
- Custom font support: place .ttf/.otf in `CustomFont/`, auto-detected / 自定义字体支持

## Installation / 安装

### JipperKeyViewer (AssetBundle)

1. Copy `JipperKeyViewer.dll` to `A Dance of Fire and Ice/Mods/JipperKeyViewer/`
2. Copy `assets/` and `lang/` folders to the same directory
3. Enable in UnityModManager / 在 UMM 中启用

```
Mods/JipperKeyViewer/
├── JipperKeyViewer.dll
├── Info.json
├── lang/lang.json
└── assets/keyviewer_resources
```

### JipperKeyViewer-FileBased

1. Copy `JipperKeyViewer-FileBased.dll` to `A Dance of Fire and Ice/Mods/JipperKeyViewer-FileBased/`
2. Copy `assets/` and `lang/` folders to the same directory
3. Enable in UnityModManager / 在 UMM 中启用

```
Mods/JipperKeyViewer-FileBased/
├── JipperKeyViewer-FileBased.dll
├── Info.json
├── lang/lang.json
└── assets/
    ├── KeyBackground.png
    ├── KeyOutline.png
    ├── GhostRain.png
    ├── MAPLESTORY_OTF_BOLD.OTF
    └── cjkFonts-regular-normalized.otf
```

## Build / 构建

### Mod DLL

Open `JipperKeyViewer.slnx` in Visual Studio 2022+. Two projects in the solution:

| Project | Output | Description |
|---------|--------|------------|
| `JipperKeyViewer` | `JipperKeyViewer.dll` | AssetBundle version |
| `JipperKeyViewer-FileBased` | `JipperKeyViewer-FileBased.dll` | File-based version |

Reference DLLs are in `libs/`. Builds are automated via [GitHub Actions](https://github.com/2228293026/JipperKeyViewer/actions).

### AssetBundle

Two Unity projects for building the AssetBundle:

| Project | Unity Version |
|---------|--------------|
| `JipperKeyViewer-Unity/` | Unity 6000 |
| `JipperKeyViewer-Unity2022/` | Unity 2022 |

To rebuild: open in Unity → `Tools → Build KeyViewer AssetBundle` → copy `keyviewer_resources` to mod's `assets/`.

## Files / 文件

```
├── JipperKeyViewer.slnx            # Solution (2 projects)
├── Info.json                       # Mod metadata
├── Repository.json                 # UMM release info
├── libs/                           # Reference DLLs
├── .github/workflows/
│   ├── build.yml                   # CI: build on push/PR
│   └── release.yml                 # CD: manual/tag release
│
├── JipperKeyViewer/                # AssetBundle project
│   ├── JipperKeyViewer.csproj
│   ├── Main.cs                     # UMM entry point
│   ├── KeyViewer/
│   │   ├── KeyViewer.cs            # Core lifecycle & config
│   │   ├── KeyViewerGUI.cs         # Settings window (IMGUI)
│   │   ├── KeyViewerInput.cs       # Key detection & rebinding
│   │   ├── KeyViewerLayout.cs      # Layout, positioning, update loop
│   │   ├── KeyViewerResources.cs   # AssetBundle & font management
│   │   ├── KeyViewerSettings.cs    # Settings model & helpers
│   │   ├── RainSystem.cs           # Rain effect manager & object pool
│   │   ├── Key.cs                  # Key MonoBehaviour
│   │   ├── Rain.cs                 # Rain drop rendering
│   │   ├── RawRain.cs              # Rain drop data
│   │   ├── KeyviewerStyle.cs       # Main layout enum
│   │   ├── FootKeyviewerStyle.cs   # Foot key layout enum
│   │   └── I18n.cs                 # i18n system (en/zh/ko)
│   ├── Properties/AssemblyInfo.cs
│   ├── lang/lang.json              # Translations
│   └── assets/keyviewer_resources  # AssetBundle (runtime)
│
└── JipperKeyViewer-FileBased/      # File-based project
    ├── JipperKeyViewer-FileBased.csproj
    ├── Info.json
    ├── KeyViewer/
    │   └── KeyViewerResources.cs   # File-based resource loading
    ├── Properties/AssemblyInfo.cs
    └── assets/                     # Loose PNG/OTF files (runtime)
```

## Settings / 设置

All settings are saved to `config/settings.json` and can be edited in the UMM settings panel.

| Category | Options |
|----------|---------|
| **Layout** | Main: 8K/10K/12K/14K/16K/20K/**24K**, Standard Key Width toggle, Foot: Off/2K-16K |
| **Position** | Custom position (X/Y 0-1), place below toggle |
| **Size** | Scale slider (0.1x – 2.0x) |
| **Colors** | Background, Outline, Text (normal + pressed), Rain (per-row), KPS, Total |
| **Per-Key Colors** | Independent colors per key + Auto Rainbow KV |
| **Font** | Built-in + custom fonts, style flags (Bold/Italic/Underline/etc.) |
| **Rain** | Enable, per-row toggle/speed/height, fade-out on release, ghost rain |
| **Display** | Hide main count, per-key KPS, streamer mode, count formatting |
| **Keys** | Rebind any key, custom text labels, ghost key bindings |
| **Language** | English / 中文 / 한국어 |

## Notes / 说明

- Zero Harmony patches — fully compatible with game updates / 零 Harmony 补丁，完全兼容游戏更新
- Pure Canvas overlay, independent of game UI system / 纯 Canvas 覆盖层，独立于游戏 UI 系统
- Normalized custom positioning: X/Y 0–1 adapts to any resolution and aspect ratio / 归一化坐标，自动适配任意分辨率和宽高比
- Dynamic font scanning: supports any TMP font, deduplicated by original font name / 动态字体扫描，按原始 Font 名去重
- Fonts: [Maplestory OTF](https://fontmeme.com/fonts/maplestory-font/), [cjkFonts](https://www.zitijia.com/i/321518733317131321.html)
- Delta-accumulated rain timer: smooth animation even during GPU spikes / Delta 累加雨滴计时
- Rain fade-out on key release: configurable duration, EaseOutQuad tween / 雨滴松开淡出：可配置时长
- CJK fallback font chain: CJK characters display correctly with any font / CJK 后备字体链：任何字体下中文字符正确显示

## Acknowledgements / 鸣谢
- Key layout and visual style references [JipperResourcePack](https://github.com/Jongye0l/JipperResourcePack).

## License / 许可证

- Primarily **MIT License** — see [LICENSE](./LICENSE.txt).

- Code adapted from [JipperResourcePack](https://github.com/Jongye0l/JipperResourcePack) by Jongyeol is under **BSD 3-Clause** — see [LICENSE-BSD](./LICENSE-BSD).
