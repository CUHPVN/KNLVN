#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using KNLVN.Game;

namespace KNLVN.Editor
{
    /// <summary>
    /// Level Editor Window for Math Sokoban.
    ///
    /// Open via:  Tools → KNLVN → Level Editor
    ///
    /// Workflow:
    ///   1. Set Width / Height → click "New Grid"
    ///   2. Select a Cell Type tool from the palette
    ///   3. Left-click / drag on grid cells to paint
    ///   4. For Blue/Yellow/Star cells click "Edit Content" to type a value
    ///   5. Click "Set Player Start" then click a cell to reposition the player
    ///   6. "Save As" → choose path → writes a LevelData ScriptableObject
    ///   7. "Import" → load an existing LevelData asset to continue editing
    /// </summary>
    public class LevelEditorWindow : EditorWindow
    {
        // ─── Menu ─────────────────────────────────────────────────────────────
        [MenuItem("Tools/KNLVN/Level Editor")]
        public static void Open() => GetWindow<LevelEditorWindow>("Level Editor");

        // ─── Grid state (applied only on "New Grid" or import) ────────────────
        private int _width  = 10;
        private int _height = 8;

        // Pending fields bound to the input boxes — never used for array indexing
        private int _pendingWidth  = 10;
        private int _pendingHeight = 8;

        // Flat array [x + y*width], origin at bottom-left
        private CellType[] _types;
        private string[]   _contents;   // "" = no content
        private Vector2Int _playerStart = new Vector2Int(1, 1);

        // ─── Palette ──────────────────────────────────────────────────────────
        private enum Tool { Paint, Erase, SetPlayer, EditContent }

        private Tool     _activeTool = Tool.Paint;
        private CellType _paintType  = CellType.Wall;

        // Valid content values
        private static readonly string[] ContentOptions =
            { "", "0","1","2","3","4","5","6","7","8","9","+","-","x","/","=" };

        // ─── Scroll / zoom ────────────────────────────────────────────────────
        private Vector2 _scroll;
        private float   _cellPx = 48f;

        // ─── Current asset ────────────────────────────────────────────────────
        private LevelData _loadedAsset;

        // ─── Colors ───────────────────────────────────────────────────────────
        private static readonly Dictionary<CellType, Color> CellColors = new()
        {
            { CellType.Empty,  new Color(0.82f, 0.80f, 0.75f) },
            { CellType.Wall,   new Color(0.22f, 0.22f, 0.25f) },
            { CellType.Blue,   new Color(0.27f, 0.52f, 0.90f) },
            { CellType.Yellow, new Color(0.98f, 0.84f, 0.20f) },
            { CellType.Red,    new Color(0.85f, 0.22f, 0.22f) },
            { CellType.Star,   new Color(1.00f, 0.72f, 0.10f) },
        };

        // ─── Lifecycle ────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (_types == null || _types.Length != _width * _height)
                InitGrid();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(4);
            DrawPalette();
            EditorGUILayout.Space(4);
            DrawGrid();
            EditorGUILayout.Space(4);
            DrawStatusBar();
        }

        // ─── Toolbar ──────────────────────────────────────────────────────────

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // Dimensions — use PENDING fields; never affects array size here
                EditorGUILayout.LabelField("W", GUILayout.Width(14));
                _pendingWidth  = EditorGUILayout.IntField(_pendingWidth,  GUILayout.Width(36));
                EditorGUILayout.LabelField("H", GUILayout.Width(14));
                _pendingHeight = EditorGUILayout.IntField(_pendingHeight, GUILayout.Width(36));
                _pendingWidth  = Mathf.Clamp(_pendingWidth,  3, 30);
                _pendingHeight = Mathf.Clamp(_pendingHeight, 3, 30);

                // Warn if pending differs from applied
                bool sizeChanged = _pendingWidth != _width || _pendingHeight != _height;
                GUI.color = sizeChanged ? new Color(1f, 0.85f, 0.3f) : Color.white;

                if (GUILayout.Button("New Grid", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    string msg = sizeChanged
                        ? $"Apply new size {_pendingWidth}×{_pendingHeight} and discard current layout?"
                        : "Reset grid and discard current layout?";

                    if (EditorUtility.DisplayDialog("New Grid", msg, "Yes", "Cancel"))
                    {
                        _width  = _pendingWidth;
                        _height = _pendingHeight;
                        InitGrid();
                    }
                }
                GUI.color = Color.white;

                GUILayout.Space(12);

                // Zoom
                EditorGUILayout.LabelField("Zoom", GUILayout.Width(36));
                _cellPx = EditorGUILayout.Slider(_cellPx, 24f, 80f, GUILayout.Width(120));

                GUILayout.FlexibleSpace();

                // Import / Save
                if (GUILayout.Button("Import…", EditorStyles.toolbarButton, GUILayout.Width(64)))
                    ImportLevel();

                if (GUILayout.Button("Save As…", EditorStyles.toolbarButton, GUILayout.Width(72)))
                    SaveLevel();

                if (_loadedAsset != null &&
                    GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(52)))
                    SaveToAsset(_loadedAsset);

