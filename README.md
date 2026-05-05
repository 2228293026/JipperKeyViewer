# JipperKeyViewer

A key overlay mod for **A Dance of Fire and Ice**, built with UnityModManager.

## Features

- Real-time on-screen key display with press feedback
- Multiple layouts: 8K, 10K, 12K, 16K, 20K + foot keys (2K-16K)
- KPS (Keys Per Second) counter & total key count
- Rain effect: visual trail from pressed keys
- Fully customizable: colors, fonts, position, size
- i18n: Chinese / English UI
- Key rebinding & custom text labels

## Installation

1. Copy `JipperKeyViewer.dll` and `keyviewer_resources` to `A Dance of Fire and Ice_Data/Managed/UnityModManager/`
2. Copy `lang.json` to the same directory (optional, for custom translations)
3. Enable the mod in UnityModManager

## Build

Open `JipperKeyViewer.slnx` in Visual Studio 2022+.

### AssetBundle

Sprites and fonts are loaded from `keyviewer_resources` AssetBundle. To rebuild:

1. Open the Unity project at `JipperKeyViewer-Unity/`
2. Set AssetBundle name `keyviewer_resources` on:
   - `_Assets/Textures/KeyBackground.png`
   - `_Assets/Textures/KeyOutline.png`
   - `_Assets/Fonts/MAPLESTORY_OTF_BOLD.OTF`
   - `_Assets/Fonts/cjkFonts-regular-normalized.otf`
3. Tools → Build KeyViewer AssetBundle
4. Copy `AssetBundles/keyviewer_resources` to the mod directory

## Files

```
JipperKeyViewer/
├── Main.cs                       # UMM entry point
├── KeyViewer/
│   ├── KeyViewer.cs              # Core logic: UI, input, rain, settings GUI
│   ├── Key.cs                    # Key MonoBehaviour with rain queue
│   ├── Rain.cs                   # Rain drop rendering with object pool
│   ├── RawRain.cs                # Rain drop data & position calculation
│   ├── KeyviewerStyle.cs         # Key layout enum
│   ├── FootKeyviewerStyle.cs     # Foot key layout enum
│   └── I18n.cs                   # i18n system (C# defaults + lang.json override)
├── Properties/AssemblyInfo.cs
└── lang.json                     # Translation file
```