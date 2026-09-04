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

| Variant | Resources | Best for |
|---|---|---|
| **JipperKeyViewer** | AssetBundle (`assets/keyviewer_resources`) | UnityModManager / MelonLoader installs |
| **JipperKeyViewer-FileBased** | loose files under `CustomImages/` etc. | setups that prefer plain files, no bundle |

## Features / 功能

### Display / 显示
- Real-time key display with press feedback / 实时按键显示，按下变色
- Layouts: 8K / 10K / 12K / 14K / 16K / 20K / **24K** / **108-key** full keyboard + foot keys 2K–16K / 多布局 + 脚键 + 完整 108 键
- **Standard Key Width**: normalize mixed-width back rows to uniform 50px / **标准按键宽度**：宽窄混排后排统一 50px
- KPS & total counters, per-key KPS, count formatting (1,234), streamer mode / KPS 与总计数、每键 KPS、千分位、主播模式
- **Center KPS/Total text** (flat boxes), label+value stacked mode / **KPS/Total 居中**（扁平框）、上下堆叠模式
- Whole-block move sliders for the 108-key layout, normalized 0–1 positioning, any resolution / 108 键整块移动、归一化定位、任意分辨率
- Custom fonts (.ttf/.otf in `CustomFont/`), font styles, per-key font sizes / 自定义字体、样式、每键字号
- i18n: English / 中文 / 한국어

### Rain / 雨滴
- Merged-mesh rain (all drops in two meshes, pooled records, zero hot-path GC) / 合并网格雨滴（两 mesh + 对象池，热路径零 GC）
- Per-row controls: speed / height / width / start-Y / shadow / outline, normal + ghost sets / 每排速度/高度/宽度/起始Y/阴影/描边，普通+鬼雨两套
- Two-color trail gradient, release fade, trail-top fade / 轨迹双色渐变、松开淡出、顶部渐隐
- Ghost keys: secondary bindings that only trigger ghost rain / 鬼键：只触发鬼雨的备用绑定

### Rendering / 渲染
- Merged shape rendering: every key box draws into two meshes (background + outline) with a dedicated text sub-canvas / 合并形状渲染：全部按键框画进两个 mesh + 独立文本子画布
- Object pooling throughout / 全面对象池

### FreeMake custom layout editor / FreeMake 自定义布局编辑器
A full node-graph key display you design yourself — see the next section. / 完全自定义的节点式按键显示——见下一节。

## FreeMake Editor / FreeMake 编辑器

Switch the layout to **自定义 (Custom)** in the Layout tab, then click **Open FreeMake Editor**. The editor is an independent floating window — edits apply to the live overlay on release.

在「布局」页切换到**自定义**布局后，点击**打开 FreeMake 编辑器**。编辑器为独立浮动窗口，改动即时应用到游戏内覆盖层。

### Canvas / 画布

| Action / 操作 | Control / 方式 |
|---|---|
| Move node / 移动节点 | Drag; Shift locks to an axis / 拖动；Shift 锁轴 |
| Select / 选择 | Click; Ctrl+click toggles; double-click cycles through overlaps / 点击；Ctrl+点击；双击循环拣选重叠节点 |
| Marquee select / 框选 | Drag on empty canvas / 空白处拖动 |
| Zoom / 缩放 | Mouse wheel at cursor / 滚轮（光标锚点）|
| Pan / 平移 | Right-mouse drag / 右键拖动 |
| Nudge / 微调 | Arrow keys (OS repeat; bursts coalesce into one undo step) / 方向键（连发合并为一步撤销）|
| Delete / 删除 | Del / Backspace |
| Copy / Paste / 复制粘贴 | Ctrl+C / Ctrl+V，粘贴偏移逐次递增 |
| Undo / Redo / 撤销重做 | Ctrl+Z / Ctrl+Y（含属性修改；0.4s 内的连续调整合并为一步）|
| Snap / 吸附 | Node edges/centers + screen edges/center, screen-constant threshold; Alt disables / 节点边/中心 + 屏幕边/中心；Alt 临时关闭 |
| Resize / 缩放节点 | 单选八向手柄（Shift 锁比例）；多选四角整体缩放 |
| Minimap / 小地图 | 单击居中；拖动白色视口框平移 |
| Select locked images / 选中锁定图片 | Shift+click |

### Presets / 内置预设

