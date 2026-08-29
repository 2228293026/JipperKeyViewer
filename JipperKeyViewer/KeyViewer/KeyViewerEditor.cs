// FreeMake editor — an independent IMGUI window (its own MonoBehaviour OnGUI, a root-level
// GUI.Window that floats above the loader's settings panel). Canvas interactions: draw-order
// hit testing, click-picking that prefers the already-selected node in an overlap stack,
// double-click cycling through stacked hits, incremental drag deltas with a snap correction
// layered on top (the mouse never fights the snap), screen-edge/center snapping, "actually
// aligned" guide lines, and marquee selection with Shift-to-select locked background images.
// Undo is a whole-list JSON snapshot stack (EditorHistory).
// FreeMake 编辑器——独立 IMGUI 弹窗（挂在组件自己的 OnGUI 上，根级 GUI.Window，浮于加载器
// 设置面板之上）。画布交互：绘制序命中测试、重叠栈中优先保持已选中项、双击在堆叠命中间
// 循环拣选、累计增量式拖拽叠加吸附修正（鼠标不会与吸附打架）、屏幕边缘/中心吸附、"实际
// 对齐才显示"的对齐线、框选 + Shift 选锁定背景图。撤销为整表 JSON 快照栈（EditorHistory）。

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace JipperKeyViewer.KeyViewer
{
    public partial class KeyViewer
    {
        private const int FmWindowId = 7717;
        private const float FmDoubleClickTime = 0.35f;
        private const float FmDoubleClickDist = 8f;
        private const float FmMinWindowWidth = 720f;
        private const float FmMinWindowHeight = 520f;
        private const float FmMinimapWidth = 150f;

        private enum FmGesture { None, Pending, DragNodes, Marquee, Resize }

        private bool editorOpen;
        private Rect editorRect = new Rect(120f, 90f, 1100f, 720f);
        private bool editorNeedsCentre = true;
        private Vector2 fmScroll = Vector2.zero; // pan offset in screen px / 平移偏移（屏幕像素）
        private float fmZoom = 1f;
        private bool fmHasKeyFocus;
        private bool fmResizing;

        private readonly List<KvNode> editorSelection = new List<KvNode>();
        private readonly List<KvNode> editorClipboard = new List<KvNode>();
        private int editorPasteSerial;
        private readonly EditorHistory editorHistory = new EditorHistory();

        private FmGesture fmGesture;
        private bool fmPointerDown;
        private Vector2 fmPressScreen;
        private Vector2 fmPressCanvas;
        private Vector2 fmMarqueeStart;
        private Vector2 fmMarqueeCur;
        private bool fmDragArmed;
        private bool fmDragMoved;
        private bool fmAxisLocked;
        private bool fmLockToX;
        private float fmDragTotalX;
        private float fmDragTotalY;
        private string fmPendingSnapshot;
        private readonly Dictionary<KvNode, Vector2> fmDragStart = new Dictionary<KvNode, Vector2>();
        private readonly List<KvNode> editorSelectionAtPress = new List<KvNode>();
        /// <summary>The ACTIVE node of the selection — the last one the user clicked. The
        /// property panel shows THIS node's values (falling back to the list head when the
        /// selection came from a marquee/select-all, which has no click order). /
        /// 选区的活动节点——用户最后点击的那个。属性面板显示它的值（框选/全选没有点击顺序，
        /// 回落到列表首项）。</summary>
        private KvNode fmActiveNode;
        private readonly List<KvNode> fmHitBuffer = new List<KvNode>();
        private float fmLastClickTime = -10f;
        private Vector2 fmLastClickPos = new Vector2(float.MinValue, float.MinValue);
        private KvNode fmCaptureNode;
        /// <summary>Node whose GHOST key is being captured (separate from the main binding
        /// capture). / 正在捕获鬼键的节点（与主键绑定捕获相互独立）。</summary>
        private KvNode fmCaptureGhostNode;
        private Vector2 fmPropsScroll;
        private GUIStyle fmNodeLabelStyle;
        private GUIStyle fmHintStyle;
        private readonly Dictionary<string, Texture2D> fmTexCache = new Dictionary<string, Texture2D>();
        private readonly List<string> fmScratchKeys = new List<string>();
        private readonly List<KvNode> fmOrderBuffer = new List<KvNode>();
        private bool fmGroupsExpanded;
        private Rect fmLastCanvasRect;
        private int fmResizeHandle = -1;
        private bool fmResizeMoved;
        private Rect fmResizeBBox;
        private readonly List<KeyValuePair<KvNode, Rect>> fmResizeOrig = new List<KeyValuePair<KvNode, Rect>>();
        private readonly List<float> fmSiblingW = new List<float>();
        private readonly List<float> fmSiblingH = new List<float>();

        private struct FmAlignLine
        {
            public bool Vertical;
            public float Coord, Min, Max;
        }

        private readonly List<FmAlignLine> fmAlignLines = new List<FmAlignLine>();

        /// <summary>Open the editor (called from the Layout tab) / 打开编辑器（布局页按钮调用）</summary>
        internal void OpenFreeMakeEditor()
        {
            editorOpen = true;
            editorNeedsCentre = true;
        }

        /// <summary>Root-level window host: a plain MonoBehaviour OnGUI keeps the popup fully
        /// independent of the loader's own window nesting. / 根级窗口宿主：组件自身的 OnGUI 让
        /// 弹窗完全独立于加载器窗口的嵌套。</summary>
        private void OnGUI()
        {
            if (!editorOpen || Settings == null) return;
            Event e = Event.current;
            if (e != null && e.type == EventType.MouseDown)
                fmHasKeyFocus = editorRect.Contains(e.mousePosition);
            editorRect.width = Mathf.Clamp(editorRect.width, FmMinWindowWidth, Mathf.Max(FmMinWindowWidth, Screen.width));
            editorRect.height = Mathf.Clamp(editorRect.height, FmMinWindowHeight, Mathf.Max(FmMinWindowHeight, Screen.height));
            editorRect.x = Mathf.Clamp(editorRect.x, 0f, Mathf.Max(0f, Screen.width - editorRect.width));
            editorRect.y = Mathf.Clamp(editorRect.y, 0f, Mathf.Max(0f, Screen.height - editorRect.height));
            editorRect = GUI.Window(FmWindowId, editorRect, DrawFreeMakeWindow, "FreeMake — " + (Settings.CurrentProfile ?? ""));
        }

        private void DrawFreeMakeWindow(int id)
        {
            Event e = Event.current;
            if (!IsCustomLayout)
            {
                GUILayout.Label(I18n.Tr("fm_not_custom_hint"), GUILayout.MinWidth(320f));
                if (GUILayout.Button(I18n.Tr("fm_switch_custom"), GUILayout.Height(30f)))
                {
                    Settings.Data.KeyViewerStyle = KeyviewerStyle.Custom;
                    ChangeKeyViewer();
                    SaveSettingsFromGui();
                    editorNeedsCentre = true;
                }
                GUI.DragWindow(new Rect(0, 0, 10000f, 24f));
                return;
            }

            DrawEditorToolbar();

            Rect canvasRect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.MinHeight(160f), GUILayout.ExpandHeight(true));
            HandleEditorCanvas(canvasRect, e);

            fmPropsScroll = GUILayout.BeginScrollView(fmPropsScroll, GUILayout.Height(280f));
            DrawEditorProperties();
            GUILayout.EndScrollView();

            HandleEditorResize(e);
            GUI.DragWindow(new Rect(0, 0, 10000f, 24f));
            EditorGcBuffers();
        }

        // ======================== toolbar / 工具栏 ========================

        private void DrawEditorToolbar()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(I18n.Tr("fm_add_key"), GUILayout.Width(64f))) EditorAddNode(0);
            if (GUILayout.Button(I18n.Tr("fm_add_kps"), GUILayout.Width(58f))) EditorAddNode(1);
            if (GUILayout.Button(I18n.Tr("fm_add_total"), GUILayout.Width(64f))) EditorAddNode(2);
            if (GUILayout.Button(I18n.Tr("fm_add_image"), GUILayout.Width(64f))) EditorAddNode(3);
            GUILayout.Space(6f);
            if (GUILayout.Button(I18n.Tr("fm_copy"), GUILayout.Width(46f))) EditorCopySelection();
            if (GUILayout.Button(I18n.Tr("fm_paste"), GUILayout.Width(46f))) EditorPaste();
            if (GUILayout.Button(I18n.Tr("fm_delete"), GUILayout.Width(46f))) EditorDeleteSelection();
            GUILayout.Space(6f);
            GUI.enabled = editorHistory.CanUndo;
            if (GUILayout.Button(I18n.Tr("fm_undo"), GUILayout.Width(46f))) EditorUndo();
            GUI.enabled = editorHistory.CanRedo;
            if (GUILayout.Button(I18n.Tr("fm_redo"), GUILayout.Width(46f))) EditorRedo();
            GUI.enabled = true;
            GUILayout.Space(6f);
            if (GUILayout.Button(I18n.Tr("fm_select_all"), GUILayout.Width(56f))) EditorSelectAll();
            if (GUILayout.Button(I18n.Tr("fm_clear_sel"), GUILayout.Width(56f))) editorSelection.Clear();
            GUILayout.Space(6f);
            // Built-in layout presets (the fixed layouts' hardcoded arrangements, generated as
            // editable nodes) + canvas wipe. Both push history — Ctrl+Z restores. /
            // 内置布局预设（固定布局的硬编码排布生成为可编辑节点）与清空画布。两者都入撤销栈
            // ——Ctrl+Z 可恢复。
            if (GUILayout.Button(I18n.Tr("fm_presets"), GUILayout.Width(46f))) fmPresetStripOpen = !fmPresetStripOpen;
            if (GUILayout.Button(I18n.Tr("fm_wipe"), GUILayout.Width(66f))) EditorWipeCanvas();
            GUILayout.FlexibleSpace();
            GUILayout.Label(string.Format(I18n.Tr("fm_status"), Settings.Data.CustomNodes.Count, editorSelection.Count),
                GUILayout.Width(120f));
            if (GUILayout.Button(I18n.Tr("fm_close"), GUILayout.Width(46f)))
            {
                // Closing the editor flushes any pending debounced changes. /
                // 关闭编辑器时冲刷挂起的去抖变更。
                editorOpen = false;
                SaveSettings();
            }
            GUILayout.EndHorizontal();
            // Preset strip: one click applies a built-in layout as editable nodes. /
            // 预设条：一键把内置布局生成为可编辑节点。
            if (fmPresetStripOpen)
            {
                string[] names = new string[KeyLayoutNames.Length - 1]; // skip only Custom / 仅跳过「自定义」
                Array.Copy(KeyLayoutNames, names, names.Length);
                int picked = GUILayout.SelectionGrid(-1, names, names.Length, GUILayout.Height(22f));
                if (picked >= 0)
                {
                    fmPresetStripOpen = false;
                    EditorApplyPreset(picked);
                }
            }
        }

        private bool fmPresetStripOpen;

        /// <summary>Apply a built-in layout preset NON-DESTRUCTIVELY: the preset lands in a NEW
        /// profile (global settings cloned from the current one, nodes replaced by the preset) and
        /// the editor switches to it — the current layout is untouched. / 非破坏性地应用内置布局
        /// 预设：预设落进一个新建的 Profile（全局设置克隆自当前配置，节点替换为预设）并切换
        /// 过去——当前布局原封不动。</summary>
        private void EditorApplyPreset(int styleIndex)
        {
            if (styleIndex < 0 || styleIndex >= 8) return; // 0-7 = 12K/16K/20K/10K/8K/14K/24K/108K
            KeyviewerStyle style = (KeyviewerStyle)styleIndex;
            List<KvNode> nodes = BuildPresetNodes(style, out int nextId);
            // Flush the CURRENT layout to its file before switching. / 切换前先把当前布局落盘。
            SaveCurrentProfile();
            // Free profile name: "16K-预设", "16K-预设 2", ... / 空闲配置名。
            string baseName = KeyLayoutNames[styleIndex] + "-" + I18n.Tr("fm_presets");
            var existing = new HashSet<string>(Settings.ProfileNames ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            string name = baseName;
            for (int k = 2; existing.Contains(name); k++) name = baseName + " " + k;
            // Clone the whole ProfileData so global settings/colors/fonts carry over, then swap in
            // the preset nodes. / 整体克隆 ProfileData 以携带全局设置/配色/字体，再换入预设节点。
            ProfileData pd = JsonConvert.DeserializeObject<ProfileData>(
                JsonConvert.SerializeObject(Settings.Data, ProfileData.ProfileSerializer),
                ProfileData.ProfileSerializer);
            if (pd == null) return;
            pd.CustomNodes = nodes;
            pd.CustomNodeNextId = nextId;
            Settings.CurrentProfile = name;
            var list = new List<string>(Settings.ProfileNames ?? Array.Empty<string>()) { name };
            Settings.ProfileNames = list.ToArray();
            Settings.Data = pd;
            EnsureCustomNodes();
            editorSelection.Clear();
            // Undo snapshots belong to the previous profile — restoring them here would write the
            // old layout's nodes into the preset profile. / 撤销快照属于原配置——在这里恢复会把
            // 旧布局的节点写进预设配置。
            editorHistory.Clear();
            EditorMutated();
        }

        private List<KvNode> BuildPresetNodes(KeyviewerStyle style, out int nextId)
        {
            List<KvNode> nodes = new List<KvNode>();
            LayoutDesc layout = GetLayout(style);
            // Must mirror GetKeyCode's mapping — a missing case (Key16 was) silently fell to the
            // key24 default and preset nodes got the wrong profile's bindings. Texts likewise. /
            // 必须与 GetKeyCode 的映射一致——漏掉某个 case（此前漏了 Key16）会静默落到默认
            // 分支，预设节点就带上了错误的绑定数组。文本同理。
            string[] texts;
            KeyCode[] binds;
            switch (style)
            {
                case KeyviewerStyle.Key8: binds = Settings.Data.key8; texts = Settings.Data.key8Text; break;
                case KeyviewerStyle.Key10: binds = Settings.Data.key10; texts = Settings.Data.key10Text; break;
                case KeyviewerStyle.Key12: binds = Settings.Data.key12; texts = Settings.Data.key12Text; break;
                case KeyviewerStyle.Key14: binds = Settings.Data.key14; texts = Settings.Data.key14Text; break;
                case KeyviewerStyle.Key16: binds = Settings.Data.key16; texts = Settings.Data.key16Text; break;
                case KeyviewerStyle.Key20: binds = Settings.Data.key20; texts = Settings.Data.key20Text; break;
                case KeyviewerStyle.Key24: binds = Settings.Data.key24; texts = Settings.Data.key24Text; break;
                case KeyviewerStyle.Full108: binds = Settings.Data.key108; texts = null; break; // no per-key texts on 108K / 108K 无每键文本
                default: binds = Settings.Data.key16; texts = Settings.Data.key16Text; break;
            }
            int id = Settings.Data.CustomNodeNextId;
            KvNode NewNode(int type, float x, float centerY, float w, float h) => new KvNode
            {
                NodeType = type,
                Id = id++,
                X = x,
                Y = 1080f - centerY - h * 0.5f,
                Width = w,
                Height = h,
            };
            int[] counts = Settings.Data.Count;
            // 108K full keyboard: the shared slot table + the same 56px column-step/6px gap
            // math the fixed layout uses; tall keys (+/Enter) span two rows. The fixed keyboard
            // never rains, so preset nodes start with rain off. / 108K 全键盘：共享槽位表 +
            // 与固定布局相同的 56px 列步进/6px 间隙换算；竖长键（+/回车）跨两行。固定全键盘
            // 从不下雨，预设节点雨滴默认关闭。
            if (style == KeyviewerStyle.Full108)
            {
                const float U = 50f;
                const float colStep = 56f;
                const float gap = 6f;
                const float rightClusterShift = 4f * colStep;
                const float rowStep = 56f;
                foreach (var s in Full108SlotTable())
                {
                    float x = (float)s.x * colStep / U - (s.idx >= 78 ? rightClusterShift : 0f);
                    float w, h, cy;
                    if (s.idx == 95 || s.idx == 104)
                    {
                        w = colStep - gap;
                        h = rowStep + 50f;
                        cy = (s.idx == 95 ? (468f + 412f) : (300f + 356f)) * 0.5f;
                    }
                    else
                    {
                        w = (float)s.w * colStep / U - gap;
                        h = 50f;
                        cy = (float)s.y;
                    }
                    KvNode n = NewNode(0, x, cy, w, h);
                    KeyCode kc = s.idx < binds.Length ? binds[s.idx] : KeyCode.None;
                    if (kc != KeyCode.None) n.KeyBind = kc.ToString();
                    if (s.idx < counts.Length) n.Count = counts[s.idx];
                    nodes.Add(n);
                }
                if (Settings.Data.FullKeyboardShowKpsTotal)
                {
                    float ktW = Settings.Data.FullKeyboardKpsTotalSize;
                    KvNode kps = NewNode(1, Settings.Data.FullKpsPosition.x * CanvasWidth,
                        (1f - Settings.Data.FullKpsPosition.y) * 1080f, ktW, 30f);
                    kps.CustomText = Settings.Data.KpsLabel;
                    nodes.Add(kps);
                    KvNode total = NewNode(2, Settings.Data.FullTotalPosition.x * CanvasWidth,
                        (1f - Settings.Data.FullTotalPosition.y) * 1080f, ktW, 30f);
                    total.CustomText = Settings.Data.TotalLabel;
                    nodes.Add(total);
                }
                nextId = id;
                return nodes;
            }
            // Front row: 8 keys at the fixed layout's front-Y. Counts and custom texts carry over
            // from the source profile's per-slot arrays. / 前排：固定布局前排 Y 上的 8 键。
            // 计数与自定义文本按槽位从源配置的数组继承。
            for (int i = 0; i < 8 && i < binds.Length; i++)
            {
                KvNode n = NewNode(0, 54f * i, layout.frontY, 50f, 50f);
                if (binds[i] != KeyCode.None) n.KeyBind = binds[i].ToString();
                if (i < counts.Length) n.Count = counts[i];
                if (texts != null && i < texts.Length) n.CustomText = texts[i] ?? "";
                n.RainEnabled = true;
                nodes.Add(n);
            }
            // Back extras + KPS/Total from the same LayoutDesc the fixed layout uses. /
            // 后排扩展与 KPS/Total 来自固定布局用的同一 LayoutDesc。
            if (layout.extras != null)
            {
                foreach (var e in layout.extras)
                {
                    if (e.index == -1 || e.index == -2)
                    {
                        KvNode n = NewNode(e.index == -1 ? 1 : 2, e.x, e.y, e.w, e.slim ? 30f : 50f);
                        n.CustomText = e.index == -1 ? Settings.Data.KpsLabel : Settings.Data.TotalLabel;
                        nodes.Add(n);
                    }
                    else if (e.index >= 0 && e.index < binds.Length)
                    {
                        KvNode n = NewNode(0, e.x, e.y, e.w, 50f);
                        if (binds[e.index] != KeyCode.None) n.KeyBind = binds[e.index].ToString();
                        if (e.index < counts.Length) n.Count = counts[e.index];
                        if (texts != null && e.index < texts.Length) n.CustomText = texts[e.index] ?? "";
                        n.RainEnabled = true;
                        n.RainRow = Mathf.Clamp(e.rainRow, 0, 2);
                        nodes.Add(n);
                    }
                }
            }
            // Foot keys — same geometry as the fixed foot layout, when one is set. Counts/texts
            // map to the FootKeyBase-anchored slots. / 脚键——设置了脚键布局时，按固定脚键的
            // 几何生成。计数/文本按 FootKeyBase 起的槽位映射。
            int footSize = FootKeySize(Settings.Data.FootKeyViewerStyle);
            if (footSize > 0)
            {
                KeyCode[] footBinds = GetFootKeyCode();
                string[] footTexts = Settings.Data.FootKeyViewerStyle switch
                {
                    FootKeyviewerStyle.Key2 => Settings.Data.footkey2Text,
                    FootKeyviewerStyle.Key4 => Settings.Data.footkey4Text,
                    FootKeyviewerStyle.Key6 => Settings.Data.footkey6Text,
                    FootKeyviewerStyle.Key8 => Settings.Data.footkey8Text,
                    FootKeyviewerStyle.Key10 => Settings.Data.footkey10Text,
                    FootKeyviewerStyle.Key12 => Settings.Data.footkey12Text,
                    FootKeyviewerStyle.Key14 => Settings.Data.footkey14Text,
                    FootKeyviewerStyle.Key16 => Settings.Data.footkey16Text,
                    _ => null,
                };
                for (int i = 0; i < footSize && i < footBinds.Length; i++)
                {
                    int col = footSize <= 8 || i < 8 ? i : i - 8;
                    int row = footSize <= 8 || i < 8 ? 0 : 1;
                    int baseY = footSize > 8 ? 15 + 34 : 15;
                    int x = 432 + col * 34;
                    if (footSize > 8 && row == 1) x += (8 - (footSize - 8)) * 17;
                    KvNode n = NewNode(0, x, baseY - row * 34, 30f, 30f);
                    if (footBinds[i] != KeyCode.None) n.KeyBind = footBinds[i].ToString();
                    int slot = FootKeyBase + i;
                    if (slot < counts.Length) n.Count = counts[slot];
                    if (footTexts != null && i < footTexts.Length) n.CustomText = footTexts[i] ?? "";
                    nodes.Add(n);
                }
            }
            nextId = id;
            return nodes;
        }

        /// <summary>Clear the whole canvas (undoable). / 清空整个画布（可撤销）。</summary>
        private void EditorWipeCanvas()
        {
            if (Settings.Data.CustomNodes.Count == 0) return;
            PushEditorHistory();
            Settings.Data.CustomNodes = new List<KvNode>();
            editorSelection.Clear();
            EditorMutated();
        }

        /// <summary>Seed a node's per-node rain shadow from its selected rain row's settings. /
        /// 用节点所选雨滴排的设置为其节点级雨滴阴影做种子。</summary>
        private static void SeedRainShadowFromRow(KvNode n)
        {
            ProfileData d = Settings.Data;
            int row = Mathf.Clamp(n.RainRow, 0, 2) + 1; // 1..3
            n.RainShadowEnabled = row == 1 ? d.EnableRainShadowRow1 : row == 2 ? d.EnableRainShadowRow2 : d.EnableRainShadowRow3;
            Color c = row == 1 ? d.RainShadowColorRow1 : row == 2 ? d.RainShadowColorRow2 : d.RainShadowColorRow3;
            n.RainShadowColor = new[] { c.r, c.g, c.b, c.a };
            n.RainShadowOffsetX = row == 1 ? d.RainShadowOffsetXRow1 : row == 2 ? d.RainShadowOffsetXRow2 : d.RainShadowOffsetXRow3;
            n.RainShadowOffsetY = row == 1 ? d.RainShadowOffsetYRow1 : row == 2 ? d.RainShadowOffsetYRow2 : d.RainShadowOffsetYRow3;
        }

        /// <summary>Seed a node's per-node rain outline from its selected rain row's settings. /
        /// 用节点所选雨滴排的设置为其节点级雨滴描边做种子。</summary>
        private static void SeedRainOutlineFromRow(KvNode n)
        {
            ProfileData d = Settings.Data;
            int row = Mathf.Clamp(n.RainRow, 0, 2) + 1; // 1..3
            n.RainOutlineEnabled = row == 1 ? d.EnableRainOutlineRow1 : row == 2 ? d.EnableRainOutlineRow2 : d.EnableRainOutlineRow3;
            Color c = row == 1 ? d.RainOutlineColorRow1 : row == 2 ? d.RainOutlineColorRow2 : d.RainOutlineColorRow3;
            n.RainOutlineColor = new[] { c.r, c.g, c.b, c.a };
            n.RainOutlineWidth = row == 1 ? d.RainOutlineWidthRow1 : row == 2 ? d.RainOutlineWidthRow2 : d.RainOutlineWidthRow3;
        }

        /// <summary>Seed a node's per-node GHOST rain shadow from its selected ghost rain row. /
        /// 用节点所选排的鬼雨阴影设置为其节点级鬼雨阴影做种子。</summary>
        private static void SeedGhostRainShadowFromRow(KvNode n)
        {
            ProfileData d = Settings.Data;
            int row = Mathf.Clamp(n.RainRow, 0, 2) + 1; // 1..3
            n.GhostRainShadowEnabled = row == 1 ? d.EnableGhostRainShadowRow1 : row == 2 ? d.EnableGhostRainShadowRow2 : d.EnableGhostRainShadowRow3;
            Color c = row == 1 ? d.GhostRainShadowColorRow1 : row == 2 ? d.GhostRainShadowColorRow2 : d.GhostRainShadowColorRow3;
            n.GhostRainShadowColor = new[] { c.r, c.g, c.b, c.a };
            n.GhostRainShadowOffsetX = row == 1 ? d.GhostRainShadowOffsetXRow1 : row == 2 ? d.GhostRainShadowOffsetXRow2 : d.GhostRainShadowOffsetXRow3;
            n.GhostRainShadowOffsetY = row == 1 ? d.GhostRainShadowOffsetYRow1 : row == 2 ? d.GhostRainShadowOffsetYRow2 : d.GhostRainShadowOffsetYRow3;
        }

        /// <summary>Seed a node's per-node GHOST rain outline from its selected ghost rain row. /
        /// 用节点所选排的鬼雨描边设置为其节点级鬼雨描边做种子。</summary>
        private static void SeedGhostRainOutlineFromRow(KvNode n)
        {
            ProfileData d = Settings.Data;
            int row = Mathf.Clamp(n.RainRow, 0, 2) + 1; // 1..3
            n.GhostRainOutlineEnabled = row == 1 ? d.EnableGhostRainOutlineRow1 : row == 2 ? d.EnableGhostRainOutlineRow2 : d.EnableGhostRainOutlineRow3;
            Color c = row == 1 ? d.GhostRainOutlineColorRow1 : row == 2 ? d.GhostRainOutlineColorRow2 : d.GhostRainOutlineColorRow3;
            n.GhostRainOutlineColor = new[] { c.r, c.g, c.b, c.a };
            n.GhostRainOutlineWidth = row == 1 ? d.GhostRainOutlineWidthRow1 : row == 2 ? d.GhostRainOutlineWidthRow2 : d.GhostRainOutlineWidthRow3;
        }

        private void EditorAddNode(int type)
        {
            if (type == 1 && Settings.Data.CustomNodes.Any(n => n != null && n.NodeType == 1)) return;
            if (type == 2 && Settings.Data.CustomNodes.Any(n => n != null && n.NodeType == 2)) return;
            if (type != 3 && CustomKeyNodeCount() >= CustomKeyNodeCap) return;
            if (type == 3 && Settings.Data.CustomNodes.Count(n => n != null && n.NodeType == 3) >= 8) return;
            PushEditorHistory();
            Vector2 center = EditorViewCenter();
            KvNode node = new KvNode
            {
                NodeType = type,
                Id = Settings.Data.CustomNodeNextId++,
                X = center.x - 30f,
                Y = center.y - 30f,
                Width = 60f,
                Height = 60f,
                Depth = Settings.Data.CustomNodes.Count > 0 ? Settings.Data.CustomNodes.Max(n => n.Depth) + 1 : 0,
            };
            if (type == 1) node.CustomText = Settings.Data.KpsLabel;
            if (type == 2) node.CustomText = Settings.Data.TotalLabel;
            Settings.Data.CustomNodes.Add(node);
            editorSelection.Clear();
            editorSelection.Add(node);
            EditorMutated();
        }

        private void EditorDeleteSelection()
        {
            if (editorSelection.Count == 0) return;
            PushEditorHistory();
            for (int i = editorSelection.Count - 1; i >= 0; i--)
                Settings.Data.CustomNodes.Remove(editorSelection[i]);
            editorSelection.Clear();
            EditorMutated();
        }

        private void EditorCopySelection()
        {
            editorClipboard.Clear();
            editorPasteSerial = 0;
            foreach (KvNode node in editorSelection)
                if (node != null) editorClipboard.Add(node.Clone());
        }

        private void EditorPaste()
        {
            if (editorClipboard.Count == 0) return;
            PushEditorHistory();
            editorPasteSerial++;
            float offset = 20f * editorPasteSerial;
            bool hasKps = Settings.Data.CustomNodes.Any(n => n != null && n.NodeType == 1);
            bool hasTotal = Settings.Data.CustomNodes.Any(n => n != null && n.NodeType == 2);
            List<KvNode> pasted = new List<KvNode>();
            foreach (KvNode template in editorClipboard)
            {
                if (template == null) continue;
                if (template.NodeType == 1 && hasKps) continue;
                if (template.NodeType == 2 && hasTotal) continue;
                KvNode copy = template.Clone();
                copy.Id = Settings.Data.CustomNodeNextId++;
                copy.X += offset;
                copy.Y += offset;
                Settings.Data.CustomNodes.Add(copy);
                pasted.Add(copy);
                if (copy.NodeType == 1) hasKps = true;
                if (copy.NodeType == 2) hasTotal = true;
            }
            if (pasted.Count == 0) return;
            editorSelection.Clear();
            editorSelection.AddRange(pasted);
            EditorMutated();
        }

        private void EditorSelectAll()
        {
            editorSelection.Clear();
            foreach (KvNode node in Settings.Data.CustomNodes)
                if (node != null) editorSelection.Add(node);
        }

        private void EditorMutated()
        {
            // Structural changes save IMMEDIATELY (they are discrete, low-rate events and the
            // user's layout must survive a crash). / 结构性变更立即落盘（离散低频事件，布局必须
            // 在崩溃后存活）。
            SaveSettings();
            Loader.Log($"KeyViewer: saved {Settings.Data.CustomNodes.Count} custom nodes");
            if (KeyViewerObject != null && IsCustomLayout) ResetKeyViewer();
        }

        private void RequestEditorRebuild()
        {
            if (KeyViewerObject != null && IsCustomLayout) ResetKeyViewer();
        }

        // ======================== history / 撤销 ========================

        private string SnapshotCustomNodes()
        {
            return JsonConvert.SerializeObject(Settings.Data.CustomNodes);
        }
        private void PushEditorHistory()
        {
            try
            {
                editorHistory.Push(SnapshotCustomNodes());
            }
            catch (Exception e)
            {
                Loader.Warning($"KeyViewer: editor snapshot failed: {e.Message}");
            }
        }

        private void EditorUndo()
        {
            string current = SnapshotCustomNodes();
            RestoreEditorSnapshot(editorHistory.Undo(current));
        }

        private void EditorRedo()
        {
            string current = SnapshotCustomNodes();
            RestoreEditorSnapshot(editorHistory.Redo(current));
        }

        private void RestoreEditorSnapshot(string snapshot)
        {
            if (snapshot == null) return;
            try
            {
                Settings.Data.CustomNodes = string.IsNullOrEmpty(snapshot)
                    ? new List<KvNode>()
                    : JsonConvert.DeserializeObject<List<KvNode>>(snapshot) ?? new List<KvNode>();
                EnsureCustomNodes();
                editorSelection.RemoveAll(n => !Settings.Data.CustomNodes.Contains(n));
                EditorMutated();
            }
            catch (Exception e)
            {
                Loader.Error($"KeyViewer: editor snapshot did not parse: {e.Message}");
            }
        }

        // ======================== canvas / 画布 ========================

        private Vector2 EditorOrigin(Rect rect)
        {
            return rect.center + fmScroll;
        }

        private Vector2 EditorCanvasOf(Rect rect, Vector2 screen)
        {
            return (screen - EditorOrigin(rect)) / Mathf.Max(0.05f, fmZoom);
        }

        private Vector2 EditorScreenOf(Rect rect, Vector2 canvasPos)
        {
            return EditorOrigin(rect) + canvasPos * fmZoom;
        }

        private void EditorZoomAt(Rect rect, Vector2 screen, float step)
        {
            float from = fmZoom;
            float to = Mathf.Clamp(from + step, 0.2f, 3f);
            if (Mathf.Approximately(from, to)) return;
            Vector2 anchor = EditorCanvasOf(rect, screen);
            fmZoom = to;
            // Keep the anchor point under the cursor: scroll = screen - origin adjustments. /
            // 让锚点保持在光标下：scroll = screen - origin 调整。
            fmScroll = screen - rect.center - anchor * to;
        }

        private Vector2 EditorViewCenter()
        {
            // View center in canvas coordinates; before the first layout pass fall back to the
            // screen-bounds center. / 画布坐标下的视口中心；首次布局前回退到屏幕范围中心。
            if (fmLastCanvasRect.width > 1f)
                return EditorCanvasOf(fmLastCanvasRect, fmLastCanvasRect.center);
            return new Vector2(CanvasWidth * 0.5f, 540f);
        }

        private void CentreEditorView(Rect rect)
        {
            float zoom = Mathf.Clamp(Mathf.Min(
                rect.width / (CanvasWidth + 80f),
                rect.height / (1080f + 80f)), 0.2f, 1.5f);
            fmZoom = zoom;
            fmScroll = -new Vector2(CanvasWidth * 0.5f, 540f) * zoom;
        }

        private void HandleEditorCanvas(Rect rect, Event e)
        {
            fmLastCanvasRect = rect;
            if (editorNeedsCentre && rect.width > 1f)
            {
                CentreEditorView(rect);
                editorNeedsCentre = false;
            }
            bool hover = rect.Contains(e.mousePosition);
            if (hover && e.type == EventType.ScrollWheel)
            {
                EditorZoomAt(rect, e.mousePosition, -e.delta.y * 0.06f);
                e.Use();
                return;
            }
            if (e.type == EventType.MouseDrag && e.button == 1 && hover)
            {
                // Right-drag pans the canvas / 右键拖动平移画布
                fmScroll += e.delta;
                e.Use();
                return;
            }
            if (e.type == EventType.Repaint)
            {
                DrawEditorCanvas(rect);
                DrawEditorMinimap(rect);
            }
            // Minimap viewport drag runs before canvas gestures and keeps working while the
            // cursor leaves the small box. Use() is only legal on input events — Layout/Repaint
            // must pass through untouched. / 小地图视口拖动先于画布手势处理，光标离开小框后
            // 仍持续生效。Use() 只对输入事件合法——Layout/Repaint 必须原样放行。
            if (fmMinimapDrag)
            {
                HandleEditorMinimapDrag(rect, e);
                if (e.type == EventType.MouseDrag || e.type == EventType.MouseUp || e.type == EventType.MouseDown)
                    e.Use();
                return;
            }
            if (HandleEditorMinimapStart(rect, e)) return;
            // Keyboard shortcuts: only while the window has mouse focus (last press inside) and
            // no editor text field is typing — HandleEditorShortcuts itself ignores non-KeyDown
            // events (Layout/Repaint carry KeyCode.None and fall through). /
            // 快捷键仅在窗口持有鼠标焦点（最后一次按下在窗口内）时处理；非 KeyDown 事件
            //（Layout/Repaint 的 KeyCode 为 None）在内部自然落空，不会触发 Use。
            if (fmHasKeyFocus && e.type == EventType.KeyDown)
                HandleEditorShortcuts(e);
            if (e.type == EventType.MouseDown && e.button == 0 && hover)
            {
                fmPointerDown = true;
                fmPressScreen = e.mousePosition;
                fmPressCanvas = EditorCanvasOf(rect, e.mousePosition);
                fmResizeHandle = EditorHandleHit(rect, e.mousePosition);
                if (fmResizeHandle >= 0)
                {
                    fmGesture = FmGesture.Resize;
                    BeginEditorResize();
                    e.Use();
                    return;
                }
                fmGesture = FmGesture.Pending;
                fmHitBuffer.Clear();
                fmHitBuffer.AddRange(HitTestEditorNodes(fmPressCanvas, e.shift));
                bool doubleClick = ConsumeEditorDoubleClick(e.mousePosition);
                KvNode target = PickEditorNode(fmHitBuffer, doubleClick, e.control);
                EditorApplyCanvasSelection(target, doubleClick, e.control);
                fmDragArmed = target != null && !e.control && !doubleClick && editorSelection.Contains(target);
                if (fmDragArmed) BeginNodeDrag();
                e.Use();
            }
            if (fmPointerDown && e.type == EventType.MouseDrag && e.button == 0)
            {
                UpdateEditorGesture(rect, e);
                e.Use();
            }
            if (fmPointerDown && e.type == EventType.MouseUp && e.button == 0)
            {
                EndEditorGesture();
                fmPointerDown = false;
                e.Use();
            }
            if (fmPointerDown && !Input.GetMouseButton(0))
            {
                EndEditorGesture();
                fmPointerDown = false;
            }
        }

        private void UpdateEditorGesture(Rect rect, Event e)
        {
            if (fmGesture == FmGesture.Resize)
            {
                UpdateEditorResize(rect, e);
                return;
            }
            if (fmGesture == FmGesture.Pending)
            {
                if ((e.mousePosition - fmPressScreen).sqrMagnitude < 25f) return;
                if (fmDragArmed) fmGesture = FmGesture.DragNodes;
                else if (fmHitBuffer.Count == 0) { fmGesture = FmGesture.Marquee; fmMarqueeStart = fmPressCanvas; }
                else { fmGesture = FmGesture.None; return; }
            }
            Vector2 canvasPos = EditorCanvasOf(rect, e.mousePosition);
            if (fmGesture == FmGesture.DragNodes)
            {
                fmDragTotalX += e.delta.x / Mathf.Max(0.05f, fmZoom);
                fmDragTotalY += e.delta.y / Mathf.Max(0.05f, fmZoom);
                if (e.delta.x != 0f || e.delta.y != 0f)
                {
                    if (!fmDragMoved)
                    {
                        // First real movement: commit the pre-drag snapshot taken at press. /
                        // 首次真实移动：提交按下时预留的拖拽前快照。
                        PushEditorHistorySnapshot(fmPendingSnapshot);
                        fmPendingSnapshot = null;
                        fmDragMoved = true;
                    }
                }
                if (!fmAxisLocked)
                {
                    fmLockToX = Mathf.Abs(e.delta.x) >= Mathf.Abs(e.delta.y);
                    fmAxisLocked = true;
                }
                float dx = fmDragTotalX, dy = fmDragTotalY;
                if (e.shift)
                {
                    if (fmLockToX) dy = 0f;
                    else dx = 0f;
                }
                ApplyEditorDrag(dx, dy, e.alt);
            }
            else if (fmGesture == FmGesture.Marquee)
            {
                fmMarqueeCur = canvasPos;
            }
        }

        private void EndEditorGesture()
        {
            if (fmGesture == FmGesture.Resize)
            {
                EndEditorResize();
            }
            else if (fmGesture == FmGesture.DragNodes)
            {
                EndNodeDrag();
            }
            else if (fmGesture == FmGesture.Marquee)
            {
                Rect band = EditorRectFromCorners(fmMarqueeStart, fmMarqueeCur);
                editorSelection.Clear();
                foreach (KvNode node in Settings.Data.CustomNodes)
                {
                    if (node == null) continue;
                    if (node.Unselectable && !Event.current.shift) continue;
                    Rect nodeRect = new Rect(node.X, node.Y, node.Width, node.Height);
                    if (nodeRect.Overlaps(band)) editorSelection.Add(node);
                }
            }
            fmGesture = FmGesture.None;
            fmAlignLines.Clear();
            fmDragStart.Clear();
            fmPendingSnapshot = null;
            fmResizeHandle = -1;
        }

        private static Rect EditorRectFromCorners(Vector2 a, Vector2 b)
        {
            return new Rect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
        }

        private void BeginNodeDrag()
        {
            fmDragStart.Clear();
            foreach (KvNode node in editorSelection)
                if (node != null) fmDragStart[node] = new Vector2(node.X, node.Y);
            fmDragTotalX = 0f;
            fmDragTotalY = 0f;
            fmDragMoved = false;
            fmAxisLocked = false;
            try
            {
                fmPendingSnapshot = SnapshotCustomNodes();
            }
            catch (Exception ex)
            {
                fmPendingSnapshot = null;
                Loader.Warning($"KeyViewer: editor snapshot failed: {ex.Message}");
            }
        }

        private void EndNodeDrag()
        {
            if (fmDragMoved)
            {
                SaveSettingsFromGui();
                RequestEditorRebuild();
            }
            fmAlignLines.Clear();
        }

        /// <summary>Live geometry sync during drag/resize gestures: update the EXISTING runtime
        /// keys in place (rect / text wrapper / image) instead of a throttled full rebuild — no
        /// object churn, no rain clearing, every frame. A full rebuild still normalizes
        /// everything at gesture end. / 拖动/缩放手势期间的实时几何同步：就地更新既有运行时
        /// 按键（矩形/文本容器/图片），取代节流整建——无对象抖动、不清雨滴、逐帧生效。手势
        /// 结束时仍做一次完整重建归一。</summary>
        private void ApplyLiveGeometry()
        {
            if (Keys == null || keyShapeLayer == null) return;
            foreach (KvNode node in editorSelection)
            {
                if (node == null || node.RuntimeKey == null) continue;
                Key key = node.RuntimeKey;
                float cy = CustomNodeCenterY(node);
                key.keySize = new Vector2(node.Width, node.Height);
                RectTransform rt = (RectTransform)key.transform;
                rt.anchoredPosition = new Vector2(node.X, cy);
                if (keyShapeLayer != null && key.shapeSlot >= 0)
                    keyShapeLayer.SetRect(key.shapeSlot, node.X, cy - node.Height * 0.5f, node.Width, node.Height);
                if (key.visuals != null)
                {
                    RectTransform vt = (RectTransform)key.visuals;
                    vt.anchoredPosition = new Vector2(node.X + node.Width * 0.5f, cy);
                    vt.sizeDelta = new Vector2(node.Width, node.Height);
                    // Keep the text width in sync with the box (height follows the layout mode). /
                    // 文本宽度与框同步（高度由布局模式决定）。
                    if (key.text != null)
                        key.text.rectTransform.sizeDelta = new Vector2(node.Width - 4f, key.text.rectTransform.sizeDelta.y);
                    if (key.value != null)
                        key.value.rectTransform.sizeDelta = new Vector2(node.Width - 4f, key.value.rectTransform.sizeDelta.y);
                }
                if (key.CustomImageRect != null)
                {
                    key.CustomImageRect.anchoredPosition = new Vector2(node.X + node.Width * 0.5f, cy);
                    key.CustomImageRect.sizeDelta = new Vector2(node.Width, node.Height);
                }
            }
        }

        // ======================== resize handles / 缩放手柄 ========================
        // Single selection gets 8-way handles; multi selection gets 4 corner handles that
        // scale the whole bounding box from the opposite corner (whole-box
        // resize). / 单选八向手柄；多选四角手柄自对角整体缩放包围盒（整体
        // 缩放语义）。

        private static readonly (int dx, int dy)[] FmHandleDirs8 =
            { (-1, -1), (0, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (0, 1), (1, 1) };
        private static readonly (int dx, int dy)[] FmHandleDirs4 =
            { (-1, -1), (1, -1), (-1, 1), (1, 1) };

        private (int dx, int dy)[] EditorActiveHandleDirs()
        {
            return editorSelection.Count == 1 ? FmHandleDirs8 : FmHandleDirs4;
        }

        private Rect EditorSelectionBounds()
        {
            Rect bbox = new Rect(float.MaxValue, float.MaxValue, 0f, 0f);
            bool any = false;
            foreach (KvNode node in editorSelection)
            {
                if (node == null) continue;
                Rect r = new Rect(node.X, node.Y, node.Width, node.Height);
                if (!any)
                {
                    bbox = r;
                    any = true;
                    continue;
                }
                bbox = Rect.MinMaxRect(
                    Mathf.Min(bbox.x, r.x), Mathf.Min(bbox.y, r.y),
                    Mathf.Max(bbox.xMax, r.xMax), Mathf.Max(bbox.yMax, r.yMax));
            }
            return any ? bbox : new Rect(0f, 0f, 0f, 0f);
        }

        private static Vector2 FmHandlePoint(Rect r, int dx, int dy)
        {
            return new Vector2(dx < 0 ? r.x : dx > 0 ? r.xMax : r.center.x,
                dy < 0 ? r.y : dy > 0 ? r.yMax : r.center.y);
        }

        private int EditorHandleHit(Rect canvasScreenRect, Vector2 screen)
        {
            if (editorSelection.Count == 0) return -1;
            Rect bbox = EditorSelectionBounds();
            var dirs = EditorActiveHandleDirs();
            for (int i = 0; i < dirs.Length; i++)
            {
                Vector2 sp = EditorScreenOf(canvasScreenRect, FmHandlePoint(bbox, dirs[i].dx, dirs[i].dy));
                if (Mathf.Abs(screen.x - sp.x) <= 7f && Mathf.Abs(screen.y - sp.y) <= 7f) return i;
            }
            return -1;
        }

        private void BeginEditorResize()
        {
            fmResizeOrig.Clear();
            foreach (KvNode node in editorSelection)
                if (node != null)
                    fmResizeOrig.Add(new KeyValuePair<KvNode, Rect>(node, new Rect(node.X, node.Y, node.Width, node.Height)));
            fmResizeBBox = EditorSelectionBounds();
            fmResizeMoved = false;
            // Sibling sizes for size-snapping during the resize. /
            // 兄弟节点尺寸表，供缩放时的尺寸吸附。
            fmSiblingW.Clear();
            fmSiblingH.Clear();
            foreach (KvNode node in Settings.Data.CustomNodes)
            {
                if (node == null || editorSelection.Contains(node)) continue;
                fmSiblingW.Add(node.Width);
                fmSiblingH.Add(node.Height);
            }
            try
            {
                fmPendingSnapshot = SnapshotCustomNodes();
            }
            catch (Exception ex)
            {
                fmPendingSnapshot = null;
                Loader.Warning($"KeyViewer: editor snapshot failed: {ex.Message}");
            }
        }

        private void UpdateEditorResize(Rect rect, Event e)
        {
            Vector2 canvasPos = EditorCanvasOf(rect, e.mousePosition);
            if (!fmResizeMoved)
            {
                if ((canvasPos - fmPressCanvas).sqrMagnitude < 0.01f) return;
                PushEditorHistorySnapshot(fmPendingSnapshot);
                fmPendingSnapshot = null;
                fmResizeMoved = true;
            }
            const float minSize = 10f;
            (int dx, int dy) = EditorActiveHandleDirs()[fmResizeHandle];
            if (editorSelection.Count == 1 && fmResizeOrig.Count > 0)
            {
                KvNode node = fmResizeOrig[0].Key;
                Rect o = fmResizeOrig[0].Value;
                float w = dx == 0 ? o.width : dx > 0 ? o.width + (canvasPos.x - fmPressCanvas.x) : o.width - (canvasPos.x - fmPressCanvas.x);
                float h = dy == 0 ? o.height : dy > 0 ? o.height + (canvasPos.y - fmPressCanvas.y) : o.height - (canvasPos.y - fmPressCanvas.y);
                w = Mathf.Max(minSize, w);
                h = Mathf.Max(minSize, h);
                // Size snapping: grid round + match sibling sizes (±4px); Alt bypasses. /
                // 尺寸吸附：网格取整 + 匹配兄弟节点尺寸（±4px）；Alt 临时关闭。
                if (!e.alt)
                {
                    if (dx != 0) w = EditorSnapSize(w, fmSiblingW);
                    if (dy != 0) h = EditorSnapSize(h, fmSiblingH);
                }
                if (e.shift && o.width > 0f && o.height > 0f)
                {
                    float aspect = o.width / o.height;
                    if (dx != 0 && dy != 0)
                    {
                        if (Mathf.Abs(w - o.width) >= Mathf.Abs(h - o.height)) h = w / aspect;
                        else w = h * aspect;
                    }
                    else if (dx != 0) h = w / aspect;
                    else if (dy != 0) w = h * aspect;
                }
                w = Mathf.Max(minSize, w);
                h = Mathf.Max(minSize, h);
                node.Width = w;
                node.Height = h;
                node.X = dx < 0 ? o.xMax - w : o.x;
                node.Y = dy < 0 ? o.yMax - h : o.y;
                ApplyLiveGeometry();
                return;
            }
            // Multi-selection: scale the whole bounding box from the opposite corner, clamped
            // so no node drops below the minimum size. / 多选：自对角整体缩放包围盒，钳制保证
            // 任何节点不低于最小尺寸。
            float sx = 1f, sy = 1f;
            if (dx != 0)
            {
                float nw = dx > 0 ? fmResizeBBox.width + (canvasPos.x - fmPressCanvas.x) : fmResizeBBox.width - (canvasPos.x - fmPressCanvas.x);
                sx = Mathf.Max(0.05f, nw / Mathf.Max(1f, fmResizeBBox.width));
                foreach (var kv in fmResizeOrig)
                    sx = Mathf.Max(sx, minSize / Mathf.Max(1f, kv.Value.width));
            }
            if (dy != 0)
            {
                float nh = dy > 0 ? fmResizeBBox.height + (canvasPos.y - fmPressCanvas.y) : fmResizeBBox.height - (canvasPos.y - fmPressCanvas.y);
                sy = Mathf.Max(0.05f, nh / Mathf.Max(1f, fmResizeBBox.height));
                foreach (var kv in fmResizeOrig)
                    sy = Mathf.Max(sy, minSize / Mathf.Max(1f, kv.Value.height));
            }
            float anchorX = dx < 0 ? fmResizeBBox.xMax : fmResizeBBox.x;
            float anchorY = dy < 0 ? fmResizeBBox.yMax : fmResizeBBox.y;
            foreach (var kv in fmResizeOrig)
            {
                kv.Key.Width = kv.Value.width * sx;
                kv.Key.Height = kv.Value.height * sy;
                kv.Key.X = anchorX + (kv.Value.x - anchorX) * sx;
                kv.Key.Y = anchorY + (kv.Value.y - anchorY) * sy;
            }
            ApplyLiveGeometry();
        }

        private void EndEditorResize()
        {
            fmResizeOrig.Clear();
            if (fmResizeMoved)
            {
                SaveSettingsFromGui();
                RequestEditorRebuild();
            }
        }

        /// <summary>Size snapping while resizing: grid rounding (5px) first, then a sibling-size
        /// match within ±4px — resizing a key to another key's exact width takes one gesture. /
        /// 缩放时的尺寸吸附：先按 5px 网格取整，再在 ±4px 内匹配兄弟节点尺寸——把一个键缩放
        /// 到与另一个键完全同宽只需一次手势。</summary>
        private static float EditorSnapSize(float value, List<float> siblingSizes)
        {
            const float grid = 5f;
            const float sizeMatch = 4f;
            float snapped = Mathf.Round(value / grid) * grid;
            if (siblingSizes != null)
            {
                float best = float.MaxValue;
                float at = snapped;
                foreach (float s in siblingSizes)
                {
                    float d = s - value;
                    if (Mathf.Abs(d) < Mathf.Abs(best))
                    {
                        best = d;
                        at = s;
                    }
                }
                if (Mathf.Abs(best) <= sizeMatch) snapped = at;
            }
            return Mathf.Max(10f, snapped);
        }

        private void DrawEditorResizeHandles(Rect rect)
        {
            if (editorSelection.Count == 0 || fmGesture == FmGesture.DragNodes || fmGesture == FmGesture.Marquee) return;
            Rect bbox = EditorSelectionBounds();
            if (bbox.width <= 0f && bbox.height <= 0f) return;
            var dirs = EditorActiveHandleDirs();
            for (int i = 0; i < dirs.Length; i++)
            {
                Vector2 sp = EditorScreenOf(rect, FmHandlePoint(bbox, dirs[i].dx, dirs[i].dy));
                sp.x = Mathf.Clamp(sp.x, rect.x + 4f, rect.xMax - 4f);
                sp.y = Mathf.Clamp(sp.y, rect.y + 4f, rect.yMax - 4f);
                GUIUtils.DrawRect(new Rect(sp.x - 6f, sp.y - 6f, 12f, 12f), Color.white);
                GUIUtils.DrawRect(new Rect(sp.x - 4f, sp.y - 4f, 8f, 8f), new Color(0.25f, 0.55f, 1f, 0.95f));
            }
        }

        // ======================== minimap / 小地图 ========================
        // Small bottom-right corner box. Its position is clamped INSIDE the canvas
        // rect, and the canvas rect is now guaranteed to fit the window (content minimums below
        // the window minimum), so the box can never escape. / 右下角小框。位置钳制在
        // 画布矩形内部，而画布矩形现在保证不超出窗口（内容最小值低于窗口最小值），因此小框
        // 永远不会出界。支持拖动白色视口框平移视图。

        private bool fmMinimapDrag;
        private bool fmMinimapMoved;

        private Rect FmMinimapRect(Rect canvasRect)
        {
            float regionW = CanvasWidth + 200f;
            float regionH = 1080f + 200f;
            float boxW = FmMinimapWidth;
            float boxH = boxW * regionH / regionW;
            float x = Mathf.Clamp(canvasRect.xMax - boxW - 8f, canvasRect.x + 4f, canvasRect.xMax - boxW - 4f);
            float y = Mathf.Clamp(canvasRect.yMax - boxH - 8f, canvasRect.y + 4f, canvasRect.yMax - boxH - 4f);
            return new Rect(x, y, boxW, boxH);
        }

        private bool HandleEditorMinimapStart(Rect canvasRect, Event e)
        {
            Rect box = FmMinimapRect(canvasRect);
            if (e.type != EventType.MouseDown || e.button != 0 || !box.Contains(e.mousePosition)) return false;
            // Only the white viewport rect itself starts a drag; clicks elsewhere in the box are
            // consumed (so canvas gestures don't bleed through) but move nothing. /
            // 只有白色视口框本身可以发起拖动；框内白框外的点击被吞掉（避免误触发画布手势），
            // 但不移动视图。
            e.Use();
            if (!FmMinimapViewportBox(canvasRect, box).Contains(e.mousePosition)) return true;
            fmMinimapDrag = true;
            fmMinimapMoved = false;
            return true;
        }

        /// <summary>The white viewport rectangle mapped into minimap box space. /
        /// 映射到小地图框空间的白色视口矩形。</summary>
        private Rect FmMinimapViewportBox(Rect canvasRect, Rect box)
        {
            float scale = Mathf.Min(box.width / (CanvasWidth + 200f), box.height / (1080f + 200f));
            Vector2 regionCenter = new Vector2(CanvasWidth * 0.5f, 540f);
            Vector2 boxCenter = box.center;
            Vector2 vpMin = EditorCanvasOf(canvasRect, canvasRect.min);
            Vector2 vpMax = EditorCanvasOf(canvasRect, canvasRect.max);
            Vector2 a = boxCenter + (new Vector2(Mathf.Min(vpMin.x, vpMax.x), Mathf.Min(vpMin.y, vpMax.y)) - regionCenter) * scale;
            return new Rect(a.x, a.y, Mathf.Abs(vpMax.x - vpMin.x) * scale, Mathf.Abs(vpMax.y - vpMin.y) * scale);
        }

        private void HandleEditorMinimapDrag(Rect canvasRect, Event e)
        {
            Rect box = FmMinimapRect(canvasRect);
            float scale = Mathf.Min(box.width / (CanvasWidth + 200f), box.height / (1080f + 200f));
            if (e.type == EventType.MouseDrag)
            {
                // Drag the white viewport box: box-space mouse delta converts to canvas-space
                // view-center movement. / 拖动白色视口框：框内鼠标增量换算为画布空间的视口中
                // 心移动。
                Vector2 canvasDelta = new Vector2(e.delta.x, e.delta.y) / Mathf.Max(0.0001f, scale);
                fmScroll -= canvasDelta * fmZoom;
                fmMinimapMoved = true;
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                if (!fmMinimapMoved)
                {
                    // Plain click centers the view. / 单击居中视图。
                    Vector2 target = (e.mousePosition - box.center) / scale + new Vector2(CanvasWidth * 0.5f, 540f);
                    fmScroll = -target * fmZoom;
                }
                fmMinimapDrag = false;
                fmMinimapMoved = false;
            }
        }

        private void DrawEditorMinimap(Rect canvasRect)
        {
            Rect box = FmMinimapRect(canvasRect);
            float scale = Mathf.Min(box.width / (CanvasWidth + 200f), box.height / (1080f + 200f));
            // Canvas (0,0) maps to the box's top-left area, NOT its center — the mapped region
            // is [−100, CanvasWidth+100] × [−100, 1180] centered on (CanvasWidth/2, 540);
            // forgetting the region-center subtraction shoves everything into the bottom-right
            // quadrant of the box. / 画布 (0,0) 映射到框的左上区域而非中心——映射区域以
            //(CanvasWidth/2, 540) 为中心；漏掉区域中心减法会把所有内容挤进框的右下象限。
            Vector2 regionCenter = new Vector2(CanvasWidth * 0.5f, 540f);
            Vector2 boxCenter = box.center;
            Rect MapRect(Rect r)
            {
                Vector2 a = boxCenter + (new Vector2(r.x, r.y) - regionCenter) * scale;
                return new Rect(a.x, a.y, r.width * scale, r.height * scale);
            }
            DrawRectOutline(MapRect(new Rect(0f, 0f, CanvasWidth, 1080f)), new Color(1f, 0.9f, 0.35f, 0.7f), 1f);
            foreach (KvNode node in EditorDrawOrder())
            {
                if (node.Hidden) continue;
                Color c = editorSelection.Contains(node)
                    ? new Color(1f, 0.82f, 0.15f, 0.95f)
                    : node.NodeType == 3 ? new Color(0.6f, 0.7f, 1f, 0.8f) : new Color(0.75f, 0.75f, 0.78f, 0.8f);
                Rect r = MapRect(new Rect(node.X, node.Y, node.Width, node.Height));
                GUIUtils.DrawRect(new Rect(r.x, r.y, Mathf.Max(1.5f, r.width), Mathf.Max(1.5f, r.height)), c);
            }
            DrawRectOutline(FmMinimapViewportBox(canvasRect, box), new Color(1f, 1f, 1f, 0.85f), 1f);
        }

        // ---- layer groups UI / 图层组 UI ----

        private void DrawEditorGroupManager()
        {
            fmGroupsExpanded = GUILayout.Toggle(fmGroupsExpanded, I18n.Tr("fm_groups"), GUILayout.Height(20f));
            if (!fmGroupsExpanded) return;
            List<KvLayerGroup> groups = Settings.Data.LayerGroups;
            for (int i = 0; i < groups.Count; i++)
            {
                KvLayerGroup g = groups[i];
                GUILayout.BeginHorizontal();
                string name = TextInputField("fme_g_" + g.Id, g.Name ?? "", GUILayout.MinWidth(80f));
                if (!string.Equals(name, g.Name ?? "", StringComparison.Ordinal))
                {
                    g.Name = name;
                    SaveSettingsFromGui();
                }
                bool vis = GUILayout.Toggle(g.Visible, GUIContent.none, GUILayout.Width(30f));
                if (vis != g.Visible)
                {
                    g.Visible = vis;
                    EditorMutated();
                }
                GUI.enabled = editorSelection.Count > 0;
                if (GUILayout.Button(I18n.Tr("fm_group_assign"), GUILayout.Width(64f)))
                {
                    PushEditorHistory();
                    foreach (KvNode n in editorSelection) n.GroupId = g.Id;
                    EditorMutated();
                }
                GUI.enabled = true;
                if (GUILayout.Button(I18n.Tr("fm_group_del"), GUILayout.Width(64f)))
                {
                    PushEditorHistory();
                    string deadId = g.Id;
                    groups.RemoveAt(i);
                    foreach (KvNode n in Settings.Data.CustomNodes)
                        if (n != null && n.GroupId == deadId) n.GroupId = "";
                    EditorMutated();
                    break;
                }
                GUILayout.EndHorizontal();
            }
            if (GUILayout.Button(I18n.Tr("fm_group_add"), GUILayout.Width(140f)))
            {
                PushEditorHistory();
                int n = Settings.Data.LayerGroupNextId++;
                groups.Add(new KvLayerGroup { Id = "g" + n, Name = I18n.Tr("fm_group") + " " + n, Visible = true });
                SaveSettingsFromGui();
            }
        }

        private void PushEditorHistorySnapshot(string snapshot)
        {
            if (snapshot == null) return;
            try
            {
                editorHistory.Push(snapshot);
            }
            catch (Exception e)
            {
                Loader.Warning($"KeyViewer: editor snapshot failed: {e.Message}");
            }
        }

        /// <summary>Incremental drag with a snap correction on top: the accumulated delta is the
        /// raw mouse motion, snapping adds a one-shot correction — the mouse never fights the
        /// snap. / 累计增量式拖拽叠加吸附修正：累计量是原始鼠标运动，吸附加一次性修正——
        /// 鼠标永远不会与吸附打架。</summary>
        private void ApplyEditorDrag(float dx, float dy, bool noSnap)
        {
            if (fmDragStart.Count == 0) return;
            float corrX = 0f, corrY = 0f;
            fmAlignLines.Clear();
            if (!noSnap) EditorSnapDrag(dx, dy, out corrX, out corrY);
            float fx = dx + corrX;
            float fy = dy + corrY;
            foreach (KeyValuePair<KvNode, Vector2> kv in fmDragStart)
            {
                kv.Key.X = kv.Value.x + fx;
                kv.Key.Y = kv.Value.y + fy;
            }
            ApplyLiveGeometry();
        }

        /// <summary>Alignment snap against other nodes' L/C/R + T/C/B and the three screen
        /// lines (left edge / center / right edge of the real screen). Threshold is
        /// screen-constant (5px / zoom). Guide lines are emitted only for edges that are
        /// actually aligned after the correction. / 对其它节点的左/中/右、上/中/下以及真实
        /// 屏幕三条线（左缘/中心/右缘）做对齐吸附。阈值屏幕恒定（5px/zoom）。对齐线只在
        /// 修正后确实对齐的边上输出。</summary>
        private void EditorSnapDrag(float dx, float dy, out float corrX, out float corrY)
        {
            corrX = 0f;
            corrY = 0f;
            float snapLimit = 5f / Mathf.Max(0.05f, fmZoom);
            List<KvNode> refs = new List<KvNode>();
            foreach (KvNode node in Settings.Data.CustomNodes)
                if (node != null && !fmDragStart.ContainsKey(node)) refs.Add(node);

            float bestX = float.MaxValue, bestY = float.MaxValue;
            float atX = 0f, atY = 0f;
            KvNode refXNode = null, refYNode = null;

            foreach (KvNode node in fmDragStart.Keys)
            {
                Vector2 start = fmDragStart[node];
                float w = node.Width, h = node.Height;
                float[] edgesX = { start.x + dx, start.x + dx + w * 0.5f, start.x + dx + w };
                float[] edgesY = { start.y + dy, start.y + dy + h * 0.5f, start.y + dy + h };

                void TryX(float candidate, KvNode owner)
                {
                    foreach (float edge in edgesX)
                    {
                        float diff = candidate - edge;
                        if (Math.Abs(diff) <= snapLimit && Math.Abs(diff) < Math.Abs(bestX))
                        {
                            bestX = diff;
                            atX = candidate;
                            refXNode = owner;
                        }
                    }
                }

                void TryY(float candidate, KvNode owner)
                {
                    foreach (float edge in edgesY)
                    {
                        float diff = candidate - edge;
                        if (Math.Abs(diff) <= snapLimit && Math.Abs(diff) < Math.Abs(bestY))
                        {
                            bestY = diff;
                            atY = candidate;
                            refYNode = owner;
                        }
                    }
                }

                TryX(0f, null);
                TryX(CanvasWidth * 0.5f, null);
                TryX(CanvasWidth, null);
                TryY(0f, null);
                TryY(540f, null);
                TryY(1080f, null);
                foreach (KvNode r in refs)
                {
                    float rw = r.Width, rh = r.Height;
                    TryX(r.X, r);
                    TryX(r.X + rw * 0.5f, r);
                    TryX(r.X + rw, r);
                    TryY(r.Y, r);
                    TryY(r.Y + rh * 0.5f, r);
                    TryY(r.Y + rh, r);
                }
            }

            if (Math.Abs(bestX) <= snapLimit)
            {
                corrX = bestX;
                float fx = dx + corrX, fy = dy;
                EmitAlignLine(true, atX, refXNode, fx, fy);
            }
            if (Math.Abs(bestY) <= snapLimit)
            {
                corrY = bestY;
                float fx = dx, fy = dy + corrY;
                EmitAlignLine(false, atY, refYNode, fx, fy);
            }
        }

        /// <summary>A guide line is drawn only when, after applying the correction, a selected
        /// edge really sits on the reference coordinate; its extent covers both the selected
        /// nodes and the reference node (±10px for screen lines). / 只有应用修正后选中边确实
        /// 落在参考坐标上才画线；线段范围覆盖选中节点与参考节点（屏幕线各延伸 10px）。</summary>
        private void EmitAlignLine(bool vertical, float coord, KvNode refNode, float fx, float fy)
        {
            float min = float.MaxValue, max = float.MinValue;
            bool aligned = false;
            foreach (KeyValuePair<KvNode, Vector2> kv in fmDragStart)
            {
                KvNode node = kv.Key;
                float w = node.Width, h = node.Height;
                float x = kv.Value.x + fx, y = kv.Value.y + fy;
                float[] edges = vertical
                    ? new[] { x, x + w * 0.5f, x + w }
                    : new[] { y, y + h * 0.5f, y + h };
                foreach (float edge in edges)
                {
                    if (Math.Abs(edge - coord) < 0.01f)
                    {
                        aligned = true;
                        float lo = vertical ? y : x;
                        float hi = vertical ? y + h : x + w;
                        if (lo < min) min = lo;
                        if (hi > max) max = hi;
                    }
                }
            }
            if (!aligned) return;
            if (refNode != null)
            {
                float lo = vertical ? refNode.Y : refNode.X;
                float hi = vertical ? refNode.Y + refNode.Height : refNode.X + refNode.Width;
                if (lo < min) min = lo;
                if (hi > max) max = hi;
            }
            else
            {
                min -= 10f;
                max += 10f;
            }
            fmAlignLines.Add(new FmAlignLine { Vertical = vertical, Coord = coord, Min = min, Max = max });
        }

        // ---- picking / 拣选 ----

        private List<KvNode> EditorDrawOrder()
        {
            fmOrderBuffer.Clear();
            foreach (KvNode node in Settings.Data.CustomNodes)
                if (node != null && node.NodeType == 3) fmOrderBuffer.Add(node);
            foreach (KvNode node in Settings.Data.CustomNodes)
                if (node != null && node.NodeType != 3) fmOrderBuffer.Add(node);
            // Stable sort: images bucket first, then Depth ascending within each bucket. /
            // 稳定排序：图片桶在前，桶内 Depth 升序。
            for (int i = 1; i < fmOrderBuffer.Count; i++)
            {
                KvNode cur = fmOrderBuffer[i];
                int curKey = cur.NodeType == 3 ? 0 : 1;
                int j = i - 1;
                while (j >= 0)
                {
                    KvNode prev = fmOrderBuffer[j];
                    int prevKey = prev.NodeType == 3 ? 0 : 1;
                    if (prevKey < curKey || (prevKey == curKey && prev.Depth <= cur.Depth)) break;
                    fmOrderBuffer[j + 1] = prev;
                    j--;
                }
                fmOrderBuffer[j + 1] = cur;
            }
            return fmOrderBuffer;
        }

        private List<KvNode> HitTestEditorNodes(Vector2 canvasPos, bool includeLocked)
        {
            // Topmost first = reverse draw order. / 最上层优先 = 绘制顺序的逆序。
            List<KvNode> order = EditorDrawOrder();
            List<KvNode> hits = new List<KvNode>();
            for (int i = order.Count - 1; i >= 0; i--)
            {
                KvNode node = order[i];
                if (node.Unselectable && !includeLocked) continue;
                if (canvasPos.x >= node.X && canvasPos.x <= node.X + node.Width
                    && canvasPos.y >= node.Y && canvasPos.y <= node.Y + node.Height)
                    hits.Add(node);
            }
            return hits;
        }

        private KvNode PickEditorNode(List<KvNode> hits, bool doubleClick, bool ctrl)
        {
            if (hits == null || hits.Count == 0) return null;
            if (doubleClick && hits.Count > 1)
            {
                // Cycle through the overlap stack: pick the hit AFTER the currently selected
                // one, wrapping around. / 在重叠栈中循环：选当前选中项之后的那个，环形回绕。
                KvNode sel = hits.FirstOrDefault(h => editorSelection.Contains(h));
                int idx = sel != null ? hits.IndexOf(sel) : -1;
                return hits[(idx + 1 + hits.Count) % hits.Count];
            }
            if (!ctrl)
            {
                KvNode sel = hits.FirstOrDefault(h => editorSelection.Contains(h));
                if (sel != null) return sel;
            }
            return hits[0];
        }

        private void EditorApplyCanvasSelection(KvNode target, bool doubleClick, bool ctrl)
        {
            // Clicking empty canvas deselects (unless Ctrl is held) — the old early-return made
            // it impossible to clear the selection by clicking away. /
            // 点击空白处取消选中（按住 Ctrl 除外）——旧的提前返回导致无法通过点空白取消选中。
            if (target == null)
            {
                if (!ctrl && !doubleClick && editorSelection.Count > 0) editorSelection.Clear();
                fmActiveNode = null;
                return;
            }
            if (doubleClick)
            {
                editorSelection.Clear();
                editorSelection.Add(target);
                fmActiveNode = target;
                return;
            }
            if (ctrl)
            {
                if (!editorSelection.Remove(target))
                {
                    editorSelection.Add(target);
                    fmActiveNode = target; // added → becomes the active node / 新加入 → 成为活动节点
                }
                else if (fmActiveNode == target) fmActiveNode = null; // toggled off / 切换移除
                return;
            }
            if (!editorSelection.Contains(target))
            {
                editorSelection.Clear();
                editorSelection.Add(target);
            }
            fmActiveNode = target;
        }

        private bool ConsumeEditorDoubleClick(Vector2 mousePos)
        {
            float now = Time.unscaledTime;
            Vector2 d = mousePos - fmLastClickPos;
            bool doubleClick = now - fmLastClickTime <= FmDoubleClickTime && d.sqrMagnitude <= FmDoubleClickDist * FmDoubleClickDist;
            fmLastClickTime = now;
            fmLastClickPos = mousePos;
            return doubleClick;
        }

        // ---- canvas rendering / 画布绘制 ----

        private void DrawEditorCanvas(Rect rect)
        {
            GUIUtils.DrawRect(rect, new Color(0.07f, 0.07f, 0.09f, 1f));
            Vector2 origin = EditorOrigin(rect);
            // Grid / 网格
            float step = 50f * fmZoom;
            if (step >= 6f)
            {
                float startX = (origin.x - rect.x) % step;
                if (startX < 0) startX += step;
                for (float x = startX; x < rect.width; x += step)
                    GUIUtils.DrawRect(new Rect(rect.x + x, rect.y, 1f, rect.height), new Color(1f, 1f, 1f, 0.045f));
                float startY = (origin.y - rect.y) % step;
                if (startY < 0) startY += step;
                for (float y = startY; y < rect.height; y += step)
                    GUIUtils.DrawRect(new Rect(rect.x, rect.y + y, rect.width, 1f), new Color(1f, 1f, 1f, 0.045f));
            }
            // Game screen bounds / 游戏屏幕范围
            Rect bounds = new Rect(origin.x, origin.y, CanvasWidth * fmZoom, 1080f * fmZoom);
            Rect clipped = ClipRect(bounds, rect);
            if (clipped.width > 0f && clipped.height > 0f)
            {
                DrawRectOutline(clipped, new Color(1f, 0.9f, 0.35f, 0.75f), 1f);
                if (fmHintStyle == null)
                    fmHintStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = new Color(1f, 0.9f, 0.35f, 0.85f) } };
                GUI.Label(new Rect(bounds.x + 5f, bounds.y + 3f, 240f, 18f),
                    string.Format(I18n.Tr("fm_screen_bounds"), CanvasWidth.ToString("F0")), fmHintStyle);
            }
            // Nodes / 节点
            foreach (KvNode node in EditorDrawOrder())
            {
                Rect sr = new Rect(origin.x + node.X * fmZoom, origin.y + node.Y * fmZoom,
                    node.Width * fmZoom, node.Height * fmZoom);
                if (!sr.Overlaps(rect)) continue;
                DrawEditorNode(node, sr, rect);
            }
            // Align lines / 对齐线
            foreach (FmAlignLine line in fmAlignLines)
            {
                Color col = new Color(1f, 0.85f, 0.2f, 0.95f);
                if (line.Vertical)
                {
                    float x = origin.x + line.Coord * fmZoom;
                    float y1 = Mathf.Max(rect.y, origin.y + line.Min * fmZoom);
                    float y2 = Mathf.Min(rect.yMax, origin.y + line.Max * fmZoom);
                    if (y2 > y1) GUIUtils.DrawRect(new Rect(x - 0.75f, y1, 1.5f, y2 - y1), col);
                }
                else
                {
                    float y = origin.y + line.Coord * fmZoom;
                    float x1 = Mathf.Max(rect.x, origin.x + line.Min * fmZoom);
                    float x2 = Mathf.Min(rect.xMax, origin.x + line.Max * fmZoom);
                    if (x2 > x1) GUIUtils.DrawRect(new Rect(x1, y - 0.75f, x2 - x1, 1.5f), col);
                }
            }
            // Marquee / 框选
            if (fmGesture == FmGesture.Marquee)
            {
                Rect band = EditorRectFromCorners(fmMarqueeStart, fmMarqueeCur);
                Rect bandScreen = new Rect(origin.x + band.x * fmZoom, origin.y + band.y * fmZoom,
                    band.width * fmZoom, band.height * fmZoom);
                GUIUtils.DrawRect(ClipRect(bandScreen, rect), new Color(1f, 0.92f, 0.4f, 0.12f));
                DrawRectOutline(ClipRect(bandScreen, rect), new Color(1f, 0.92f, 0.4f, 0.9f), 1f);
            }
            // Resize handles / 缩放手柄
            DrawEditorResizeHandles(rect);
        }

        private static Rect ClipRect(Rect r, Rect clip)
        {
            float xMin = Mathf.Max(r.x, clip.x);
            float yMin = Mathf.Max(r.y, clip.y);
            float xMax = Mathf.Min(r.xMax, clip.xMax);
            float yMax = Mathf.Min(r.yMax, clip.yMax);
            if (xMax <= xMin || yMax <= yMin) return new Rect(0, 0, 0, 0);
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private void DrawRectOutline(Rect r, Color color, float t)
        {
            if (r.width <= 0f || r.height <= 0f) return;
            t = Mathf.Min(t, r.height * 0.5f, r.width * 0.5f);
            GUIUtils.DrawRect(new Rect(r.x, r.y, r.width, t), color);
            GUIUtils.DrawRect(new Rect(r.x, r.yMax - t, r.width, t), color);
            GUIUtils.DrawRect(new Rect(r.x, r.y + t, t, Mathf.Max(0f, r.height - 2f * t)), color);
            GUIUtils.DrawRect(new Rect(r.xMax - t, r.y + t, t, Mathf.Max(0f, r.height - 2f * t)), color);
        }

        private void DrawEditorNode(KvNode node, Rect sr, Rect canvasRect)
        {
            bool selected = editorSelection.Contains(node);
            float dim = node.Hidden ? 0.25f : node.Unselectable && !selected ? 0.45f : 1f;
            if (node.NodeType == 3)
            {
                Texture2D tex = EditorNodeTexture(node);
                if (tex != null)
                {
                    Color prev = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(node.Opacity) * dim);
                    GUI.DrawTexture(ClipRect(sr, canvasRect), tex, ScaleMode.StretchToFill);
                    GUI.color = prev;
                }
                else
                {
                    GUIUtils.DrawRect(ClipRect(sr, canvasRect), new Color(0.3f, 0.3f, 0.34f, 0.9f * dim));
                }
            }
            else
            {
                ProfileData d = Settings.Data;
                Color bg = node.UseCustomColor ? NodeColor(node.Bg, d.Background) : d.Background;
                Color ol = node.UseCustomColor ? NodeColor(node.Outline, d.Outline) : d.Outline;
                GUIUtils.DrawRect(ClipRect(sr, canvasRect), WithAlpha(bg, dim));
                DrawRectOutline(ClipRect(sr, canvasRect), WithAlpha(ol, dim), 1.5f);
                string label = node.NodeType == 1
                    ? (string.IsNullOrEmpty(node.CustomText) ? "KPS" : node.CustomText)
                    : node.NodeType == 2
                        ? (string.IsNullOrEmpty(node.CustomText) ? "Total" : node.CustomText)
                        : !string.IsNullOrEmpty(node.CustomText)
                            ? node.CustomText
                            : KeyToString(CustomNodeKeyCode(node));
                if (fmNodeLabelStyle == null)
                    fmNodeLabelStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        clipping = TextClipping.Overflow,
                        fontStyle = FontStyle.Bold,
                    };
                fmNodeLabelStyle.fontSize = Mathf.Max(7, Mathf.RoundToInt(18f * fmZoom));
                fmNodeLabelStyle.normal.textColor = WithAlpha(d.Text, dim);
                GUI.Label(ClipRect(sr, canvasRect), label, fmNodeLabelStyle);
            }
            if (selected)
            {
                // Multi-select: the ACTIVE node (whose values the property panel shows) gets a
                // distinct cyan, thicker frame; the rest stay yellow. / 多选时活动节点（属性
                // 面板显示其值的那个）用醒目的青色加粗框，其余保持黄色。
                bool active = editorSelection.Count > 1 && fmActiveNode == node;
                DrawRectOutline(new Rect(sr.x - 2f, sr.y - 2f, sr.width + 4f, sr.height + 4f),
                    active ? new Color(0.25f, 0.95f, 1f, 1f) : new Color(1f, 0.82f, 0.15f, 0.95f),
                    active ? 3.5f : 2f);
            }
        }

        /// <summary>Human-readable node name for the panel: key name / custom text / KPS /
        /// Total / image file name — mirrors the canvas label. / 面板用的节点人话名称：键名/
        /// 自定义文本/KPS/Total/图片文件名——与画布标签一致。</summary>
        private static string EditorNodeDisplayName(KvNode node)
        {
            if (node.NodeType == 1) return string.IsNullOrEmpty(node.CustomText) ? "KPS" : node.CustomText;
            if (node.NodeType == 2) return string.IsNullOrEmpty(node.CustomText) ? "Total" : node.CustomText;
            if (node.NodeType == 3)
                return string.IsNullOrWhiteSpace(node.ImagePath) ? I18n.Tr("fm_add_image") : System.IO.Path.GetFileName(node.ImagePath);
            if (!string.IsNullOrEmpty(node.CustomText)) return node.CustomText;
            KeyCode kc = CustomNodeKeyCode(node);
            return kc != KeyCode.None ? KeyToString(kc) : "?";
        }

        private static Color WithAlpha(Color c, float mul)
        {
            c.a *= mul;
            return c;
        }

        private Texture2D EditorNodeTexture(KvNode node)
        {
            string path = ResolveCustomImagePath(node.ImagePath);
            if (path == null) return null;
            if (fmTexCache.TryGetValue(path, out Texture2D cached) && cached != null) return cached;
            Texture2D tex = KvImageLoader.LoadTexture(path);
            fmTexCache[path] = tex;
            return tex;
        }

        // ---- resize corner / 缩放角 ----

        private void HandleEditorResize(Event e)
        {
            Rect corner = new Rect(editorRect.xMax - 20f, editorRect.yMax - 20f, 20f, 20f);
            if (e.type == EventType.Repaint)
                GUIUtils.DrawRect(new Rect(editorRect.xMax - 12f, editorRect.yMax - 4f, 8f, 2f), new Color(1f, 1f, 1f, 0.35f));
            if (e.type == EventType.MouseDown && e.button == 0 && corner.Contains(e.mousePosition))
            {
                fmResizing = true;
                e.Use();
            }
            if (fmResizing && e.type == EventType.MouseDrag)
            {
                editorRect.width = Mathf.Clamp(e.mousePosition.x - editorRect.x + 12f, FmMinWindowWidth,
                    Mathf.Max(FmMinWindowWidth, Screen.width - editorRect.x));
                editorRect.height = Mathf.Clamp(e.mousePosition.y - editorRect.y + 12f, FmMinWindowHeight,
                    Mathf.Max(FmMinWindowHeight, Screen.height - editorRect.y));
                e.Use();
            }
            if (fmResizing && e.type == EventType.MouseUp)
            {
                fmResizing = false;
                e.Use();
            }
        }

        // ---- keyboard shortcuts / 快捷键 ----

        private void HandleEditorShortcuts(Event e)
        {
            bool typing = GUI.GetNameOfFocusedControl()?.StartsWith("fme_", StringComparison.Ordinal) == true;
            if (typing) return;
            if (fmCaptureNode != null) return;
            if (fmCaptureGhostNode != null) return;
            bool ctrl = e.control;
            switch (e.keyCode)
            {
                case KeyCode.Delete:
                case KeyCode.Backspace:
                    EditorDeleteSelection();
                    e.Use();
                    return;
                case KeyCode.Escape:
                    editorSelection.Clear();
                    e.Use();
                    return;
                case KeyCode.Z when ctrl:
                    if (e.shift) EditorRedo();
                    else EditorUndo();
                    e.Use();
                    return;
                case KeyCode.Y when ctrl:
                    EditorRedo();
                    e.Use();
                    return;
                case KeyCode.C when ctrl:
                    EditorCopySelection();
                    e.Use();
                    return;
                case KeyCode.V when ctrl:
                    EditorPaste();
                    e.Use();
                    return;
                case KeyCode.A when ctrl:
                    EditorSelectAll();
                    e.Use();
                    return;
            }
            // Arrow-key nudge (IMGUI repeats KeyDown while held — the OS repeat does the job). /
            // 方向键微调（按住时 IMGUI 重复派发 KeyDown——系统重复即够用）。
            float dx = 0f, dy = 0f;
            switch (e.keyCode)
            {
                case KeyCode.LeftArrow: dx = -1f; break;
                case KeyCode.RightArrow: dx = 1f; break;
                case KeyCode.UpArrow: dy = -1f; break;
                case KeyCode.DownArrow: dy = 1f; break;
                default: return;
            }
            if (editorSelection.Count == 0) return;
            try
            {
                editorHistory.PushNudge(SnapshotCustomNodes(), Time.unscaledTime);
            }
            catch (Exception ex)
            {
                Loader.Warning($"KeyViewer: editor snapshot failed: {ex.Message}");
            }
            foreach (KvNode node in editorSelection)
            {
                node.X += dx;
                node.Y += dy;
            }
            e.Use();
            SaveSettingsFromGui();
            RequestEditorRebuild();
        }

        // ---- property panel / 属性面板 ----

        private void DrawEditorProperties()
        {
            if (editorSelection.Count == 0)
            {
                GUILayout.Label(I18n.Tr("fm_no_selection_hint"));
                DrawEditorGroupManager();
                return;
            }
            // Panel shows the ACTIVE node's values — the last clicked one; marquee/select-all
            // (no click order) fall back to the list head. / 面板显示活动节点的值——最后点击
            // 的那个；框选/全选（无点击顺序）回落到列表首项。
            if (fmActiveNode == null || !editorSelection.Contains(fmActiveNode)) fmActiveNode = editorSelection[0];
            KvNode first = fmActiveNode;
            bool single = editorSelection.Count == 1;

            GUILayout.Label(string.Format(I18n.Tr("fm_selected_count"), editorSelection.Count));
            if (!single)
                GUILayout.Label("<i>" + string.Format(I18n.Tr("fm_shown_node"), EditorNodeDisplayName(first)) + "</i>");

            if (single)
            {
                DrawEditorNodeTypeCombo(first);
                DrawEditorKeyBindCapture(first);
                if (first.NodeType == 0 || first.NodeType == 3)
                    DrawEditorGhostBindCapture(first);
            }

            DrawEditorFloatField(I18n.Tr("fm_pos_x"), "fme_x_" + first.Id, n => n.X, v =>
            {
                float delta = v - first.X;
                foreach (KvNode n in editorSelection) n.X += delta;
                EditorPropertyChanged();
            });
            DrawEditorFloatField(I18n.Tr("fm_pos_y"), "fme_y_" + first.Id, n => n.Y, v =>
            {
                float delta = v - first.Y;
                foreach (KvNode n in editorSelection) n.Y += delta;
                EditorPropertyChanged();
            });
            DrawEditorFloatField(I18n.Tr("fm_width"), "fme_w_" + first.Id, n => n.Width, v =>
            {
                foreach (KvNode n in editorSelection) n.Width = Mathf.Max(10f, v);
                EditorPropertyChanged();
            });
            DrawEditorFloatField(I18n.Tr("fm_height"), "fme_h_" + first.Id, n => n.Height, v =>
            {
                foreach (KvNode n in editorSelection) n.Height = Mathf.Max(10f, v);
                EditorPropertyChanged();
            });

            int depth = first.Depth;
            GUILayout.BeginHorizontal();
            GUILayout.Label(I18n.Tr("fm_depth"), GUILayout.Width(96f));
            int newDepth = Mathf.RoundToInt(GUILayout.HorizontalSlider(depth, 0, 60));
            // Mixed depths show "—" (never parses → no accidental mass-apply); a deliberate
            // slider drag or typed number still applies to all. / 深度不一致时显示"—"（永不解析
            // →不会意外群发）；有意拖滑杆或输入数字仍然应用到全部。
            bool depthMixed = false;
            for (int i = 1; i < editorSelection.Count; i++)
                if (editorSelection[i].Depth != first.Depth) { depthMixed = true; break; }
            string depthText = TextInputField("fme_d_" + first.Id, depthMixed ? "—" : newDepth.ToString(), GUILayout.Width(56f));
            if (int.TryParse(depthText, out int parsedDepth)) newDepth = Mathf.Clamp(parsedDepth, 0, 60);
            GUILayout.EndHorizontal();
            if (newDepth != first.Depth && (!depthMixed || int.TryParse(depthText, out _)))
            {
                foreach (KvNode n in editorSelection) n.Depth = newDepth;
                EditorPropertyChanged();
            }

            if (first.NodeType == 0)
            {
                DrawEditorTextField(I18n.Tr("fm_custom_text"), "fme_ct_" + first.Id, first.CustomText, v =>
                {
                    foreach (KvNode n in editorSelection) n.CustomText = v;
                    EditorPropertyChanged();
                });
                DrawEditorTextField(I18n.Tr("fm_pressed_text"), "fme_pt_" + first.Id, first.PressedText, v =>
                {
                    foreach (KvNode n in editorSelection) n.PressedText = v;
                    EditorPropertyChanged();
                });
                DrawEditorToggle(I18n.Tr("fm_count_in_total"), first.CountInTotal, v => { foreach (KvNode n in editorSelection) n.CountInTotal = v; });
                DrawEditorToggle(I18n.Tr("fm_per_key_kps"), first.PerKeyKps, v => { foreach (KvNode n in editorSelection) n.PerKeyKps = v; });
            }
            if (first.NodeType == 3)
            {
                DrawEditorTextField(I18n.Tr("fm_image_path"), "fme_img_" + first.Id, first.ImagePath, v =>
                {
                    foreach (KvNode n in editorSelection) n.ImagePath = v;
                    EditorPropertyChanged();
                });
                DrawEditorTextField(I18n.Tr("fm_pressed_image"), "fme_imgp_" + first.Id, first.ImagePathPressed, v =>
                {
                    foreach (KvNode n in editorSelection) n.ImagePathPressed = v;
                    EditorPropertyChanged();
                });
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(I18n.Tr("fm_import"), GUILayout.Width(90f))) EditorImportImages();
                if (GUILayout.Button(I18n.Tr("fm_open_dir"), GUILayout.Width(90f))) OpenCustomImagesDir();
                GUILayout.EndHorizontal();
                DrawEditorFloatField(I18n.Tr("fm_opacity"), "fme_o_" + first.Id, n => n.Opacity, v =>
                {
                    foreach (KvNode n in editorSelection) n.Opacity = Mathf.Clamp01(v);
                    EditorPropertyChanged();
                });
                if (!single)
                    GUILayout.Label(I18n.Tr("fm_bind_image_hint"));
            }
            // Stat nodes: per-node text layout (the Display tab's centered/stacked/hide-label
            // sunk to the panel node). / 面板节点：节点级文本布局（显示页的居中/堆叠/隐藏标签
            // 下沉到面板节点）。
            if (first.NodeType == 1 || first.NodeType == 2)
            {
                bool useStatLayout = GUILayout.Toggle(first.UseCustomStatLayout, I18n.Tr("fm_stat_layout"));
                if (useStatLayout != first.UseCustomStatLayout)
                {
                    foreach (KvNode n in editorSelection)
                    {
                        if (n.NodeType != 1 && n.NodeType != 2) continue;
                        n.UseCustomStatLayout = useStatLayout;
                        if (useStatLayout)
                        {
                            // Seed from the CURRENT effective global mode — enabling the override
                            // used to snap the panel to the field defaults (flat label+value),
                            // which looked like centered/stacked "disappearing". /
                            // 用当前生效的全局模式做种子——此前开启覆盖会瞬间回到字段默认值
                            //（平铺标签+数值），看起来像居中/堆叠"消失"了。
                            n.StatCentered = KpsTotalCenteredApplies();
                            n.StatStacked = KpsTotalStackedApplies();
                            n.HideLabel = !KpsTotalIsSlim() && Settings.Data.HideKpsTotalLabel;
                        }
                    }
                    EditorPropertyChanged();
                }
                if (first.UseCustomStatLayout)
                {
                    DrawEditorToggle(I18n.Tr("fk_kps_total_centered"), first.StatCentered,
                        v => { foreach (KvNode n in editorSelection) if (n.NodeType == 1 || n.NodeType == 2) n.StatCentered = v; });
                    if (first.StatCentered)
                        DrawEditorToggle(I18n.Tr("kps_total_stacked"), first.StatStacked,
                            v => { foreach (KvNode n in editorSelection) if (n.NodeType == 1 || n.NodeType == 2) n.StatStacked = v; });
                    GUILayout.Label("<i>" + I18n.Tr("fm_stat_hide_label_hint") + "</i>");
                }
            }
            // Rain: keys and image keys (an image needs a binding to have a trigger source). /
            // 雨滴：按键与图片按键（图片需绑定按键才有触发源）。
            bool rainCapable = first.NodeType == 0 || (first.NodeType == 3 && !string.IsNullOrWhiteSpace(first.KeyBind));
            if (rainCapable)
            {
                GUILayout.Label(I18n.Tr("fm_rain_row"));
                GUILayout.BeginHorizontal();
                string[] rainRowNames = { I18n.Tr("rain_row1"), I18n.Tr("rain_row2"), I18n.Tr("rain_row3") };
                int rainRow = GUILayout.SelectionGrid(Mathf.Clamp(first.RainRow, 0, 2), rainRowNames, 3, GUILayout.Height(20f));
                GUILayout.EndHorizontal();
                if (rainRow >= 0 && rainRow <= 2 && rainRow != first.RainRow)
                {
                    foreach (KvNode n in editorSelection) n.RainRow = rainRow;
                    EditorPropertyChanged();
                }
                DrawEditorToggle(I18n.Tr("fm_rain"), first.RainEnabled, v => { foreach (KvNode n in editorSelection) n.RainEnabled = v; });
                DrawEditorFloatField(I18n.Tr("rain_width"), "fme_rw_" + first.Id, n => n.RainWidth, v =>
                {
                    foreach (KvNode n in editorSelection) n.RainWidth = Mathf.Max(0f, v);
                    EditorPropertyChanged();
                });
                DrawEditorFloatField(I18n.Tr("rain_height"), "fme_rh_" + first.Id, n => n.RainHeight, v =>
                {
                    foreach (KvNode n in editorSelection) n.RainHeight = Mathf.Max(0f, v);
                    EditorPropertyChanged();
                });
                DrawEditorFloatField(I18n.Tr("rain_speed"), "fme_rs_" + first.Id, n => n.RainSpeed, v =>
                {
                    foreach (KvNode n in editorSelection) n.RainSpeed = Mathf.Max(0f, v);
                    EditorPropertyChanged();
                });
                bool useRainColor = GUILayout.Toggle(first.UseCustomRainColor, I18n.Tr("fm_use_custom_rain_color"));
                if (useRainColor != first.UseCustomRainColor)
                {
                    foreach (KvNode n in editorSelection) n.UseCustomRainColor = useRainColor;
                    EditorPropertyChanged();
                }
                if (first.UseCustomRainColor)
                {
                    // Two-color rain: separate top/bottom ends. /
                    // 雨滴双色：顶/底两端独立取色。
                    Color rowColor = rainSystem.GetRainColor(CustomRainRowByte(first));
                    DrawEditorColorField(I18n.Tr("fm_rain_color_top"), first.RainColorTop, rowColor, arr => { foreach (KvNode n in editorSelection) n.RainColorTop = arr; });
                    DrawEditorColorField(I18n.Tr("fm_rain_color_bottom"), first.RainColorBottom, rowColor, arr => { foreach (KvNode n in editorSelection) n.RainColorBottom = arr; });
                }
                DrawEditorFloatField(I18n.Tr("fm_rain_offset_x"), "fme_rox_" + first.Id, n => n.RainOffsetX, v =>
                {
                    foreach (KvNode n in editorSelection) n.RainOffsetX = Mathf.Clamp(v, -2000f, 2000f);
                    EditorPropertyChanged();
                });
                DrawEditorFloatField(I18n.Tr("fm_rain_offset_y"), "fme_roy_" + first.Id, n => n.RainOffsetY, v =>
                {
                    foreach (KvNode n in editorSelection) n.RainOffsetY = Mathf.Clamp(v, -2000f, 2000f);
                    EditorPropertyChanged();
                });
                // Per-node shadow/outline overrides (the Rain tab's shadow/outline rows sunk to
                // the node; off → follow the selected row). / 节点级阴影/描边覆盖（雨线页的
                // 阴影/描边设置下沉到节点；关闭 → 跟随所选排）。
                bool useRainShadow = GUILayout.Toggle(first.UseCustomRainShadow, I18n.Tr("fm_rain_shadow_custom"));
                if (useRainShadow != first.UseCustomRainShadow)
                {
                    foreach (KvNode n in editorSelection)
                    {
                        n.UseCustomRainShadow = useRainShadow;
                        // Seed from the node's row so enabling takes over the CURRENT look — the
                        // unseeded state mixed node-default enable/offsets with row colors, which
                        // read as "the shadow ignores the per-node setting". / 从节点所在排做种子，
                        // 开启即接管当前外观——未做种子时节点默认开关/偏移与排颜色混搭，看起来
                        // 就是"阴影无视节点设置"。
                        if (useRainShadow) SeedRainShadowFromRow(n);
                    }
                    EditorPropertyChanged();
                }
                if (first.UseCustomRainShadow)
                {
                    DrawEditorToggle(I18n.Tr("rain_shadow"), first.RainShadowEnabled, v => { foreach (KvNode n in editorSelection) n.RainShadowEnabled = v; });
                    DrawEditorColorField(I18n.Tr("rain_shadow_color"), first.RainShadowColor, new Color(0f, 0f, 0f, 0.35f), arr => { foreach (KvNode n in editorSelection) n.RainShadowColor = arr; });
                    DrawEditorFloatField("X", "fme_rshx_" + first.Id, n => n.RainShadowOffsetX, v =>
                    {
                        foreach (KvNode n in editorSelection) n.RainShadowOffsetX = Mathf.Clamp(v, -50f, 50f);
                        EditorPropertyChanged();
                    });
                    DrawEditorFloatField("Y", "fme_rshy_" + first.Id, n => n.RainShadowOffsetY, v =>
                    {
                        foreach (KvNode n in editorSelection) n.RainShadowOffsetY = Mathf.Clamp(v, -50f, 50f);
                        EditorPropertyChanged();
                    });
                }
                bool useRainOutline = GUILayout.Toggle(first.UseCustomRainOutline, I18n.Tr("fm_rain_outline_custom"));
                if (useRainOutline != first.UseCustomRainOutline)
                {
                    foreach (KvNode n in editorSelection)
                    {
                        n.UseCustomRainOutline = useRainOutline;
                        if (useRainOutline) SeedRainOutlineFromRow(n);
                    }
                    EditorPropertyChanged();
                }
                if (first.UseCustomRainOutline)
                {
                    DrawEditorToggle(I18n.Tr("rain_outline"), first.RainOutlineEnabled, v => { foreach (KvNode n in editorSelection) n.RainOutlineEnabled = v; });
                    DrawEditorColorField(I18n.Tr("rain_outline_color"), first.RainOutlineColor, new Color(1f, 1f, 1f, 0.5f), arr => { foreach (KvNode n in editorSelection) n.RainOutlineColor = arr; });
                    DrawEditorFloatField(I18n.Tr("rain_outline_width"), "fme_row_" + first.Id, n => n.RainOutlineWidth, v =>
                    {
                        foreach (KvNode n in editorSelection) n.RainOutlineWidth = Mathf.Clamp(v, 0f, 50f);
                        EditorPropertyChanged();
                    });
                }
                // Per-node GHOST rain shadow/outline — only meaningful with a ghost key bound. /
                // 节点级鬼雨阴影/描边——仅在绑定了鬼键时有意义。
                if (!string.IsNullOrWhiteSpace(first.GhostKey))
                {
                    bool useGhostShadow = GUILayout.Toggle(first.UseCustomGhostRainShadow, I18n.Tr("fm_ghost_rain_shadow_custom"));
                    if (useGhostShadow != first.UseCustomGhostRainShadow)
                    {
                        foreach (KvNode n in editorSelection)
                        {
                            n.UseCustomGhostRainShadow = useGhostShadow;
                            if (useGhostShadow) SeedGhostRainShadowFromRow(n);
                        }
                        EditorPropertyChanged();
                    }
                    if (first.UseCustomGhostRainShadow)
                    {
                        DrawEditorToggle(I18n.Tr("rain_shadow"), first.GhostRainShadowEnabled, v => { foreach (KvNode n in editorSelection) n.GhostRainShadowEnabled = v; });
                        DrawEditorColorField(I18n.Tr("rain_shadow_color"), first.GhostRainShadowColor, new Color(0f, 0f, 0f, 0.35f), arr => { foreach (KvNode n in editorSelection) n.GhostRainShadowColor = arr; });
                        DrawEditorFloatField("X", "fme_grshx_" + first.Id, n => n.GhostRainShadowOffsetX, v =>
                        {
                            foreach (KvNode n in editorSelection) n.GhostRainShadowOffsetX = Mathf.Clamp(v, -50f, 50f);
                            EditorPropertyChanged();
                        });
                        DrawEditorFloatField("Y", "fme_grshy_" + first.Id, n => n.GhostRainShadowOffsetY, v =>
                        {
                            foreach (KvNode n in editorSelection) n.GhostRainShadowOffsetY = Mathf.Clamp(v, -50f, 50f);
                            EditorPropertyChanged();
                        });
                    }
                    bool useGhostOutline = GUILayout.Toggle(first.UseCustomGhostRainOutline, I18n.Tr("fm_ghost_rain_outline_custom"));
                    if (useGhostOutline != first.UseCustomGhostRainOutline)
                    {
                        foreach (KvNode n in editorSelection)
                        {
                            n.UseCustomGhostRainOutline = useGhostOutline;
                            if (useGhostOutline) SeedGhostRainOutlineFromRow(n);
                        }
                        EditorPropertyChanged();
                    }
                    if (first.UseCustomGhostRainOutline)
                    {
                        DrawEditorToggle(I18n.Tr("rain_outline"), first.GhostRainOutlineEnabled, v => { foreach (KvNode n in editorSelection) n.GhostRainOutlineEnabled = v; });
                        DrawEditorColorField(I18n.Tr("rain_outline_color"), first.GhostRainOutlineColor, new Color(1f, 1f, 1f, 0.5f), arr => { foreach (KvNode n in editorSelection) n.GhostRainOutlineColor = arr; });
                        DrawEditorFloatField(I18n.Tr("rain_outline_width"), "fme_grow_" + first.Id, n => n.GhostRainOutlineWidth, v =>
                        {
                            foreach (KvNode n in editorSelection) n.GhostRainOutlineWidth = Mathf.Clamp(v, 0f, 50f);
                            EditorPropertyChanged();
                        });
                    }
                }
                // Per-node press scale (the Display tab's press animation per key). /
                // 节点级按压缩放（显示页的按压缩放，按按键配置）。
                bool pressAnim = GUILayout.Toggle(first.PressAnimEnabled, I18n.Tr("fm_press_anim"));
                if (pressAnim != first.PressAnimEnabled)
                {
                    foreach (KvNode n in editorSelection) n.PressAnimEnabled = pressAnim;
                    EditorPropertyChanged();
                }
                if (first.PressAnimEnabled)
                {
                    bool customPressScale = GUILayout.Toggle(first.UseCustomPressAnim, I18n.Tr("fm_press_anim_custom"));
                    if (customPressScale != first.UseCustomPressAnim)
                    {
                        foreach (KvNode n in editorSelection) n.UseCustomPressAnim = customPressScale;
                        EditorPropertyChanged();
                    }
                    if (first.UseCustomPressAnim)
                        DrawEditorFloatField(I18n.Tr("fm_press_anim_scale"), "fme_pas_" + first.Id, n => n.PressAnimScale, v =>
                        {
                            foreach (KvNode n in editorSelection) n.PressAnimScale = Mathf.Clamp(v, 0.3f, 2f);
                            EditorPropertyChanged();
                        });
                }
                // Counter bounce . / 计数器弹跳（计数器弹跳动画）。
                bool counterAnim = GUILayout.Toggle(first.CounterAnimEnabled, I18n.Tr("fm_counter_anim"));
                if (counterAnim != first.CounterAnimEnabled)
                {
                    foreach (KvNode n in editorSelection) n.CounterAnimEnabled = counterAnim;
                    EditorPropertyChanged();
                }
                if (first.CounterAnimEnabled)
                {
                    DrawEditorFloatField(I18n.Tr("fm_anim_scale"), "fme_ascale_" + first.Id, n => n.CounterAnimScale, v =>
                    {
                        foreach (KvNode n in editorSelection) n.CounterAnimScale = Mathf.Clamp(v, 1f, 2f);
                        EditorPropertyChanged();
                    });
                    DrawEditorFloatField(I18n.Tr("fm_anim_duration"), "fme_adur_" + first.Id, n => n.CounterAnimDurationMs, v =>
                    {
                        foreach (KvNode n in editorSelection) n.CounterAnimDurationMs = Mathf.Clamp(v, 100f, 5000f);
                        EditorPropertyChanged();
                    });
                }
            }
            DrawEditorToggle(I18n.Tr("fm_unselectable"), first.Unselectable, v => { foreach (KvNode n in editorSelection) n.Unselectable = v; });
            DrawEditorToggle(I18n.Tr("fm_hidden"), first.Hidden, v => { foreach (KvNode n in editorSelection) n.Hidden = v; });
            DrawEditorFontSize(first);
            DrawEditorToggle(I18n.Tr("fm_hide_label"), first.HideLabel, v => { foreach (KvNode n in editorSelection) n.HideLabel = v; });
            DrawEditorToggle(I18n.Tr("fm_hide_count"), first.HideCount, v => { foreach (KvNode n in editorSelection) n.HideCount = v; });

            // Layer group assignment / 图层组指派
            if (!string.IsNullOrEmpty(first.GroupId))
            {
                KvLayerGroup grp = Settings.Data.LayerGroups.FirstOrDefault(g => g != null && g.Id == first.GroupId);
                GUILayout.Label(I18n.Tr("fm_group") + ": " + (grp != null ? grp.Name : first.GroupId));
                if (GUILayout.Button(I18n.Tr("fm_group_ungroup"), GUILayout.Width(140f)))
                {
                    PushEditorHistory();
                    foreach (KvNode n in editorSelection) n.GroupId = "";
                    EditorMutated();
                }
            }

            // Color overrides work for multi-select too: fields show the first node's current
            // values and every change applies to the whole selection. / 配色覆盖对多选同样生效：
            // 字段显示首个节点的当前值，改动应用到整个选区。
            if (first.NodeType != 3)
            {
                bool isKps = first.NodeType == 1;
                bool isTotal = first.NodeType == 2;
                Color fbBg = isKps ? Settings.Data.KpsBackground : isTotal ? Settings.Data.TotalBackground : Settings.Data.Background;
                Color fbOl = isKps ? Settings.Data.KpsOutline : isTotal ? Settings.Data.TotalOutline : Settings.Data.Outline;
                GUILayout.Space(4f);
                bool useCustom = GUILayout.Toggle(first.UseCustomColor, I18n.Tr("fm_custom_colors"));
                if (useCustom != first.UseCustomColor)
                {
                    foreach (KvNode n in editorSelection) n.UseCustomColor = useCustom;
                    EditorPropertyChanged();
                }
                if (first.UseCustomColor)
                {
                    DrawEditorColorField(I18n.Tr("color_bg"), first.Bg, fbBg, arr => { foreach (KvNode n in editorSelection) if (n.NodeType != 3) n.Bg = arr; });
                    DrawEditorColorField(I18n.Tr("color_bg_clicked"), first.BgPressed, fbBg, arr => { foreach (KvNode n in editorSelection) if (n.NodeType != 3) n.BgPressed = arr; });
                    DrawEditorColorField(I18n.Tr("color_outline"), first.Outline, fbOl, arr => { foreach (KvNode n in editorSelection) if (n.NodeType != 3) n.Outline = arr; });
                    DrawEditorColorField(I18n.Tr("color_outline_clicked"), first.OutlinePressed, fbOl, arr => { foreach (KvNode n in editorSelection) if (n.NodeType != 3) n.OutlinePressed = arr; });
                }
            }

            if (single && first.NodeType == 0 && GUILayout.Button(I18n.Tr("fm_reset_count"), GUILayout.Width(140f)))
            {
                first.Count = 0;
                if (first.RuntimeKey != null) first.RuntimeKey.LastShownKps = int.MinValue;
                RefreshAllCountDisplay();
                SaveSettingsFromGui();
            }
        }

        private void DrawEditorNodeTypeCombo(KvNode node)
        {
            string[] names = { I18n.Tr("fm_add_key"), I18n.Tr("fm_add_kps"), I18n.Tr("fm_add_total"), I18n.Tr("fm_add_image") };
            GUILayout.BeginHorizontal();
            GUILayout.Label(I18n.Tr("fm_node_type"), GUILayout.Width(96f));
            int type = node.NodeType;
            int newType = GUILayout.SelectionGrid(type, names, 4, GUILayout.Height(20f));
            GUILayout.EndHorizontal();
            if (newType == type || newType < 0 || newType > 3) return;
            if ((newType == 1 && Settings.Data.CustomNodes.Any(n => n != null && n.NodeType == 1 && n != node))
                || (newType == 2 && Settings.Data.CustomNodes.Any(n => n != null && n.NodeType == 2 && n != node)))
                return; // uniqueness / 唯一性
            PushEditorHistory();
            node.NodeType = newType;
            EditorMutated();
        }

        /// <summary>Ghost-key capture row: the alternate trigger that drops ghost rain from this
        /// node's column (runtime fully supported it; the editor just never exposed it — the
        /// binding was only settable by hand-editing the profile JSON). / 鬼键捕获行：按它会从
        /// 该节点列掉落鬼雨的备用触发键（运行时早已完整支持，只是编辑器一直没有入口——此前
        /// 只能手改配置 JSON 设置）。</summary>
        private void DrawEditorGhostBindCapture(KvNode node)
        {
            GUILayout.BeginHorizontal();
            string bound = string.IsNullOrWhiteSpace(node.GhostKey) ? "None" : node.GhostKey;
            GUILayout.Label(I18n.Tr("fm_ghost_bind") + ": " + bound, GUILayout.Width(160f));
            bool capturing = fmCaptureGhostNode == node;
            if (GUILayout.Button(capturing ? I18n.Tr("fm_wait_key") : I18n.Tr("fm_bind"), GUILayout.Width(90f)))
                fmCaptureGhostNode = capturing ? null : node;
            if (GUILayout.Button(I18n.Tr("fm_clear"), GUILayout.Width(60f)))
            {
                node.GhostKey = "";
                fmCaptureGhostNode = null;
                EditorPropertyChanged();
            }
            GUILayout.EndHorizontal();
            if (capturing)
            {
                GUILayout.Label(I18n.Tr("fm_press_hint"));
                Event e = Event.current;
                if (e != null && e.type == EventType.KeyDown)
                {
                    if (e.keyCode == KeyCode.Escape)
                    {
                        fmCaptureGhostNode = null;
                        e.Use();
                    }
                    else if (e.keyCode != KeyCode.None
                        && (e.keyCode < KeyCode.Mouse0 || e.keyCode > KeyCode.Mouse6)
                        && e.keyCode != KeyCode.Return)
                    {
                        node.GhostKey = e.keyCode.ToString();
                        fmCaptureGhostNode = null;
                        e.Use();
                        EditorPropertyChanged();
                    }
                }
            }
        }

        private void DrawEditorKeyBindCapture(KvNode node)
        {
            GUILayout.BeginHorizontal();
            string bound = string.IsNullOrEmpty(node.KeyBind) ? "None" : node.KeyBind;
            GUILayout.Label(I18n.Tr("fm_bind") + ": " + bound, GUILayout.Width(160f));
            bool capturing = fmCaptureNode == node;
            if (GUILayout.Button(capturing ? I18n.Tr("fm_wait_key") : I18n.Tr("fm_bind"), GUILayout.Width(90f)))
                fmCaptureNode = capturing ? null : node;
            if (GUILayout.Button(I18n.Tr("fm_clear"), GUILayout.Width(60f)))
            {
                node.KeyBind = "";
                node.CustomText = "";
                fmCaptureNode = null;
                EditorPropertyChanged();
            }
            GUILayout.EndHorizontal();
            if (capturing)
            {
                GUILayout.Label(I18n.Tr("fm_press_hint"));
                Event e = Event.current;
                if (e != null && e.type == EventType.KeyDown)
                {
                    if (e.keyCode == KeyCode.Escape)
                    {
                        fmCaptureNode = null;
                        e.Use();
                    }
                    else if (e.keyCode != KeyCode.None
                        && (e.keyCode < KeyCode.Mouse0 || e.keyCode > KeyCode.Mouse6)
                        && e.keyCode != KeyCode.Return)
                    {
                        node.KeyBind = e.keyCode.ToString();
                        node.CustomText = KeyToString(e.keyCode);
                        fmCaptureNode = null;
                        e.Use();
                        EditorPropertyChanged();
                    }
                }
            }
        }

        /// <summary>Node font size (0 = follow the global key font size) / 节点字号（0 = 跟随全局按键字号）</summary>
        private void DrawEditorFontSize(KvNode first)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(I18n.Tr("key_font_size"), GUILayout.Width(96f));
            int size = Mathf.RoundToInt(GUILayout.HorizontalSlider(first.FontSize, 0f, 72f));
            // Mixed sizes show "—" (never parses → no accidental mass-apply); deliberate input
            // still applies to all. / 字号不一致时显示"—"（永不解析→不会意外群发）；有意输入
            // 仍然应用到全部。
            bool mixed = false;
            for (int i = 1; i < editorSelection.Count; i++)
                if (!Mathf.Approximately(editorSelection[i].FontSize, first.FontSize)) { mixed = true; break; }
            string text = TextInputField("fme_fs_" + first.Id, mixed ? "—" : size.ToString(), GUILayout.Width(56f));
            if (int.TryParse(text, out int parsed)) size = Mathf.Clamp(parsed, 0, 72);
            GUILayout.Label(size <= 0 ? I18n.Tr("fm_font_global") : size + "px", GUILayout.Width(48f));
            GUILayout.EndHorizontal();
            if (!Mathf.Approximately(size, first.FontSize) && (!mixed || int.TryParse(text, out _)))
            {
                foreach (KvNode n in editorSelection) n.FontSize = size;
                EditorPropertyChanged();
            }
        }

        private void DrawEditorToggle(string label, bool value, Action<bool> apply)
        {
            bool newValue = GUILayout.Toggle(value, label);
            if (newValue == value) return;
            apply(newValue);
            EditorPropertyChanged();
        }

        private void DrawEditorTextField(string label, string ctrl, string value, Action<string> apply)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(96f));
            string text = TextInputField(ctrl, value ?? "", GUILayout.MinWidth(120f));
            GUILayout.EndHorizontal();
            if (!string.Equals(text, value ?? "", StringComparison.Ordinal)) apply(text ?? "");
        }

        /// <summary>Multi-aware float field. When the selection DISAGREES on the value the field
        /// shows "—" — the active node's value can no longer masquerade as the group's and get
        /// mass-applied by an accidental commit. "—" never parses; typing a number applies it to
        /// the whole selection. Uniform selections behave exactly as before.
        /// / 多选感知浮点字段。选区在该值上不一致时显示"—"——活动节点的值不再冒充组值、也不会
        /// 被一次意外提交群体覆盖；"—"永不解析，输入数字即应用到全部。一致时行为与从前相同。
        /// </summary>
        private void DrawEditorFloatField(string label, string ctrl, Func<KvNode, float> get, Action<float> apply)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(96f));
            float v0 = get(editorSelection[0]);
            bool mixed = false;
            for (int i = 1; i < editorSelection.Count; i++)
            {
                if (Math.Abs(get(editorSelection[i]) - v0) > 0.001f)
                {
                    mixed = true;
                    break;
                }
            }
            string seed = mixed ? "—" : v0.ToString("0.##");
            string text = TextInputField(ctrl, seed, GUILayout.Width(110f));
            if (float.TryParse(text, out float parsed) && IsFiniteFloat(parsed) && (mixed || Math.Abs(parsed - v0) > 0.001f))
                apply(parsed);
            GUILayout.EndHorizontal();
        }

        private void DrawEditorColorField(string label, float[] arr, Color fallback, Action<float[]> apply)
        {
            Color cur = NodeColor(arr, fallback);
            Color next = DrawColorPicker(label, cur, fallback);
            if (next != cur)
            {
                apply(new[] { next.r, next.g, next.b, next.a });
                EditorPropertyChanged();
            }
        }

        private void EditorPropertyChanged()
        {
            SaveSettingsFromGui();
            RequestEditorRebuild();
        }

        private void EditorImportImages()
        {
            foreach (KvNode node in editorSelection)
            {
                if (node == null || node.NodeType != 3) continue;
                string p = node.ImagePath?.Trim();
                if (string.IsNullOrWhiteSpace(p)) continue;
                try
                {
                    string abs = Path.IsPathRooted(p) ? p : Path.GetFullPath(Path.Combine(Loader.ModPath, p));
                    if (!File.Exists(abs)) continue;
                    string dir = Path.Combine(Loader.ModPath, "CustomImages");
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    string dest = Path.Combine(dir, Path.GetFileName(abs));
                    if (!string.Equals(Path.GetFullPath(abs), Path.GetFullPath(dest), StringComparison.OrdinalIgnoreCase))
                        File.Copy(abs, dest, true);
                    node.ImagePath = Path.GetFileName(abs);
                }
                catch (Exception e)
                {
                    Loader.Error($"KeyViewer: image import failed: {e.Message}");
                }
            }
            EditorPropertyChanged();
        }

        private void OpenCustomImagesDir()
        {
            try
            {
                string dir = Path.Combine(Loader.ModPath, "CustomImages");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                System.Diagnostics.Process.Start("explorer.exe", dir);
            }
            catch (Exception e)
            {
                Loader.Error($"KeyViewer: failed to open folder: {e.Message}");
            }
        }

        /// <summary>Editor-scoped stale-buffer GC: only "fme_" entries belong to this window's
        /// pass; settings-window buffers are managed by EndTextInputPass and are never touched
        /// here (and vice versa). / 编辑器范围的陈旧缓冲回收：只处理 "fme_" 前缀；设置窗口的
        /// 缓冲归 EndTextInputPass 管，二者互不越界。</summary>
        private void EditorGcBuffers()
        {
            if (textInputBuffer.Count == 0) return;
            string focused = GUI.GetNameOfFocusedControl();
            fmScratchKeys.Clear();
            foreach (string key in textInputBuffer.Keys)
            {
                if (!key.StartsWith("fme_", StringComparison.Ordinal)) continue;
                if (textCtrlsDrawnThisPass.Contains(key) || key == focused) continue;
                fmScratchKeys.Add(key);
            }
            for (int i = 0; i < fmScratchKeys.Count; i++)
                textInputBuffer.Remove(fmScratchKeys[i]);
            textCtrlsDrawnThisPass.Clear();
        }
    }
}