                string assetLabel = _loadedAsset != null ? _loadedAsset.name : "(unsaved)";
                EditorGUILayout.LabelField(assetLabel, EditorStyles.miniLabel, GUILayout.Width(120));
            }
        }

        // ─── Palette ──────────────────────────────────────────────────────────

        private void DrawPalette()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Tool:", GUILayout.Width(36));

                foreach (var ct in System.Enum.GetValues(typeof(CellType)).Cast<CellType>())
                {
                    bool active = _activeTool == Tool.Paint && _paintType == ct;
                    GUI.backgroundColor = active ? Color.white : CellColors[ct] * 0.85f;
                    if (GUILayout.Toggle(active, ct.ToString(), "Button", GUILayout.Width(64)) && !active)
                    {
                        _activeTool = Tool.Paint;
                        _paintType  = ct;
                    }
                    GUI.backgroundColor = Color.white;
                }

                GUILayout.Space(8);

                DrawToolToggle(ref _activeTool, Tool.Erase,     "✕ Erase",   Color.red * 1.2f,             68);
                DrawToolToggle(ref _activeTool, Tool.SetPlayer,  "👤 Player", new Color(0.18f, 0.72f, 0.9f), 72);
                DrawToolToggle(ref _activeTool, Tool.EditContent,"✏ Content", new Color(0.9f, 0.9f, 0.3f),   78);
            }
        }

        private static void DrawToolToggle(ref Tool current, Tool target, string label, Color activeColor, float w)
        {
            bool active = current == target;
            GUI.backgroundColor = active ? activeColor : Color.white;
            if (GUILayout.Toggle(active, label, "Button", GUILayout.Width(w)) && !active)
                current = target;
            GUI.backgroundColor = Color.white;
        }

        // ─── Grid canvas ──────────────────────────────────────────────────────

        private void DrawGrid()
        {
            // Guard: arrays must exist and match current grid dimensions
            if (_types == null || _contents == null ||
                _types.Length != _width * _height)
                return;

            float totalW = _width  * _cellPx;
            float totalH = _height * _cellPx;

            _scroll = EditorGUILayout.BeginScrollView(_scroll,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            Rect canvasRect = GUILayoutUtility.GetRect(totalW + 2, totalH + 2);

            // Draw cells
            for (int y = 0; y < _height; y++)
            for (int x = 0; x < _width;  x++)
            {
                Rect cell = CellScreenRect(canvasRect, x, y);
                DrawCell(cell, x, y);
            }

            // Handle mouse input
            HandleMouse(canvasRect);

            EditorGUILayout.EndScrollView();
        }

        private Rect CellScreenRect(Rect canvas, int x, int y)
        {
            float sy = canvas.yMin + (_height - 1 - y) * _cellPx;
            return new Rect(canvas.xMin + x * _cellPx, sy, _cellPx, _cellPx);
        }

        private void DrawCell(Rect rect, int x, int y)
        {
            int    idx  = Index(x, y);
            var    type = _types[idx];
            string ct   = _contents[idx];

            // Background
            EditorGUI.DrawRect(rect, CellColors[type]);
            DrawBorder(rect, new Color(0.1f, 0.1f, 0.1f, 0.6f));

            // Player start indicator
            if (_playerStart.x == x && _playerStart.y == y)
            {
                EditorGUI.DrawRect(rect.Inflated(-4), new Color(0.18f, 0.72f, 0.90f, 0.7f));
                GUI.Label(rect, "P", CenteredStyle(Color.white, 14, FontStyle.Bold));
            }

            // Coordinates (small, top-left)
            GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, 12),
                $"{x},{y}", SmallStyle(new Color(0, 0, 0, 0.35f)));

            // Content label
            if (!string.IsNullOrEmpty(ct))
            {
                string label = (type == CellType.Star) ? $"{ct}x2" : ct;
                GUI.Label(rect, label,
                    CenteredStyle(type == CellType.Wall ? Color.gray : Color.white, 16, FontStyle.Bold));
            }
            else if (type != CellType.Wall && type != CellType.Empty)
            {
                GUI.Label(rect, type.ToString()[0].ToString(),
                    CenteredStyle(new Color(1, 1, 1, 0.35f), 11, FontStyle.Normal));
            }
        }

        private void HandleMouse(Rect canvasRect)
        {
            Event e = Event.current;
            if (!canvasRect.Contains(e.mousePosition)) return;
            if (e.type != EventType.MouseDown && e.type != EventType.MouseDrag) return;
            if (e.button != 0) return;

            float rx = e.mousePosition.x - canvasRect.xMin;
            float ry = e.mousePosition.y - canvasRect.yMin;
            int gx   = Mathf.FloorToInt(rx / _cellPx);
            int gy   = _height - 1 - Mathf.FloorToInt(ry / _cellPx);

            if (gx < 0 || gx >= _width || gy < 0 || gy >= _height) return;

            int idx = Index(gx, gy);

            switch (_activeTool)
            {
                case Tool.Paint:
                    _types[idx] = _paintType;
                    if (_paintType == CellType.Wall || _paintType == CellType.Empty)
                        _contents[idx] = "";
                    MarkDirty();
                    break;

                case Tool.Erase:
                    _types[idx]    = CellType.Empty;
                    _contents[idx] = "";
                    MarkDirty();
                    break;

                case Tool.SetPlayer:
                    _playerStart = new Vector2Int(gx, gy);
                    MarkDirty();
                    break;

                case Tool.EditContent:
                    if (e.type == EventType.MouseDown)
                        OpenContentPopup(gx, gy);
                    break;
            }

            e.Use();
        }

        // ─── Content popup ────────────────────────────────────────────────────

        private void OpenContentPopup(int x, int y)
        {
            int idx  = Index(x, y);
            var type = _types[idx];

            if (type == CellType.Wall)
            {
                ShowNotification(new GUIContent("Wall cells cannot have content."));
                return;
            }

            var menu = new GenericMenu();
            foreach (var opt in ContentOptions)
            {
                string display  = string.IsNullOrEmpty(opt) ? "(none)" : opt;
                string captured = opt;
                bool   isCurrent = _contents[idx] == opt;
                menu.AddItem(new GUIContent(display), isCurrent, () =>
                {
                    _contents[idx] = captured;
                    MarkDirty();
                    Repaint();
                });
            }
            menu.ShowAsContext();
        }

        // ─── Status bar ───────────────────────────────────────────────────────

        private void DrawStatusBar()
        {
            if (_types == null) return;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                int walls   = 0, blues = 0, yellows = 0, stars = 0, reds = 0, floors = 0;
                for (int i = 0; i < _types.Length; i++)
                {
                    switch (_types[i])
                    {
                        case CellType.Wall:   walls++;   break;
                        case CellType.Blue:   blues++;   break;
                        case CellType.Yellow: yellows++; break;
                        case CellType.Star:   stars++;   break;
                        case CellType.Red:    reds++;    break;
                        case CellType.Empty:
                            if (!string.IsNullOrEmpty(_contents[i])) floors++;
                            break;
                    }
                }

                bool sizeChanged = _pendingWidth != _width || _pendingHeight != _height;
                string sizeNote  = sizeChanged ? $"  ⚠ pending {_pendingWidth}×{_pendingHeight} (click New Grid)" : "";

                EditorGUILayout.LabelField(
                    $"{_width}×{_height}{sizeNote}  |  Player: ({_playerStart.x},{_playerStart.y})  |  " +
                    $"Wall:{walls}  Blue:{blues}  Yellow:{yellows}  Star:{stars}  Red:{reds}  Floor:{floors}",
                    EditorStyles.miniLabel);

                GUILayout.FlexibleSpace();

                string toolHint = _activeTool switch
                {
                    Tool.Paint       => $"🖌 Painting: {_paintType}",
                    Tool.Erase       => "✕ Erasing",
                    Tool.SetPlayer   => "👤 Click cell to set player start",
                    Tool.EditContent => "✏ Click cell to edit content",
                    _                => ""
                };
                EditorGUILayout.LabelField(toolHint, EditorStyles.miniLabel, GUILayout.Width(240));
            }
        }

        // ─── Grid init ────────────────────────────────────────────────────────

        private void InitGrid()
        {
            int total  = _width * _height;
            _types     = new CellType[total];
            _contents  = new string[total];

            for (int x = 0; x < _width;  x++)
            for (int y = 0; y < _height; y++)
            {
                bool border         = x == 0 || x == _width - 1 || y == 0 || y == _height - 1;
                _types[Index(x, y)] = border ? CellType.Wall : CellType.Empty;
                _contents[Index(x, y)] = "";
            }

            _playerStart = new Vector2Int(1, 1);
            _loadedAsset = null;

            // Sync pending to applied
            _pendingWidth  = _width;
            _pendingHeight = _height;

            Repaint();
        }

        private int Index(int x, int y) => x + y * _width;

        private void MarkDirty()
        {
            if (_loadedAsset != null) EditorUtility.SetDirty(_loadedAsset);
            Repaint();
        }

        // ─── Save ─────────────────────────────────────────────────────────────

        private void SaveLevel()
        {
            string defaultName = _loadedAsset != null ? _loadedAsset.name : "Level_New";
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Level", defaultName, "asset",
                "Choose where to save the LevelData asset.",
                "Assets/_Project/Resources/Levels");

            if (string.IsNullOrEmpty(path)) return;

            var existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (existing != null)
            {
                SaveToAsset(existing);
                _loadedAsset = existing;
            }
            else
            {
                var asset = ScriptableObject.CreateInstance<LevelData>();
                PopulateAsset(asset);
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                _loadedAsset = asset;
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;
            }

            ShowNotification(new GUIContent($"Saved: {System.IO.Path.GetFileName(path)}"));
        }

        private void SaveToAsset(LevelData asset)
        {
            Undo.RecordObject(asset, "Save Level");
            PopulateAsset(asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent($"Saved: {asset.name}"));
        }

        private void PopulateAsset(LevelData asset)
        {
            asset.Width          = _width;
            asset.Height         = _height;
            asset.PlayerStartPos = _playerStart;
            asset.Cells          = new List<CellDefinition>();

            for (int x = 0; x < _width;  x++)
            for (int y = 0; y < _height; y++)
            {
                int idx  = Index(x, y);
                var type = _types[idx];

                if (type == CellType.Empty && string.IsNullOrEmpty(_contents[idx]))
                    continue;

                asset.Cells.Add(new CellDefinition
                {
                    Pos     = new Vector2Int(x, y),
                    Type    = type,
                    Content = _contents[idx] ?? ""
                });
            }
        }

        // ─── Import ───────────────────────────────────────────────────────────

        private void ImportLevel()
        {
            string path = EditorUtility.OpenFilePanelWithFilters(
                "Import LevelData", "Assets", new[] { "LevelData Asset", "asset" });

            if (string.IsNullOrEmpty(path)) return;

            if (path.StartsWith(Application.dataPath))
                path = "Assets" + path[Application.dataPath.Length..];

            var asset = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (asset == null)
            {
                EditorUtility.DisplayDialog("Import Failed",
                    "Could not load a LevelData asset at that path.\n" +
                    "Make sure the file is inside the Assets folder.", "OK");
                return;
            }

            LoadFromAsset(asset);
        }

        private void LoadFromAsset(LevelData asset)
        {
            // Apply dimensions first so InitGrid uses the right size
            _width  = asset.Width;
            _height = asset.Height;

            int total  = _width * _height;
            _types     = new CellType[total];
            _contents  = new string[total];

            // Default: all empty
            for (int i = 0; i < total; i++)
            {
                _types[i]    = CellType.Empty;
                _contents[i] = "";
            }

            // Border walls
            for (int x = 0; x < _width;  x++)
            for (int y = 0; y < _height; y++)
            {
                if (x == 0 || x == _width - 1 || y == 0 || y == _height - 1)
                    _types[Index(x, y)] = CellType.Wall;
            }

            // Apply cells from asset
            foreach (var cd in asset.Cells)
            {
                if (cd.Pos.x < 0 || cd.Pos.x >= _width ||
                    cd.Pos.y < 0 || cd.Pos.y >= _height) continue;

                int idx        = Index(cd.Pos.x, cd.Pos.y);
                _types[idx]    = cd.Type;
                _contents[idx] = cd.Content ?? "";
            }

            _playerStart   = asset.PlayerStartPos;
            _loadedAsset   = asset;
            _pendingWidth  = _width;
            _pendingHeight = _height;

            Repaint();
            ShowNotification(new GUIContent($"Loaded: {asset.name}"));
        }

        // ─── GUI helpers ──────────────────────────────────────────────────────

        private static void DrawBorder(Rect rect, Color color)
        {
            float t = 1f;
            EditorGUI.DrawRect(new Rect(rect.x,        rect.y,        rect.width, t),      color);
            EditorGUI.DrawRect(new Rect(rect.x,        rect.yMax - t, rect.width, t),      color);
            EditorGUI.DrawRect(new Rect(rect.x,        rect.y,        t, rect.height),     color);
            EditorGUI.DrawRect(new Rect(rect.xMax - t, rect.y,        t, rect.height),     color);
        }

        private static GUIStyle CenteredStyle(Color color, int size, FontStyle style)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = size,
                fontStyle = style,
            };
            s.normal.textColor = color;
            return s;
        }

        private static GUIStyle SmallStyle(Color color)
        {
            var s = new GUIStyle(GUI.skin.label) { fontSize = 9 };
            s.normal.textColor = color;
            return s;
        }
    }

    // ─── Rect extension ───────────────────────────────────────────────────────

    internal static class RectExtensions
    {
        public static Rect Inflated(this Rect r, float amount) =>
            new Rect(r.x - amount, r.y - amount,
                     r.width + amount * 2, r.height + amount * 2);
    }
}
#endif
