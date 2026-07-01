## v1.6.2

### 🚀 Features
- **Per-row rain start height**: `RainStartYRow1/2/3` sliders replace hardcoded container Y (−223/−169/−115), live preview on slider drag via `UpdateRainContainerPositions()`
- **Per-row ghost rain start height**: `GhostRainStartYRow1/2/3` absolute position sliders, independent from normal rain.
- **Per-row rain shadow**: Independent shadow toggle, color, X/Y offset for each row (Row1/2/3), separate settings for ghost rain
- **Per-row rain outline**: Independent outline toggle, color, width for each row (Row1/2/3), separate settings for ghost rain
- **Per-row rain width**: New `RainWidthRow1/2/3` sliders (10–200px), each row's rain width adjustable independently
- **Rain speed/height range extended**: Slider ceiling raised from 1000→2000, text field upper bound fully removed
- **Korean (ko) language added**

### 🐛 Bug Fixes
- **Profile load field leak**: `LoadProfile` used `FromJsonOverwrite` on existing `Settings.Data` — new fields missing from old profile JSONs kept their value from the previous profile, behaving as global. Fixed by resetting `Settings.Data = new ProfileData()` before overwrite.
- **Ghost rain invisible**: `RainGraphic` always enabled for ghost rain, empty mesh (0 verts) with both shadow/outline off interfered with child `ghostImage`. Fixed: only enable `graphic` when shadow or outline active.
- **Ghost rain start Y trail size**: `startY` was mixed into trail length calculation (`FinalSize`), making ghost drops appear with an initial trail. Separated `y` (elapsed → size) from `dropY = startY + y` (→ position).
- **Rain invisible on first press**: `RainSystem.PreWarmPool` created objects without a valid Canvas. Removed pre-warming entirely.
- **Color foldout resetting per-item state every frame**: Foldout expanded state was overwritten each frame.
- **KPS/Total sub-foldout comparison**: Used `>=0` instead of `==t`.

### 🔧 Performance
- **Zero-allocation KPS/Count display**: `NumBuffer.Format` with pre-allocated `char[32]` + `TMP_Text.SetText(buf, offset, length)` eliminates per-frame `ToString` allocations
- **Queue pre-allocation**: `PressTimes` (256) and per-key KPS queues (32) pre-allocated
- **Idle skip**: `_hasKeyPressActivity` flag skips `ProcessPerKeyKpsInUpdate` loop when no keys pressed

### 🧹 Refactor
- **Profile system**: `KeyViewerSettings` split into `ProfileData` (all config) + meta wrapper. Multi-profile create/switch/rename/delete
- **GUI refactor**: Extracted `FloatSliderField` / `DrawFoldoutButton`; split `DrawSettingsWindow` into 10+ standalone section methods
- **RainSystem refactor**: `UpdateEffects` split into `SyncCachedSpeeds` / `UpdateSingleRainDrop` / `ApplyRainTransforms` / `UpdateFadeOut` / `UpdateTrailEdge`
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
