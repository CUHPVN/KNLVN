using System.Collections.Generic;
using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// Builds the <see cref="GameGrid"/> from a <see cref="LevelData"/> asset,
    /// manages the current level lifecycle (load / reset),
    /// and serves as the single source of truth for grid state.
    /// </summary>
    public class LevelManager : Singleton<LevelManager>
    {
        // ─── Settings ─────────────────────────────────────────────────────────
        [SerializeField] private float _cellSize = 1f;

        [Header("Debug")]
        [Tooltip("Hiện text tọa độ và đường kẻ ô lưới (CodeMonkey debug). Tắt khi play thật.")]
        [SerializeField] private bool _showDebugGrid = false;

        // ─── Dependencies (assign in Inspector) ───────────────────────────────
        [SerializeField] private LevelData         _levelData;
        [SerializeField] private EventBusComponent _eventBus;

        // ─── Runtime state ────────────────────────────────────────────────────
        public GameGrid  Grid         { get; private set; }
        public LevelData CurrentLevel { get; private set; }

        // ─── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            LoadLevel(_levelData);
        }

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>Build the grid from the given level asset and notify all systems.</summary>
        public void LoadLevel(LevelData data)
        {
            if (data == null)
            {
                Debug.LogError("[LevelManager] LevelData is null.");
                return;
            }

            CurrentLevel = data;
            Grid = BuildGrid(data);

            _eventBus?.Publish(new LevelLoadedEvent { Data = data });
            Debug.Log($"[LevelManager] Level loaded: {data.name} ({data.Width}×{data.Height})");
        }

        /// <summary>Rebuild the grid from the same level (full reset).</summary>
        public void ResetLevel()
        {
            LoadLevel(CurrentLevel);
            _eventBus?.Publish(new LevelResetEvent());
            Debug.Log("[LevelManager] Level reset.");
        }

        // ─── Private helpers ──────────────────────────────────────────────────

        private GameGrid BuildGrid(LevelData data)
        {
            var grid = new GameGrid(data.Width, data.Height, _cellSize, transform.position, _showDebugGrid);

            // Fill border with walls if not defined in data
            FillBorderWalls(grid, data.Width, data.Height);

            // Apply explicit cell definitions
            foreach (var def in data.Cells)
            {
                if (!grid.InBounds(def.Pos))
                {
                    Debug.LogWarning($"[LevelManager] Cell at {def.Pos} is out of bounds. Skipped.");
                    continue;
                }

                var content = CellContent.FromRaw(def.Content);

                // Floor items live on Empty cells; the cell type stays Empty.
                if (def.Type == CellType.Empty && !content.IsEmpty)
                {
                    var cell = new GameGridCell(def.Pos, CellType.Empty);
                    cell.SetFloorItem(content);
                    grid.SetCell(cell);
                }
                else
                {
                    var cell = new GameGridCell(def.Pos, def.Type, content);
                    grid.SetCell(cell);
                }
            }

            return grid;
        }

        private static void FillBorderWalls(GameGrid grid, int w, int h)
        {
            for (int x = 0; x < w; x++)
            {
                grid.SetCell(new GameGridCell(new Vector2Int(x, 0),     CellType.Wall));
                grid.SetCell(new GameGridCell(new Vector2Int(x, h - 1), CellType.Wall));
            }
            for (int y = 1; y < h - 1; y++)
            {
                grid.SetCell(new GameGridCell(new Vector2Int(0,     y), CellType.Wall));
                grid.SetCell(new GameGridCell(new Vector2Int(w - 1, y), CellType.Wall));
            }
        }
    }
}
