# JipperKeyViewer
![C#](https://img.shields.io/badge/Lang-Csharp-c9c8e4.svg?&logo=c#)
![Visual Studio 2026](https://img.shields.io/badge/IDE-Visual%20Studio%202026-5C2D91?logo=visualstudio&logoColor=white)
[![Downloads](https://img.shields.io/github/downloads/2228293026/JipperKeyViewer/total)](https://github.com/2228293026/JipperKeyViewer/releases/latest)

A key overlay mod for **A Dance of Fire and Ice**, built with UnityModManager.

一款适用于 **冰与火之舞** 的按键显示 Mod，基于 UnityModManager 开发。

## Features / 功能

- Real-time on-screen key display with press feedback / 实时按键显示，按下时颜色变化
- Multiple layouts: 8K, 10K, 12K, 16K, 20K + foot keys 2K-16K / 多布局：8K、10K、12K、16K、20K + 脚键 2K-16K
- KPS counter & total key count / KPS（每秒按键数）和总按键计数
- Rain effect: visual trail from pressed keys / 雨滴效果：按键拖尾动画
- Fully customizable: colors, fonts, position, size / 完全自定义：颜色、字体、位置、大小
- **Normalized custom positioning** (v1.3+): X/Y 0–1 range, auto-adapts to any resolution and aspect ratio / **归一化自定义位置**：0–1 范围，自动适配任意分辨率和宽高比
- i18n: Chinese / English UI / 中英文界面
- Key rebinding & custom text labels / 按键绑定修改和自定义文本标签
- Object pooling for zero GC allocation on hot path / 对象池，热路径零 GC 分配
- **Custom font support**: Place .ttf/.otf files in `CustomFont/` folder, auto-detected on startup / **自定义字体支持**：将 .ttf/.otf 文件放入 `CustomFont/` 文件夹，启动自动识别

## Installation / 安装

1. Copy `JipperKeyViewer.dll` and `assets/` to `A Dance of Fire and Ice/Mods/JipperKeyViewer/`
   将 `JipperKeyViewer.dll` 和 `assets/` 文件夹复制到 `A Dance of Fire and Ice/Mods/JipperKeyViewer/`
2. Copy `lang.json` to the lang directory (optional, for custom translations) / 可选：复制 `lang.json` 到lang目录
3. Enable the mod in UnityModManager / 在 UnityModManager 中启用 Mod

### AssetBundle files / 资源文件

The `assets/` folder must contain the AssetBundle file:

```
assets/
└── keyviewer_resources         (AssetBundle with sprites and bundled fonts / 包含精灵和内置字体的 AB 文件)
```

The file is loaded from the `assets/` directory at startup.  
AB 文件在启动时会从 `assets/` 目录加载。

> **Note:** For Unity 2022 and Unity 6000 builds, the AssetBundle is compatible with both versions.  
> **注意：** 该 AB 文件同时兼容 Unity 2022 和 Unity 6000 版本。

## Build / 构建

### Mod DLL

Open `JipperKeyViewer.slnx` in Visual Studio 2022+.

### AssetBundle

Two Unity projects are provided for different Unity versions:

| Project | Unity Version | Output Directory |
|---------|--------------|-----------------|
| `JipperKeyViewer-Unity/` | Unity 6000 | `AssetBundles/` |
| `JipperKeyViewer-Unity2022/` | Unity 2022 | `AssetBundles/` |

To rebuild:

1. Open the **Unity 6000** project in Unity 6000, or the **Unity 2022** project in Unity 2022 / 在对应版本中打开项目
2. `Tools → Build KeyViewer AssetBundle`
3. Copy the generated `keyviewer_resources` from `AssetBundles/` to the mod's `assets/` folder / 将生成的 `keyviewer_resources` 复制到 mod 的 `assets/` 目录

## Files / 文件

```
JipperKeyViewer/
├── Main.cs                       # UMM entry point / Mod 入口
├── KeyViewer/
│   ├── KeyViewer.cs              # Core lifecycle & config / 生命周期 & 配置
│   ├── KeyViewerGUI.cs           # Settings window / 设置界面
│   ├── KeyViewerInput.cs         # Key detection & rebinding / 按键检测 & 绑定
│   ├── KeyViewerLayout.cs        # Layout init, positioning, core update loop / 布局 & 定位
│   ├── KeyViewerRain.cs          # Rain effect & object pool / 雨滴效果 & 对象池
│   ├── KeyViewerResources.cs     # AssetBundle & font management / 资源 & 字体
│   ├── KeyViewerSettings.cs      # Settings data model & helpers / 设置模型 & 辅助类
│   ├── Key.cs                    # Key MonoBehaviour with rain queue / 按键组件
│   ├── Rain.cs                   # Rain drop rendering with object pool / 雨滴渲染
│   ├── RawRain.cs                # Rain drop data & position calculation / 雨滴数据对象
│   ├── KeyviewerStyle.cs         # Key layout enum / 布局枚举
│   ├── FootKeyviewerStyle.cs     # Foot key layout enum / 脚键布局枚举
│   └── I18n.cs                   # i18n system (C# defaults + lang.json override)
├── Properties/AssemblyInfo.cs
├── lang.json                     # Translation file / 翻译文件
├── CustomFont/                   # Custom fonts (.ttf/.otf) / 用户自定义字体目录
└── assets/                       # AssetBundle files (runtime) / AB 资源文件（运行时）
```

## Notes / 说明

- Zero Harmony patches — fully compatible with game updates / 零 Harmony 补丁，完全兼容游戏更新
- Pure Canvas overlay, independent of game UI system / 纯 Canvas 覆盖层，独立于游戏 UI 系统
- Normalized custom positioning (v1.2.2+): X/Y 0–1 adapts to any resolution and aspect ratio / 归一化坐标，自动适配任意分辨率和宽高比
- Dynamic font scanning: supports any TMP font used in the game / 动态字体扫描：支持游戏内所有 TMP 字体
- Fonts: [Maplestory OTF](https://fontmeme.com/fonts/maplestory-font/), [cjkFonts](https://www.zitijia.com/i/321518733317131321.html)
- CustomFont folder path displayed in settings UI for easy access / 设置界面中显示 CustomFont 文件夹路径，方便查找

## Acknowledgements / 鸣谢
- The key layout and visual style references the implementation in [JipperResourcePack](https://github.com/Jongye0l/JipperResourcePack).

## License / 许可证

- This project is primarily licensed under the **MIT License** – see the [LICENSE](./LICENSE) file.

- However, it includes code adapted from [JipperResourcePack](https://github.com/Jongye0l/JipperResourcePack) by Jongyeol, which is licensed under the **BSD 3-Clause License**. The BSD-licensed portions are retained with their original copyright notice, and the full BSD license text is available in the [LICENSE-BSD](./LICENSE-BSD) file.

---

- 本项目主要使用 **MIT 许可证** – 详见 [LICENSE](./LICENSE.txt) 文件。

- 但本项目中包含改编自 [JipperResourcePack](https://github.com/Jongye0l/JipperResourcePack)（作者 Jongyeol）的代码，该部分采用 **BSD 3-Clause 许可证**。BSD 许可部分的原始版权声明已保留，完整的 BSD 许可证文本请见 [LICENSE-BSD](./LICENSE-BSD.txt) 文件。