The **Preset** button opens a strip with 12K/16K/20K/10K/8K/14K/24K/**108K**. Each preset rebuilds the fixed layout's hardcoded arrangement as editable nodes, carrying over the profile's bindings, per-key counts, and custom texts.

「预设」按钮展开 12K/16K/20K/10K/8K/14K/24K/**108K** 条带。预设把固定布局的硬编码排布生成为可编辑节点，并继承当前配置的键位、每键计数与自定义文本。

- **保存为新配置**（默认）：预设落进克隆自当前配置的新 Profile，当前布局原封不动 / lands in a NEW cloned profile — current layout untouched
- 取消勾选则**追加进当前画布**：现有节点不动，批次自动挪到空白处避开重叠 / unchecked: **added into the current canvas** — existing nodes stay, the batch shifts to free space
- Every applied preset creates its **own layer group** (e.g. `16K-预设`) — several complete layouts can live in one profile / 每次应用的预设自动归入**自己的图层组**——一个配置可容纳多套完整布局
- 12K/20K variants follow the **Standard Key Width** toggle (visible in custom mode) / 12K/20K 的宽窄变体跟随「标准按键宽度」开关

### Layer groups / 图层组

Named visibility batches — and much more:

- One toggle shows/hides a whole batch: hidden nodes don't render, don't respond to input, don't count / 一键整组显隐：隐藏的节点不渲染、不响应、不计数
- **Per-group KPS/Total**: a group's panels count only that group's keys — several layouts coexist with independent statistics / **按组 KPS/Total**：组面板只统计本组按键——多套布局共存、各自独立统计
- **Multiple KPS/Total panels**: one pair per group, gated by group visibility / **多块 KPS/Total**：每组一对，随组显隐
- **Per-group node budgets**: 112 key-like nodes + 8 decorative images per group — a 108K group doesn't eat other groups' allowance / **按组节点预算**：每组 112 按键类 + 8 图片
- Manager: create (names reuse the smallest free number), rename, show/hide, select-members, assign-selected, delete; empty groups are pruned automatically / 管理区：新建（编号复用最小空闲）、重命名、显隐、选中组内节点、指派、删除；空组自动清理

### Per-node overrides / 节点级覆盖

Every Display-tab and Rain-tab setting has a per-node escape hatch (off = follow the global/row setting):

显示页与雨线页的每个设置都有节点级逃逸口（关闭 = 跟随全局/排设置）：

| Group / 组 | Fields / 字段 |
|---|---|
| Colors / 配色 | background, pressed background, outline, pressed outline, **text, pressed text**（KPS/Total 面板含专属回退）|
| Text / 文本 | custom text, pressed text, font size (0 = global), hide label, hide count / 自定义文案、按压文案、字号、隐藏文字/计数 |
| Rain shape / 雨滴形状 | width, height, speed, X/Y offsets, per-row mapping / 宽、高、速度、XY 偏移、参数排映射 |
| Rain style / 雨滴样式 | two-color gradient, shadow (enable/color/offsets), outline (enable/color/width), trail-top fade, release fade / 双色渐变、阴影、描边、顶部渐隐、松开淡出 |
| Ghost rain / 鬼雨 | fully independent params + shadow + outline sets / 完全独立的参数 + 阴影 + 描边 |
| Animation / 动画 | press scale (enable + scale value), counter bounce (bezier/scale/duration) / 按压缩放、计数器弹跳 |
| KPS/Total panel / 面板 | centered / stacked / value-only text layout override (seeds from the current look) / 居中/堆叠/仅数值文本布局 |
| Misc / 其它 | depth, count-in-total, per-key KPS, unselectable, hidden, image opacity + pressed image, ghost-key binding capture / 深度、计入总数、每键KPS、锁定、隐藏、图片不透明度与按压图、鬼键捕获 |

Multi-select editing: fields show the active node's values (cyan frame on canvas; the label says whose), changes apply to the whole selection, mixed values show `—` (type a number to unify; sliders apply on their own).

多选编辑：字段显示活动节点（画布青色框）的值，改动应用到全部选中；值不一致时显示 `—`（输入数字即统一；滑杆拖动直接生效）。

Node bindings are captured in-window (Esc cancels, mouse ignored). Every node's count, colors, and binding live on the node itself — deleting nodes never scrambles other nodes' data.

改键在窗口内捕获（Esc 取消、忽略鼠标）。节点的计数/配色/绑定内聚在节点自身——删除节点绝不会串位。

Images load from `CustomImages/` under the mod directory (or absolute paths); **Import** copies a file there.

图片从 Mod 目录下的 `CustomImages/`（或绝对路径）加载；「导入」拷贝文件到该目录。

## Credits / 致谢

- The FreeMake editor's interaction paradigm draws inspiration from DM Note's layout editor and similar community editors. Everything here is an original implementation.
- FreeMake 编辑器的交互范式参考了 DM Note 的布局编辑器及社区同类编辑器，本仓库为原创实现。

## Installation / 安装

### UnityModManager

#### JipperKeyViewer (AssetBundle)

```
UMMMods/JipperKeyViewer/
├── JipperKeyViewer.dll                  # Core DLL
├── JipperKeyViewer.Loader.UMM.dll       # UMM entry (referenced by Info.json)
├── Info.json
└── assets/keyviewer_resources
```

#### JipperKeyViewer-FileBased

```
UMMMods/JipperKeyViewer-FileBased/
├── JipperKeyViewer-FileBased.dll
├── JipperKeyViewer-FileBased.Loader.UMM.dll
├── Info.json
└── assets/                              # (if present) loose resource files
```

### MelonLoader

#### JipperKeyViewer (AssetBundle)

```
Mods/JipperKeyViewer/
├── JipperKeyViewer.dll
├── JipperKeyViewer.Loader.Melon.dll
├── Info.json
└── assets/keyviewer_resources
```

#### JipperKeyViewer-FileBased

```
Mods/JipperKeyViewer-FileBased/
├── JipperKeyViewer-FileBased.dll
├── JipperKeyViewer-FileBased.Loader.Melon.dll
├── Info.json
└── assets/
```

After the first launch, the mod creates `Mods/JipperKeyViewer/config/` with `settings.json` (meta) and `profiles/` (one JSON per profile, switchable in-game). Custom fonts go in `CustomFont/`; FreeMake images in `CustomImages/`.

首次启动后创建 `Mods/JipperKeyViewer/config/`（`settings.json` 元数据 + `profiles/` 每配置一个 JSON，游戏内可切换）。自定义字体放 `CustomFont/`；FreeMake 图片放 `CustomImages/`。

## Build / 构建

### Mod DLL

```
dotnet build JipperKeyViewer/JipperKeyViewer.csproj -c Release
dotnet build JipperKeyViewer-FileBased/JipperKeyViewer-FileBased.csproj -c Release
```

Requires the .NET Framework 4.8.1 reference assemblies (auto-restored by the csproj — no Developer Pack needed) and the DLLs under `libs/`.

### AssetBundle

```
cd JipperKeyViewer-Unity
Unity -batchmode -quit -projectPath . -executeMethod BuildScript.BuildAll
```

`.meta` files pin the sprite borders (11px) and 100 ppu — don't lose them.

### Regression harness / 回归测试工程

```
dotnet run --project Harness -c Release -- test
```

72 assertions covering preset generation for all 8 layouts (node counts, binding mapping, count seeding, launch-line offsets), node validation/clamping/caps, per-group budgets, empty-group pruning, and full-field clone/serialization round-trips.

72 项断言：8 种预设生成（节点数/绑定映射/计数种子/发射线偏移）、节点校验/钳制/上限、按组预算、空组清理、全字段克隆与序列化往返。

## Tech Stack & Module Map / 技术栈与模块

### Tech stack / 技术栈

| Layer / 层 | Technology | Why / 用途 |
|---|---|---|
| Language / 语言 | C# 9 on .NET Framework 4.8.1 (SDK-style csproj) | matches the game's Mono runtime / 与游戏 Mono 运行时一致 |
| Editor UI / 编辑器 UI | Unity IMGUI (`OnGUI` + `GUI.Window`) | independent floating window, no game-UI dependency / 独立浮动窗，不依赖游戏 UI |
| Settings UI / 设置界面 | Unity IMGUI (same windowing) | tabs, sliders, color pickers / 标签页、滑杆、取色器 |
| Overlay rendering / 覆盖层渲染 | uGUI + TextMeshPro, custom `MaskableGraphic` merged meshes | all key boxes / rain drops draw into a handful of meshes — tiny canvas rebuilds / 全部按键框与雨滴画进极少数 mesh |
| Input / 输入 | `UnityEngine.InputLegacy` (`Input.GetKey` polling) | one poll per key per frame / 每键每帧一次轮询 |
| Persistence / 持久化 | Newtonsoft.Json 13.0.2 (game-shipped), Fields-mode contract + custom `JsonConverter` for Unity structs | the runtime `JsonUtility` drops class-array fields; atomic write via temp+replace / 运行时 JsonUtility 丢类数组字段；临时文件+替换原子写 |
| Text / 文本 | TextMeshPro (dynamic font conversion, per-key sizes) | crisp text at any scale / 任意缩放下清晰 |
| Assets / 资源 | AssetBundle variant + file-based variant (reflection PNG loading) | both loaders supported / 双变体 |
| Mod loading / 加载 | UnityModManager & MelonLoader (thin loader-entry projects) | dual-loader support / 双加载器 |
| Build & CI / 构建与 CI | `dotnet build`, GitHub Actions | / |
| Testing / 测试 | Harness — offline data-layer regression (72 assertions, reflection against the built DLL) | preset/validation/serialization regressions caught without launching the game / 免启动游戏即拦回归 |

### Module map / 模块表

| File / 文件 | Responsibility / 职责 |
|---|---|
| `ModLoader.cs` | Loader glue, logging / 加载器胶合、日志 |
| `KeyViewer.cs` | Lifecycle, config & profile management, migrations / 生命周期、配置与 Profile 管理、版本迁移 |
| `KeyViewerSettings.cs` | Data model (`ProfileData` / `FmNode` / `FmLayerGroup`) + Newtonsoft persistence / 数据模型与持久化 |
| `KeyViewerLayout.cs` | Overlay construction, fixed layouts, 108K geometry, KPS/Total text modes / 覆盖层构建、固定布局、108K 几何、面板文本模式 |
| `KeyViewerInput.cs` | Input polling, KPS/Total pipelines (per-group stats) / 输入轮询、KPS/Total 管线（按组统计）|
| `CustomLayout.cs` | FreeMake runtime: per-node input/count/colors/rain, groups, validation & caps / FreeMake 运行时：逐节点输入/计数/配色/雨滴、图层组、校验与上限 |
| `KeyViewerEditor.cs` | FreeMake IMGUI editor: canvas gestures, presets, property panel / FreeMake 编辑器：画布手势、预设、属性面板 |
| `EditorHistory.cs` | Timeline undo — snapshot cursor + nudge coalescing / 时间线撤销——快照游标 + 微调合并 |
| `KeyShapeLayer.cs` | Merged key-box meshes (bg + outline) / 合并按键框 mesh |
| `RainSystem.cs` / `RawRain.cs` / `RainLayer.cs` | Pooled rain simulation + merged-mesh rendering, normal & ghost / 对象池雨滴模拟 + 合并渲染，普通与鬼雨 |
| `Key.cs` | Per-key runtime state / 每键运行时状态 |
| `KvImageLoader.cs` | Reflection PNG loader (shared by both variants) / 反射 PNG 加载器（双变体共享）|
| `KeyViewerGUI.cs` / `...SettingsGUI` / `...ColorGUI` / `...RainGUI` / `...BindingGUI` | Settings window tabs / 设置窗口各标签页 |
| `KeyViewerResources.cs` | AssetBundle resource loading (AssetBundle variant) / 资源包加载 |
| `I18n.cs` | EN / 中文 / 한국어 strings / 三语言词条 |

## Files / 文件

```
JipperKeyViewer/
├── JipperKeyViewer/                 # AssetBundle variant
│   └── KeyViewer/
│       ├── KeyViewer.cs             # Lifecycle, config management, profiles
│       ├── KeyViewerLayout.cs       # Overlay construction, fixed layouts, 108K geometry
│       ├── KeyViewerInput.cs        # Input polling, KPS/Total pipelines
│       ├── KeyViewerEditor.cs       # FreeMake editor (IMGUI window)
│       ├── CustomLayout.cs          # FreeMake runtime (per-node input/count/rain)
│       ├── KeyViewerSettings.cs     # Data model + Newtonsoft persistence
│       ├── EditorHistory.cs         # Timeline undo (snapshot cursor)
│       ├── RainSystem.cs / RawRain.cs / RainLayer.cs   # Merged-mesh rain
│       ├── KeyShapeLayer.cs         # Merged key-box meshes
│       ├── KvImageLoader.cs         # Reflection PNG loader
│       └── I18n.cs                  # EN/ZH/KO strings
├── JipperKeyViewer-FileBased/       # File-based variant (shared sources)
├── JipperKeyViewer-Unity/           # AssetBundle build project
├── JipperKeyViewer.Loader.UMM / .Melon   # Loader entries
├── Harness/                         # Offline data-layer regression suite
└── libs/                            # Reference DLLs
```

## Settings / 设置

Open the in-game mod settings (UMM mod manager window / Melon overlay). All tabs — Layout, Display, Rain, Keys, Colors — apply live. Configs auto-save (debounced during slider drags) and survive crashes via atomic writes.

游戏内打开 Mod 设置（UMM 管理窗 / Melon 覆盖层）。布局/显示/雨线/按键/配色所有页实时生效；配置自动保存（拖动滑杆时去抖），原子写入防崩溃损坏。

## Notes / 说明

- Key detection samples `Input.GetKey` once per frame; a complete press+release inside one frame (very low FPS) may be missed / 按键检测每帧采样一次，极低帧率下同帧内的完整按压可能漏计
- Custom-layout profiles persist through Newtonsoft (the runtime JsonUtility drops class-array fields); legacy interim formats import automatically / 自定义布局经 Newtonsoft 持久化（运行时 JsonUtility 会丢类数组字段）；过渡期格式自动导入
- Old builds reading a Custom profile fall back to Key16 / 旧版本读到 Custom 配置回落 Key16

## License / 许可证

- **MIT License** — see [LICENSE](./LICENSE.txt).
