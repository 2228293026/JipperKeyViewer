# JipperKeyViewer
![C#](https://img.shields.io/badge/Lang-Csharp-c9c8e4.svg?&logo=c#)
![Visual Studio 2022](https://img.shields.io/badge/IDE-Visual%20Studio%202022-5C2D91?logo=visualstudio&logoColor=white)
[![Downloads](https://img.shields.io/github/downloads/2228293026/JipperKeyViewer/total)](https://github.com/2228293026/JipperKeyViewer/releases/latest)
[![Build](https://github.com/2228293026/JipperKeyViewer/actions/workflows/build.yml/badge.svg)](https://github.com/2228293026/JipperKeyViewer/actions/workflows/build.yml)

Keyboard overlay for **A Dance of Fire and Ice** — real-time key presses, KPS counter, and rain effects.
Supports **UnityModManager** and **MelonLoader**.

一款适用于 **冰与火之舞** 的按键显示 Mod，实时显示按键按下、KPS 统计和雨滴特效。
支持 **UnityModManager** 和 **MelonLoader**。

ADOFAI 키보드 오버레이 — 실시간 키 입력, KPS 카운터, 빗줄 효과를 표시합니다.
**UnityModManager** 및 **MelonLoader** 지원.

## Variants / 版本

| Variant | Description |
|---------|------------|
| **JipperKeyViewer** (AssetBundle) | Standard version, loads bundled resources from `keyviewer_resources` AssetBundle |
| **JipperKeyViewer-FileBased** | Loads sprites/fonts directly from PNG/OTF files, no AssetBundle needed |

Both build from the same solution (`JipperKeyViewer.slnx`) and share the same feature set. Each variant works under both UnityModManager and MelonLoader.

两个版本从同一个 Solution 构建，功能完全一致。每个版本均支持 UMM 和 MelonLoader。
두 버전 모두 동일한 솔루션에서 빌드되며 동일한 기능을 제공합니다. 각 버전은 UMM과 MelonLoader를 모두 지원합니다.

## Features / 功能

- Real-time on-screen key display with press feedback / 实时按键显示，按下时颜色变化
- Multiple layouts: 8K, 10K, 12K, 14K, 16K, 20K, **24K**, and a full **108-key** physical keyboard + foot keys 2K-16K / 多布局 + 脚键 + 完整 108 键全键盘
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
- **108-key full keyboard**: complete QWERTY + numpad layout, even 6px key spacing, own KPS/Total controls and unified color / **108 键全键盘**：完整 QWERTY + 小键盘，统一 6px 间距，独立 KPS/Total 与统一色
- **Move the whole keyboard**: custom-position sliders drag the entire 108-key block to any screen edge; KPS/Total keep their own position / **整块移动**：自定义位置滑块整体平移 108 键到任意屏幕边缘，KPS/Total 独立定位
- **Center KPS / Total text**: merge label + number into one centered line that re-centers as the number grows (flat boxes only) / **KPS/Total 文本居中**：标签与数值合并居中，数值变长整行重排（仅扁平框）
- Font style options: Bold, Italic, Underline, etc. / 字体样式：粗体、斜体、下划线等
- Normalized custom positioning (0–1 range), auto-adapts to any resolution / 归一化自定义位置，自动适配任意分辨率
- i18n: English / Chinese / Korean / 中英韩三语
- Key rebinding & custom text labels / 按键绑定修改和自定义文本标签
- Object pooling for zero GC allocation on hot path / 对象池，热路径零 GC 分配
- Custom font support: place .ttf/.otf in `CustomFont/`, auto-detected / 自定义字体支持

## Installation / 安装

### UnityModManager

#### JipperKeyViewer (AssetBundle)

```
UMMMods/JipperKeyViewer/
├── JipperKeyViewer.dll                  # Core DLL
├── JipperKeyViewer.Loader.UMM.dll       # UMM entry (referenced by Info.json)
├── Info.json
├── lang/lang.json
└── assets/keyviewer_resources
```

1. Copy the folder to `UMMMods/JipperKeyViewer/`
2. Enable in UnityModManager / 在 UMM 中启用

#### JipperKeyViewer-FileBased

```
UMMMods/JipperKeyViewer-FileBased/
├── JipperKeyViewer-FileBased.dll
├── JipperKeyViewer-FileBased.Loader.UMM.dll
├── Info.json
├── lang/lang.json
└── assets/
    ├── KeyBackground.png
    ├── KeyOutline.png
    ├── GhostRain.png
    ├── MAPLESTORY_OTF_BOLD.OTF
    └── cjkFonts-regular-normalized.otf
```

1. Copy the folder to `UMMMods/JipperKeyViewer-FileBased/`
2. Enable in UnityModManager / 在 UMM 中启用

### MelonLoader

**Default hotkey**: `F1` to open settings / 默认热键 `F1` 打开设置
**Custom hotkey**: edit `UserData/MelonPreferences.cfg`, section `[JipperKeyViewer]` / 编辑配置文件修改热键

#### JipperKeyViewer (AssetBundle)

```
Mods/JipperKeyViewer/
├── JipperKeyViewer.dll                  # Core DLL
├── JipperKeyViewer.Loader.Melon.dll     # MelonLoader entry (auto-detected)
├── lang/lang.json
└── assets/keyviewer_resources
```

1. Copy the folder to `Mods/JipperKeyViewer/`
2. MelonLoader auto-detects on launch / MelonLoader 启动时自动加载

#### JipperKeyViewer-FileBased

```
Mods/JipperKeyViewer-FileBased/
├── JipperKeyViewer-FileBased.dll
├── JipperKeyViewer-FileBased.Loader.Melon.dll
├── lang/lang.json
└── assets/
    ├── KeyBackground.png
    ├── KeyOutline.png
    ├── GhostRain.png
    ├── MAPLESTORY_OTF_BOLD.OTF
    └── cjkFonts-regular-normalized.otf
```

1. Copy the folder to `Mods/JipperKeyViewer-FileBased/`
2. MelonLoader auto-detects on launch / MelonLoader 启动时自动加载

## Build / 构建

### Mod DLL

Open `JipperKeyViewer.slnx` in Visual Studio 2022+. Six projects in the solution:

| Project | Output | Description |
|---------|--------|------------|
| `JipperKeyViewer` | `JipperKeyViewer.dll` | Core (AssetBundle version) |
| `JipperKeyViewer.Loader.UMM` | `JipperKeyViewer.Loader.UMM.dll` | UMM entry for AssetBundle |
| `JipperKeyViewer.Loader.Melon` | `JipperKeyViewer.Loader.Melon.dll` | MelonLoader entry for AssetBundle |
| `JipperKeyViewer-FileBased` | `JipperKeyViewer-FileBased.dll` | Core (File-based version) |
| `JipperKeyViewer-FileBased.Loader.UMM` | `JipperKeyViewer-FileBased.Loader.UMM.dll` | UMM entry for FileBased |
| `JipperKeyViewer-FileBased.Loader.Melon` | `JipperKeyViewer-FileBased.Loader.Melon.dll` | MelonLoader entry for FileBased |

Reference DLLs are in `libs/`. Builds are automated via [GitHub Actions](https://github.com/2228293026/JipperKeyViewer/actions).

**Architecture**: The core DLL contains all mod logic and is loader-agnostic.
Loader DLLs are thin wrappers that bridge to UnityModManager or MelonLoader.

**架构**：核心 DLL 包含所有 Mod 逻辑，与加载器无关。
加载器 DLL 是薄桥接层，负责对接 UnityModManager 或 MelonLoader。

### AssetBundle

Two Unity projects for building the AssetBundle:

| Project | Unity Version |
|---------|--------------|
| `JipperKeyViewer-Unity/` | Unity 6000 |
| `JipperKeyViewer-Unity2022/` | Unity 2022 |

To rebuild: open in Unity → `Tools → Build KeyViewer AssetBundle` → copy `keyviewer_resources` to mod's `assets/`.

## Files / 文件

```
├── JipperKeyViewer.slnx            # Solution (6 projects)
├── Info.json                       # Mod metadata (UMM: AssetBundle)
├── Repository.json                 # UMM release info
├── libs/                           # Reference DLLs
├── .github/workflows/
│   ├── build.yml                   # CI: build on push/PR
│   └── release.yml                 # CD: manual/tag release
│
├── JipperKeyViewer/                # AssetBundle core project
│   ├── JipperKeyViewer.csproj
│   ├── Main.cs                     # Loader-agnostic entry point
│   ├── KeyViewer/
│   │   ├── ModLoader.cs            # IModLoader interface + Loader
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
├── JipperKeyViewer.Loader.UMM/     # UMM loader (AssetBundle)
│
├── JipperKeyViewer.Loader.Melon/   # MelonLoader loader (AssetBundle)
│
├── JipperKeyViewer-FileBased/      # File-based core project
│   ├── JipperKeyViewer-FileBased.csproj
│   ├── Info.json
│   ├── KeyViewer/
│   │   └── KeyViewerResources.cs   # File-based resource loading
│   ├── Properties/AssemblyInfo.cs
│   └── assets/                     # Loose PNG/OTF files (runtime)
│
├── JipperKeyViewer-FileBased.Loader.UMM/   # UMM loader (FileBased)
│
└── JipperKeyViewer-FileBased.Loader.Melon/ # MelonLoader loader (FileBased)
```

## Settings / 设置

Settings are saved to `config/settings.json` and can be edited via:
- **UMM**: In the UnityModManager settings panel / 在 UMM 设置面板中
- **MelonLoader**: Press default hotkey `F1` to open settings window / 按默认热键 `F1` 打开设置窗口

| Category | Options |
|----------|---------|
| **Layout** | Main: 8K/10K/12K/14K/16K/20K/**24K**/**108-key full**, Standard Key Width toggle, Foot: Off/2K-16K |
| **Position** | Custom position (X/Y 0-1); for the 108-key layout this moves the whole keyboard block |
| **Size** | Scale slider (0.1x – 2.0x) |
| **Colors** | Background, Outline, Text (normal + pressed), Rain (per-row), KPS, Total |
| **Per-Key Colors** | Independent colors per key + Auto Rainbow KV |
| **Font** | Built-in + custom fonts, style flags (Bold/Italic/Underline/etc.) |
| **Rain** | Enable, per-row toggle/speed/height, fade-out on release, ghost rain |
| **Display** | Hide main count, per-key KPS, streamer mode, count formatting |
| **Full Keyboard (108-key)** | Show KPS/Total, KPS/Total size (40–400px), KPS/Total position, unified color, center KPS/Total text |
| **Keys** | Rebind any key, custom text labels, ghost key bindings |
| **Language** | English / 中文 / 한국어 |

## Notes / 说明

- Zero Harmony patches — fully compatible with game updates / 零 Harmony 补丁，完全兼容游戏更新
- Dual-loader support: UnityModManager **and** MelonLoader / 双加载器支持
- Pure Canvas overlay, independent of game UI system / 纯 Canvas 覆盖层，独立于游戏 UI 系统
- Normalized custom positioning: X/Y 0–1 adapts to any resolution and aspect ratio / 归一化坐标，自动适配任意分辨率和宽高比
- Dynamic font scanning: supports any TMP font, deduplicated by original font name / 动态字体扫描，按原始 Font 名去重
- Fonts: [Maplestory OTF](https://fontmeme.com/fonts/maplestory-font/), [cjkFonts](https://www.zitijia.com/i/321518733317131321.html)
- Delta-accumulated rain timer: smooth animation even during GPU spikes / Delta 累加雨滴计时
- Rain fade-out on key release: configurable duration, EaseOutQuad tween / 雨滴松开淡出：可配置时长
- CJK fallback font chain: CJK characters display correctly with any font / CJK 后备字体链：任何字体下中文字符正确显示
- **MelonLoader**: Settings hotkey configurable in `UserData/MelonPreferences.cfg` (`[JipperKeyViewer]` section) / 设置热键可在配置文件中修改

## Acknowledgements / 鸣谢
- Key layout and visual style references [JipperResourcePack](https://github.com/Jongye0l/JipperResourcePack).

## License / 许可证

- Primarily **MIT License** — see [LICENSE](./LICENSE.txt).

- Code adapted from [JipperResourcePack](https://github.com/Jongye0l/JipperResourcePack) by Jongyeol is under **BSD 3-Clause** — see [LICENSE-BSD](./LICENSE-BSD).
