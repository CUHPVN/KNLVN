using System.Collections.Generic;
using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// Owns and provides access to all <see cref="GameGridCell"/>s in the current level.
    /// Built from a <see cref="LevelData"/> asset by <see cref="LevelManager"/>.
    /// </summary>
    public class GameGrid : CUHP.Grid<GameGridCell>
    {
        // ─── Constructor ──────────────────────────────────────────────────────

        public GameGrid(int width, int height, float cellSize = 1f, Vector3 originPosition = default, bool showDebug = false)
            : base(width, height, cellSize, originPosition,
                   (g, x, y) => new GameGridCell(new Vector2Int(x, y), CellType.Empty),
                   showDebug)
        {
        }

        // ─── Dimensions ───────────────────────────────────────────────────────
        public int Width => GetWidth();
        public int Height => GetHeight();

        // ─── Access ───────────────────────────────────────────────────────────

        public GameGridCell GetCell(int x, int y)
        {
            if (!InBounds(x, y)) return null;
            return GetGridObject(x, y);
        }

        public GameGridCell GetCell(Vector2Int pos) => GetCell(pos.x, pos.y);

        public void SetCell(GameGridCell cell)
        {
            if (cell == null || !InBounds(cell.GridPos)) return;
            SetGridObject(cell.GridPos.x, cell.GridPos.y, cell);
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public bool InBounds(Vector2Int pos) => InBounds(pos.x, pos.y);

        // ─── Iteration helpers ────────────────────────────────────────────────

        /// <summary>Returns all Blue cells (used by equation evaluator).</summary>
        public List<GameGridCell> GetBlueCells()
        {
            var result = new List<GameGridCell>();
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    var cell = GetCell(x, y);
                    if (cell != null && cell.IsBlue) result.Add(cell);
                }
            return result;
        }

        /// <summary>Returns every cell of the given type.</summary>
        public List<GameGridCell> GetCellsOfType(CellType type)
        {
            var result = new List<GameGridCell>();
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    var cell = GetCell(x, y);
                    if (cell != null && cell.CellType == type) result.Add(cell);
                }
            return result;
        }

        // ─── Snapshot (for Undo) ──────────────────────────────────────────────

        public List<GameGridCellSnapshot> TakeSnapshot()
        {
            var snap = new List<GameGridCellSnapshot>(Width * Height);
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    var cell = GetCell(x, y);
                    if (cell != null) snap.Add(cell.TakeSnapshot());
                }
            return snap;
        }

        public void RestoreSnapshot(List<GameGridCellSnapshot> snap)
        {
            foreach (var s in snap)
                GetCell(s.GridPos)?.RestoreSnapshot(s);
        }
    }
}
