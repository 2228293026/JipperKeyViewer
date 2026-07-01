## v1.6.2

### 🚀 Features
- **Per-row rain shadow**: Independent shadow toggle, color, X/Y offset for each row (Row1/2/3), separate settings for ghost rain
- **Per-row rain outline**: Independent outline toggle, color, width for each row (Row1/2/3), separate settings for ghost rain
- **Per-row rain start height**: `RainStartYRow1/2/3` sliders (−400–−20) control where raindrops emerge relative to each key row, live preview on slider drag
- **Rain shadow/outline I18n**: Chinese/Korean translations for all shadow/outline UI labels

### 🐛 Bug Fixes
- **Profile load field leak**: `LoadProfile` used `FromJsonOverwrite` on existing `Settings.Data` — new fields (shadow/outline/start Y) missing from old profile JSONs kept their value from the previous profile, behaving as global settings. Fixed by resetting `Settings.Data = new ProfileData()` before overwrite, so missing fields correctly default.

## v1.6.1.1

### 🚀 Features
- **Per-row rain width**: New `RainWidthRow1/2/3` sliders (10–200px), each row's rain width adjustable independently
- **Rain speed/height range extended**: Slider ceiling raised from 1000→2000, text field upper bound fully removed
- **Korean (ko) language added**

### 🐛 Bug Fixes
- **Rain invisible on first press**: `RainSystem.PreWarmPool` created objects without a valid Canvas, leaving CanvasRenderer/mesh uninitialized. Removed pre-warming entirely — Rain objects are lightweight enough that it's unnecessary
- **Color foldout resetting per-item state every frame**: Foldout expanded state was overwritten each frame, preventing sub-items from staying open/closed
- **KPS/Total sub-foldout comparison**: Used `>=0` instead of `==t`, causing incorrect expanded-state tracking

### 🔧 Performance
- **Zero-allocation KPS/Count display**: `NumBuffer.Format` with pre-allocated `char[32]` + `TMP_Text.SetText(buf, offset, length)` eliminates per-frame `ToString` string allocations
- **Queue pre-allocation**: `PressTimes` (256) and per-key KPS queues (32) pre-allocated to avoid mid-game queue growth allocations
- **Idle skip**: `_hasKeyPressActivity` flag skips `ProcessPerKeyKpsInUpdate` loop when no keys have been pressed recently

### 🧹 Refactor
- **Profile system**: `KeyViewerSettings` split into `ProfileData` (all config) + meta wrapper (Version/CurrentProfile/ProfileNames/Language). Multi-profile create/switch/rename/delete, v2→v3 auto-migration
- **GUI refactor**: Extracted `FloatSliderField` / `DrawFoldoutButton` / `DrawFoldoutItemButton` to eliminate repetition; split `DrawSettingsWindow` into 10+ standalone section methods
- **RainSystem refactor**: `UpdateEffects` split into `SyncCachedSpeeds` / `UpdateSingleRainDrop` / `ApplyRainTransforms` / `UpdateFadeOut` / `UpdateTrailEdge`; unified rain/ghost rain color retrieval
- **AddQuad signature simplified**: 7 params → 4 params `(VertexHelper, Rect, Color, Color)`
- **I18n rewrite**: Custom JSON parser replaced with `JsonUtility.FromJson<LangFile>`
- **FileBased font style parity**: Added `FontStyleFlags` support (Bold/Italic/etc.)

## v1.6.1

### 🚀 Features
- Custom `RainGraphic` replaces `Image` for normal rain — lighter rendering (solid/gradient quad, 4 verts, no texture sampling)
- Ghost rain now correctly renders the `GhostRain.png` sprite (child `Image` + Tiled mode)
- Per-row ghost rain color customization (`GhostRainColor` / `GhostRainColor2` / `GhostRainColor3`)
- Edge fade slider changed from percentage to pixels (1–200px)

### 🐛 Bug Fixes
- Ghost rain sprite was loaded but never used for rendering — now displays correctly
- Release fade and edge fade now work together without conflict

### 🔧 Performance
- Normal rain: 4 verts / 2 tris, no texture sampling — lighter than old `Image`
- Ghost rain: child `Image` tiling — negligible overhead (low frequency)

### 🧹 Misc
- Removed dead code (unused `Init` overloads, pool methods, stale `ghostSprite`)
- Bumped version to 1.6.1
