using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// Runtime data for a single cell in the game grid.
    /// Tracks type, content, and whether a floor item is present.
    /// </summary>
    public class GameGridCell
    {
        // ─── Grid position ────────────────────────────────────────────────────
        public Vector2Int GridPos { get; }

        // ─── Cell structural type (Blue, Yellow, Wall, …) ────────────────────
        public CellType CellType { get; private set; }

        // ─── Value carried by Blue/Yellow/Star cells ──────────────────────────
        /// <summary>Content embedded in the cell itself (for Blue fixed-value cells).</summary>
        public CellContent Content { get; private set; } = CellContent.Empty;

        // ─── Floor item (loose number/operator lying on empty floor) ──────────
        /// <summary>A loose item placed on the floor of an Empty cell.</summary>
        public CellContent FloorItem { get; private set; } = CellContent.Empty;

        /// <summary>True when there is a floor item on this cell.</summary>
        public bool HasFloorItem => !FloorItem.IsEmpty;

        // ─── View reference ───────────────────────────────────────────────────
        /// <summary>The Unity GameObject representing this cell in the scene.</summary>
        public GameObject View { get; set; }

        // ─── Constructor ──────────────────────────────────────────────────────

        public GameGridCell(Vector2Int gridPos, CellType cellType, CellContent content = null)
        {
            GridPos  = gridPos;
            CellType = cellType;
            Content  = content ?? CellContent.Empty;
        }

        // ─── Mutation API ─────────────────────────────────────────────────────

        public void SetContent(CellContent content)  => Content   = content ?? CellContent.Empty;
        public void ClearContent()                   => Content   = CellContent.Empty;
        public void SetFloorItem(CellContent item)   => FloorItem = item    ?? CellContent.Empty;
        public void ClearFloorItem()                 => FloorItem = CellContent.Empty;
        public void SetCellType(CellType type)       => CellType  = type;

        // ─── Query helpers ────────────────────────────────────────────────────

        public bool IsWall      => CellType == CellType.Wall;
        public bool IsBlue      => CellType == CellType.Blue;
        public bool IsYellow    => CellType == CellType.Yellow;
        public bool IsStar      => CellType == CellType.Star;
        public bool IsRed       => CellType == CellType.Red;
        public bool IsEmpty     => CellType == CellType.Empty;
        public bool IsPushable  => IsYellow || IsStar;

        /// <summary>True when a pushable cell has no content (can absorb floor items).</summary>
        public bool IsEmptyPushable => IsPushable && Content.IsEmpty;

        /// <summary>True when the player can step onto this cell (not wall, not blue).</summary>
        public bool IsWalkable(bool doorOpen) =>
            !IsWall && !IsBlue;

        // ─── Snapshot (for Undo) ──────────────────────────────────────────────

        public GameGridCellSnapshot TakeSnapshot() =>
            new GameGridCellSnapshot(GridPos, CellType, Content, FloorItem);

        public void RestoreSnapshot(GameGridCellSnapshot snap)
        {
            CellType  = snap.CellType;
            Content   = snap.Content;
            FloorItem = snap.FloorItem;
        }

        public override string ToString() =>
            $"[{GridPos}] {CellType} | Content={Content} | Floor={FloorItem}";
    }

    // ─── Lightweight snapshot struct ──────────────────────────────────────────

    public readonly struct GameGridCellSnapshot
    {
        public readonly Vector2Int GridPos;
        public readonly CellType   CellType;
        public readonly CellContent Content;
        public readonly CellContent FloorItem;

        public GameGridCellSnapshot(Vector2Int pos, CellType type, CellContent content, CellContent floor)
        {
            GridPos   = pos;
            CellType  = type;
            Content   = content;
            FloorItem = floor;
        }
    }
}
