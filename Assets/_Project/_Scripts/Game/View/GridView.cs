using System.Collections.Generic;
using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// Spawns one <see cref="CellView"/> per grid cell and refreshes all of them
    /// after any game action (move, push, interact, undo, reset).
    ///
    /// Refresh strategy:
    ///   • <see cref="PlayerMovedEvent"/>          → refreshAll (covers moves AND pushes)
    ///   • <see cref="UndoPerformedEvent"/>         → refreshAll
    ///   • <see cref="LevelLoadedEvent"/>           → rebuild
    ///   • <see cref="LevelResetEvent"/>            → rebuild
    /// </summary>
    public class GridView : MonoBehaviour
    {
        [SerializeField] private LevelManager      _levelManager;
        [SerializeField] private EventBusComponent _eventBus;
        [SerializeField] private GameVisualConfig  _visualConfig;

        // Flat list of all spawned CellViews — used for batch refresh
        private readonly List<CellView> _cellViews = new List<CellView>();

        // Floor background (single tiled sprite behind the whole grid)
        private GameObject _floorBg;

        // Cached shared overlay sprites (generated once per rebuild)
        private Sprite _contentPanelSprite;
        private Sprite _floorTokenSprite;

        // ─── Cached handlers ──────────────────────────────────────────────────
        private System.Action<PlayerMovedEvent>    _onPlayerMoved;
        private System.Action<UndoPerformedEvent>  _onUndoPerformed;
        private System.Action<LevelLoadedEvent>    _onLevelLoaded;
        private System.Action<LevelResetEvent>     _onLevelReset;
        private System.Action<EquationSolvedEvent> _onEquationSolved;
        private System.Action<PlayerHeldItemChangedEvent> _onHeldItemChanged;

        // ─── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _onPlayerMoved   = evt =>
            {
                if (evt.PushedBoxToPos.HasValue)
                    AnimatePushedBox(evt.PushedBoxFromPos!.Value, evt.PushedBoxToPos.Value);
                RefreshAllCells();
            };
            _onUndoPerformed = evt =>
            {
                // Animate box sliding back if this undo reversed a push
                if (evt.BoxWasAt.HasValue && evt.BoxNowAt.HasValue)
                    AnimatePushedBox(evt.BoxWasAt.Value, evt.BoxNowAt.Value);
                RefreshAllCells();
            };
            _onLevelLoaded   = _ => RebuildViews();
            _onLevelReset    = _ => RebuildViews();
            _onEquationSolved = evt => BounceChain(evt.ChainPositions);
            _onHeldItemChanged = _ => RefreshAllCells();
        }

        private void OnEnable()
        {
            _eventBus?.Subscribe(_onPlayerMoved);
            _eventBus?.Subscribe(_onUndoPerformed);
            _eventBus?.Subscribe(_onLevelLoaded);
            _eventBus?.Subscribe(_onLevelReset);
            _eventBus?.Subscribe(_onEquationSolved);
            _eventBus?.Subscribe(_onHeldItemChanged);
        }

        private void OnDisable()
        {
            _eventBus?.Unsubscribe(_onPlayerMoved);
            _eventBus?.Unsubscribe(_onUndoPerformed);
            _eventBus?.Unsubscribe(_onLevelLoaded);
            _eventBus?.Unsubscribe(_onLevelReset);
            _eventBus?.Unsubscribe(_onEquationSolved);
            _eventBus?.Unsubscribe(_onHeldItemChanged);
        }

        // ─── Build ────────────────────────────────────────────────────────────

        private void RebuildViews()
        {
            // Destroy old views
            foreach (Transform child in transform)
                Destroy(child.gameObject);
            _cellViews.Clear();

            var grid = _levelManager?.Grid;
            if (grid == null) return;

            var cfg = _visualConfig;
            if (cfg == null)
            {
                KNLVN.GameDebug.LogWarning("[GridView] GameVisualConfig not assigned.");
                return;
            }

            // Generate shared overlay sprites once per level
            _contentPanelSprite = cfg.GetContentPanelSprite();
            _floorTokenSprite   = cfg.GetFloorTokenSprite();

            float cellSize = grid.GetCellSize();

            BuildFloorBackground(grid, cellSize, cfg);

            for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
            {
                var go = new GameObject($"Cell_{x}_{y}");
                go.transform.SetParent(transform, false);
                go.transform.position   = grid.GetCenterWorldPosition(x, y);
                go.transform.localScale = Vector3.one * cellSize;

                var view = go.AddComponent<CellView>();
                view.Init(x, y, grid, cfg, _eventBus, _contentPanelSprite, _floorTokenSprite);
                _cellViews.Add(view);

                // Also store on cell data for external access
                var cell = grid.GetCell(x, y);
                if (cell != null) cell.View = go;
            }

            KNLVN.GameDebug.Log($"[GridView] Built {_cellViews.Count} cell views.");
        }

        // ─── Floor background ────────────────────────────────────────────────

        /// <summary>
        /// Spawns (or re-uses) a single tiled <see cref="SpriteRenderer"/> that covers
        /// the entire grid floor. sortingOrder = -1 so it sits below all cell sprites.
        /// </summary>
        private void BuildFloorBackground(GameGrid grid, float cellSize, GameVisualConfig cfg)
        {
            // Destroy old background if rebuilding
            if (_floorBg != null) Destroy(_floorBg);

            _floorBg = new GameObject("FloorBackground");
            _floorBg.transform.SetParent(transform, false);

            // Bottom-left corner of the whole grid in world space
            Vector3 origin = grid.GetCenterWorldPosition(0, 0)
                             - new Vector3(cellSize, cellSize) * 0.5f;

            // Centre of the floor quad
            float totalW = grid.Width  * cellSize;
            float totalH = grid.Height * cellSize;
            _floorBg.transform.position = origin + new Vector3(totalW * 0.5f, totalH * 0.5f, 1f);

            var sr        = _floorBg.AddComponent<SpriteRenderer>();
            sr.sprite     = cfg.GetFloorTileSprite();
            sr.drawMode   = SpriteDrawMode.Tiled;
            sr.tileMode   = SpriteTileMode.Continuous;
            sr.size        = new Vector2(totalW, totalH);
            sr.sortingOrder = -1;
        }

        // ─── Batch refresh ────────────────────────────────────────────────────

        /// <summary>
        /// Refreshes all cell views from current grid state.
        /// Called after any action that may have changed grid contents.
        /// </summary>
        private void RefreshAllCells()
        {
            foreach (var view in _cellViews)
                view.Refresh();
        }

        // ─── Push animation ───────────────────────────────────────────────────

        /// <summary>
        /// Finds the CellView that landed at <paramref name="toPos"/> and animates it
        /// flying in from the world position of <paramref name="fromPos"/>.
        /// Must be called BEFORE RefreshAllCells so the view is already at fromPos
        /// visually (grid data is already updated at this point).
        /// </summary>
        private void AnimatePushedBox(Vector2Int fromPos, Vector2Int toPos)
        {
            var grid = _levelManager?.Grid;
            if (grid == null) return;

            // _cellViews is built column-major (x-outer, y-inner): index = x * height + y
            int h       = grid.Height;
            int toIndex = toPos.x * h + toPos.y;

            if (toIndex < 0 || toIndex >= _cellViews.Count) return;

            var view = _cellViews[toIndex];

            Vector3 worldFrom = grid.GetCenterWorldPosition(fromPos.x, fromPos.y);
            Vector3 worldTo   = grid.GetCenterWorldPosition(toPos.x,   toPos.y);

            // Teleport view to the "from" position, then animate to "to"
            view.transform.position = worldFrom;
            view.AnimatePush(worldFrom, worldTo);
        }

        // ─── Bounce (equation solved) ─────────────────────────────────────────

        /// <summary>
        /// Triggers a staggered bounce on every CellView in the solved chain.
        /// Cells pop one after another (wave effect) with a 0.05 s stagger.
        /// </summary>
        private void BounceChain(System.Collections.Generic.List<Vector2Int> positions)
        {
            if (positions == null) return;
            var grid = _levelManager?.Grid;
            if (grid == null) return;

            int   h       = grid.Height;
            float stagger = 0.05f;

            for (int i = 0; i < positions.Count; i++)
            {
                var pos   = positions[i];
                int index = pos.x * h + pos.y;
                if (index < 0 || index >= _cellViews.Count) continue;
                _cellViews[index].PlayBounce(delay: i * stagger);
            }
        }
    }
}
