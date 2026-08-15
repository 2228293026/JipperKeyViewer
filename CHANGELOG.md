## Unreleased

### 🐛 Bug Fixes
- **Slider text fields couldn't type intermediate values**: The old helper re-fed `value.ToString` every IMGUI event, wiping half-typed input ("", "-", "0.") — negatives in the rain shadow X/Y fields (-10..10) were unreachable by typing — and silently re-clamped stored below-min values to the minimum just by opening the tab. Text fields now use the color picker's input-buffer pattern and only commit when the text actually changed; typed values are stored fully unclamped (negatives and out-of-range values included — the slider is just a range indicator), except that "NaN"/"Infinity" literals are rejected (NaN slips past every `<=0` guard and poisons mesh vertices).
- **Old-profile migration crashed and wiped user settings**: Builds between the profile refactor and MaxKeySlots=40 wrote `Count[36]`; the V3→V4 foot-base migration's `Array.Copy` ran past its end, the exception reset everything to defaults, and the next save overwrote the profile files — permanent settings loss. `EnsureSettingsArrays` now resizes `Count`, `PerKeyGhostRainColor`, `PerKeyFontSize`, and `key108` to their target lengths (it used to null-check only), and the batch migration of other profile files resizes first too.
- **Load failures swallowed user config**: On a parse failure or migration exception, settings fall back to defaults — but now the offending settings.json and current profile are first backed up as `*.corrupt`. All config writes are atomic (temp file + replace), so a crash or power loss mid-write can no longer truncate the file.
- **Press animation stuck shrunken when toggled off mid-press**: The release transition is gated on the toggle; turning it off now resets every key's scale and stops the running animation coroutines.
- **Ghost rain desynced when re-enabled mid-hold**: Ghost key states weren't tracked while the rain toggles were off, so a held key needed an extra release/press cycle before triggering again. Tracking no longer depends on the gates.
- **Profile foldout scanned the disk every GUI event** while expanded (several times per frame); it now syncs once on open.
- **16K drew an empty "row 3" label** in the per-key color / text-size sections (`>= 8` vs the binding tab's `> 8`; 16K has exactly 8 back keys). Unified to `> 8`.
- **Per-key text-size section shown on the 108-key layout** with mislabeled rows — it targets the standard layouts and is now hidden there.
- **24K key lists ordered 23 before 22**, opposite the on-screen layout (…22, 23 left-to-right). Sequence corrected.
- **Unknown language highlighted Korean** while I18n rendered English; unknown codes now map to English everywhere.
- **Profile duplicate checks were case-sensitive**: On NTFS "MyProfile"/"myprofile" are the same file, and renaming could delete the case variant first. Comparisons are now case-insensitive (case-only renames still work).
- **Per-scene font restore was dead logic**: `OnSceneLoaded` reset the flag but `RestoreFontOnce` only ever ran from `Start`. It now runs at the end of each scene load.
- **Unity fake-null checks**: `UpdateAllFonts` / `ExecuteCountReset` used `?.` on MonoBehaviours (bypasses the destroyed-check) — replaced with explicit nulls; one `ClearActiveDrops` call gained the null guard its siblings have.
- **FileBased variant failed silently on missing assets**: font files (MapleStory/CJK) and GhostRain.png missing produced no log — a missing CJK font also broke the fallback chain and rendered CJK labels as boxes. Both now log like the bundle variant.
- **Foot keys rebuilt twice per layout/profile switch**: `ResetKeyViewer` already recreates them internally; the outer duplicate calls were removed.

### 🔧 Performance
- **Zero-allocation KPS/Total update path**: `KpsTotalIsSlim` allocated a fresh LayoutDesc + ExtraSlot[] per keypress via `GetLayout` (twice per press in hide-label mode). Its result depends only on layout + standard-width, so it's cached now.
- **Settings GUI GUIStyle caching**: the per-key color button loop allocated a GUIStyle per button (~26 per frame) plus red-text styles every event — all cached; `PerKeyTypeOrder` arrays made static.
- **Bundle rebuild can't lose sprite borders**: `.meta` files aren't version-controlled, so a fresh clone re-imported the PNGs with border 0, silently collapsing 9-slicing and ghost-rain tiling in both variants. The build script now enforces the 11px spriteBorder + 100 ppu on the three sprites.

### 🧰 Build & Release
- **One-command version bump**: new `tools/SetVersion.ps1` updates all six AssemblyInfo files, the `[MelonInfo]` version string, both Info.json files, and Repository.json (versions + download URLs) in one idempotent pass. The release workflow runs the same script before msbuild — previously `/p:Version` had no effect on the classic (non-SDK) csproj files, so shipped DLL versions never changed; the dead "patch Info.json after zipping" step was also removed.
- **Resolution changes reposition immediately**: full-keyboard KPS/Total, the 108-key block offset, and custom-position bases were baked at build/slider time and stayed misplaced after a mid-session resolution change until the next rebuild; the change is now detected and positions re-applied.
- **Untracked the Unity-generated `Assembly-CSharp-Editor.csproj`** (regenerated per editor version) and added it to .gitignore.
- **Per-row rain toggles did nothing individually**: The released version wired rows 1 and 2 both to the Row-2 toggle, leaving the Row-1 toggle dead. All three rows now toggle independently (keys 0-7 = row 1, 8-15 = row 2, 16+ = row 3), and turning a row off immediately clears that row's in-flight drops.
- **Ghost rain sprite broke on long holds**: The GhostRain sprite carries an 11px 9-slice border, and the old `Image.Type.Tiled` only tiles the inner region for bordered sprites, repeating the border strips along their own direction. The merged renderer mistakenly tiled the whole texture, so ghost columns past 100px re-drew a border every 100px. Now ported from uGUI's `GenerateTiledSpriteToVertexBuffer` verbatim: center tiles whole, side border columns tile vertically, top/bottom rows tile horizontally, corners once, borders squeeze when the rect is too small.
- **FileBased: font-size slider didn't update existing texts**: The file-based variant's `UpdateAllFonts` was missing the `fontSizeMax` write the bundle variant has.
- **Changing settings with the display off threw NRE**: Layout/width/foot-key/centering/color handlers kept running with no overlay and crashed on null. They now safely skip while the display is off; the next enable builds from the updated settings. Pre-existing.
- **MelonLoader didn't save on window close**: Melon's save events are no-ops, so settings that rely on close-time saving (rain height/speed/width, start-Y, KPS/Total labels) were only written on game quit and lost on a crash. The settings window now saves when hidden; UMM's window also stops running on a destroyed component after the mod is toggled off.

### 🔧 Performance
- **Merged key-box rendering**: All key backgrounds/outlines draw into two self-drawn meshes (background layer + outline layer sharing slot state, 9-sliced) instead of two Image GameObjects per key. The main canvas drops from 500+ Graphics to 2; press color/scale changes rebuild one small mesh instead of re-batching the whole canvas.
- **Dedicated text sub-canvas**: All key texts moved onto a TextLayer sub-canvas; KPS/count text updates no longer re-batch the shape meshes and vice versa. Press-scale animation drives the text wrapper and shape mesh together (including "rain follows scale" mode).
- **Merged rain rendering**: The per-key RainLine canvases (~25 nested Canvases) and the whole Rain GameObject pool are gone — every drop draws into two self-drawn meshes (normal / ghost layers). Drops are pooled plain-data records (RawRain); each frame only updates rects and fade params and rewrites vertices: no per-drop RectTransform writes, no SetSiblingIndex. Start-Y and ghost offsets are read live at render time, so sliders act on in-flight drops directly.
- **Behavior kept**: 9-slice border math (legacy 2x size + 0.5 scale), trail gradient/shadow/outline math, ghost bordered tiling (see fix above), and ghost-above-normal stacking per key (bodies and shadow/outline). Known differences: (1) when columns from different rows overlap (wide rain / shared columns), the old build interleaved by row while now all ghost rain draws on top; (2) with a translucent rain color plus fade enabled, the old build snapped to full opacity on release before fading — the new one fades smoothly from the color's own alpha; (3) if the ghost sprite fails to load, ghost rain degrades to solid ghost-colored columns (the old fallback was opaque white).

---

## v1.7.0

### 🚀 Features
- **Hide KPS/Total Label**: New toggle that hides the "KPS"/"Total" label text and centers the value in the middle of the box, updating every frame. Only shown for non-flat layouts (10K/12K/20K standard mode).
- **Stacked KPS/Total display**: New "Stacked" toggle, only available when Centered is enabled. Label on top (compact font), value on bottom, both stacked inside the box.
- **Settings UI multi-tab refactor**: Settings window split into 6 tabs — General, Layout, Display, Rain, Keys, Colors — with a permanent top bar for master toggles. Each tab's content lives in its own partial class file for cleaner organization and easier maintenance. Current tab index persisted to settings for cross-session recall.
- **Hex color input**: Color picker fields now accept `#RRGGBB` / `#RRGGBBAA` hex paste/typing, auto-parsed to Unity `Color`. Input focus no longer resets on redraw; each channel input has a unique control name.
- **Foot key text centered**: Foot key text is now centered in the key box instead of pinned to the top.

### 🐛 Bug Fixes
- **Profile data leak on switch**: `LoadProfile` now creates `new ProfileData()` before `FromJsonOverwrite` — prevents fields from the previously loaded profile leaking into the newly loaded one.
- **Profile size not applied after switch**: `ResetKeyViewer()` now re-applies `KeyViewerSizeObject` localScale at the end, so a profile's Size setting isn't overridden by the previous profile's slider value.
- **Centered text stuck after switching layouts**: Turning centering on for one layout and switching to another now correctly restores the normal label/number layout.
- **Skip count update when main key count is hidden**: `HideMainKeyCount=true` no longer creates a `value` text object or tries to update it.
- **Full keyboard KPS/Total toggle survives layout switch**: Switching away from and back to the full keyboard no longer loses the KPS/Total show/hide state.

### 🔧 Performance
- Smoother settings access and reworked rain-drop object pool (larger, no leftover drops).

---

## v1.6.5

### 🚀 Features
- **108-key full keyboard layout**: New layout option that shows a complete physical QWERTY keyboard (with numpad). Keys are evenly spaced with a consistent 6px gap, the number pad hugs the main block, and the tall Numpad `+` / `Enter` keys span two rows. No foot keys, per-key colors, or ghost keys in this mode.
- **Dedicated KPS / Total for the full keyboard**: The full keyboard has its own "Show KPS / Total" toggle, plus separate position controls and a size slider (40–400px). A "Full Keyboard Unified Color" option lets KPS/Total share the keyboard's background, outline, and text colors.
- **Move the whole keyboard freely**: The custom-position sliders now move the entire 108-key block. Drag the X/Y sliders to pin it to any screen edge (left/right/top/bottom); KPS/Total keep their own independent position and are not dragged along.
- **Center the KPS / Total text**: New "Center KPS / Total Text" option. On flat (side-by-side) KPS/Total boxes it merges the label and number into one centered line that re-centers as the number grows (e.g. `KPS 123`). Stacked (top/bottom) layouts keep the label above the number and hide this toggle.
- **Cleaner full-keyboard settings**: Unrelated toggles (hide main count, per-key KPS, streamer mode) are hidden in full-keyboard mode, and the KPS/Total controls get their own collapsible section above the color settings.
- **Rebind the settings hotkey in-game (MelonLoader)**: A new "Settings Hotkey" row in the settings window lets you click the button and press any key to rebind the hotkey that opens/closes the UI. No more editing `MelonPreferences.cfg` by hand.

### 🐛 Bug Fixes
- **Crash when enabling custom position on the full keyboard**: Enabling custom position no longer throws an error on the 108-key layout.
- **Stray foot keys on the full keyboard**: Toggling custom position no longer makes foot-key controls or phantom keys appear in full-keyboard mode.
- **Centered text stuck after switching layouts**: Turning centering on for one layout and switching to another now correctly restores the normal label/number layout.

### 🔧 Performance
- Smoother settings access and a reworked rain-drop object pool (larger, no leftover drops); slightly larger internal queues for key-press timing.

## v1.6.4.1

### 🚀 Features
- **Dual loader support**: Works with both UnityModManager and MelonLoader. Core DLL is loader-agnostic, bridged via the `IModLoader` interface. Four thin loader projects added.
- **MelonLoader settings UI**: Press F1 (default, rebindable) to open the settings window; window scales with resolution.
- **MelonLoader hotkey configurable**: Edit the `Hotkey` field under `[JipperKeyViewer]` in `UserData/MelonPreferences.cfg`.
- **`[MelonGame]` removed**: Loads under any MelonLoader game (TMP support required).
- **Key Font Size**: New `KeyFontSize` slider (8–72) replaces hardcoded `fontSizeMax=20`.

### 🧹 Refactor
- **Main.cs** loader-agnostic: `Init(IModLoader)` + `EnableNow()` replace the UMM binding.
- **ModLoader.cs**: New `IModLoader` interface + static `Loader` accessor.
- **KeyViewerResources.cs / I18n.cs** etc.: All `Main.Mod.*` references replaced with `Loader.*`.
- **Info.json**: Entry points to `JipperKeyViewer.Loader.UMM.dll`.
- **Build**: 6-project build packaging both loaders and both variants.

## v1.6.4

### 🚀 Features
- **Key Font Size**: New `KeyFontSize` slider (8–72) replaces hardcoded `fontSizeMax=20`. Adjust key label text size live — fixes Tab (⇥) and Space (␣) rendering too small without `<size>` tag hacks.
- **Key Press Animation**: Keys shrink smoothly on press, return on release (80ms lerp coroutine). Toggle `EnablePressAnimation`, adjust `PressAnimationScale` (0.5–0.95), and optionally `EnablePressAnimationOnRain` so rain drops animate together. Visuals wrapper isolates scaling from rain containers.
- **Per-Key Ghost Rain Color**: New `PerKeyGhostRainColor` array with dedicated color picker in per-key color editor — set each key's ghost rain color independently from normal rain.

### 🐛 Bug Fixes
- **14K back row alignment**: Corrected to match "16K cut off outermost columns" — `BackSequence14={13,9,8,10,11,12}` with keys centered at columns 1–6 (x=54–324). Per-key counts stay at consistent screen positions when switching between layouts.
- **Rain container no-sharing**: Each key keeps its own RainLine; `ShareRainContainer` now only adjusts X offset and width for front-column alignment instead of destroying/redirecting. Eliminates Z-order issues between rows without Canvas sorting hacks.
- **Canvas.sortingOrder removed**: `FixRainContainerSortOrder()` removed — caused rain containers to override parent-key layering. GraphicRaycaster also removed from RainLine (unnecessary).
- **RainGraphic shadow gradient**: Shadow quad changed from solid-color `AddQuad` to `DrawRainQuad` — follows the top-edge gradient fade along with main rain and outline.
- **24K back sequence**: Corrected `BackSequence24` to match row 2/row 3 layout order.
- **AutoAssignRainbowColors**: Refactored to assign colors per-layout key count (+ foot keys + KPS/Total) instead of sequential index.
- **Animation null-guard**: Added `if (animTarget == null) yield break;` in coroutine to prevent NRE on destroyed keys.
- **NRE on load**: Color array initialization uses `SafeEnsure` helper; `InitPerKeyColors` rain color loop uses `footBase`.

### 🧹 Refactor
- **FootKeyBase fixed to 24**: Removed conditional (was `Key24 ? 24 : 20`). V3→V4 migration shifts foot key data from indices 20 → 24 for all profiles.
- **Image-based rain shadow/outline**: Added `shadowImage`/`outlineImage` child objects on Rain with `SetupShadow`/`SetupOutline` methods. Gradient texture synced to all three images.
- **EnsureColorArray/SafeEnsure**: Unified helper replaces per-field null checks for color arrays.
- **PerKeyTypeOrder extended**: Includes type 7 (ghost rain color) for main keys.
- **Settings.Version bumped**: 3 → 4 for foot base migration.

## v1.6.3

### 🚀 Features
- **24K layout**: New 24-key layout (8+8+8) — three full rows of 8 keys each at 50px, all 54px apart. Foot keys use indices 24-39, allowing up to 16K foot key selection. Ghost key bindings and third-row rain settings fully supported. Layout tuned with frontY=375, KPS/Total at y=221, 6px gap between rows and KPS (matching 16K standard).
- **Standard Key Width mode**: Toggle `StandardKeyWidth` that converts mixed-width back rows (12K key 8/10 from 77→50px, 10K 129→50px, 20K third row 77→50px) with widened KPS/Total filling the ends — matching KorenResourcePack's uniform layout style.
- **Ghost rain independent height/speed/width**: New `GhostRainHeightRow1/2/3`, `GhostRainSpeedRow1/2/3`, `GhostRainWidthRow1/2/3` sliders under the ghost rain section.

### 🐛 Bug Fixes
- **12K/10K grid rain position**: Back row keys with non-standard widths (12K key 8/10=77px, 10K key 8/9=129px) now share the front row's RainLine (key 9→2, 8→3, 10→4, 11→5 for 12K; 8→3, 9→4 for 10K). Rain drops align with the front column instead of centering within wide keys.
- **Rain render order with shared containers**: Back row drops hidden behind front row drops in shared containers. Fixed by `SetSiblingIndex((row-1)*2 + isGhost)` — front=0, middle=2, third=4, ghost=base+1.
- **14K layout jump**: Switching from 16K to 14K dropped the overlay by 21px. All 14K Y values normalized to 16K (frontY 299→320, backY 245→266, KPS 199→220).
- **8K layout too high with DownLocation**: 8K repositioned as "16K minus second row" — frontY=266 (16K's back row position), KPS/Total y=220 (16K KPS position). DownLocation now aligns KPS at y=20 matching 16K.
- **20K third row ghost key UI reversed**: Ghost key buttons showed 16,17,18,19 but visual layout is 17,16,18,19. Now uses `BackSequence20[8..12]`.
- **Custom position foldout conflated with toggle**: Foldout was acting as on/off switch. Now separate expand state `CustomPositionExpanded` + internal enable toggle.
- **DrawMainKeyRows third row loop guard**: `i < keyCodes.Length` was checking loop index instead of `backSequence[i]`. Corrected guard.
- **24K third row rain not showing**: `IsRainEnabledForKey` capped at index 20, blocking indices 20-23. Changed to `keyIndex < FootKeyBase`.
- **24K KPS/Total overlapping third row**: KPS/Total at y=165 overlapped with third row (y=232). Raised to y=221 with 6px gap.
- **24K layout switching stale keys**: Switching out of 24K left Keys[20-23] with stale 24K main key objects. Fixed `ChangeKeyViewer()` to also call `ResetFootKeyViewer()`.
- **24K foot key limit removed**: MaxKeySlots=40 allows 14K/16K foot keys in 24K mode (indices 24-39).
- **Crash on settings load**: `InitPerKeyColors` called `FootKeyBase` before `Settings` was assigned, causing NullReferenceException. Replaced with `MaxKeySlots` constant.

### 🧹 Refactor
- **Rain container sharing**: `ApplyRainContainerSharing()` / `ShareRainContainer()` redirects back-row `key.rain` to front row's RainLine. `UpdateRainContainerPositions` uses `HashSet<RectTransform>` dedup.
- **Dynamic foot key base**: `FootKeyBase` property replaces hardcoded 20 throughout the codebase (24 for 24K, 20 otherwise).
- **MaxKeySlots=40**: All key arrays (`Keys`, `Count`, `keyPressTimes`, `lastPerKeyKps`) use `MaxKeySlots`. Per-key color arrays use `MaxKeySlots + 2`. `KeyIndex` returns `Keys.Length`/`+1` for KPS/Total. All hardcoded 36/37/38/20 boundaries removed.
- **HasThirdRow property**: Replaces `style == Key20` checks in 18 locations across rain settings, shadow/outline, and ghost rain sections. Covers both 20K and 24K.

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
