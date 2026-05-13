using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// Encapsulates all push-related rules.
    /// Called by <see cref="PlayerController"/> before a move is committed.
    /// </summary>
    public class PushSystem
    {
        private readonly GameGrid _grid;

        public PushSystem(GameGrid grid) => _grid = grid;

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Checks whether the player moving from <paramref name="playerPos"/> in
        /// <paramref name="dir"/> would push a cell, and whether that push is legal.
        /// </summary>
        /// <param name="playerPos">Current player position.</param>
        /// <param name="dir">Move direction.</param>
        /// <param name="doorOpen">Current door state (needed to evaluate passability).</param>
        /// <returns>True if the push is valid and was executed.</returns>
        public bool TryPush(Vector2Int playerPos, Vector2Int dir, bool doorOpen)
        {
            Vector2Int targetPos = playerPos + dir;
            var targetCell = _grid.GetCell(targetPos);

            if (targetCell == null || !targetCell.IsPushable) return false;

            // ── Cannot push a cell that already contains a value ──────────────
            // Rule: only empty pushable cells can be pushed freely;
            // cells with content can still be pushed IFF the landing spot is free.
            // (GDD says "cannot push valued cells" into items — they must land on empty floor.)

            Vector2Int landingPos = targetPos + dir;
            var landingCell = _grid.GetCell(landingPos);

            if (landingCell == null)                         return false; // out of bounds
            if (landingCell.IsWall)                          return false;
            if (landingCell.IsBlue)                          return false;
            if (landingCell.IsPushable)                      return false; // can only push 1 at a time
            if (landingCell.IsRed && !doorOpen)              return false; // door is locked
            // A valued box can land on a floor-item cell: the box drops its number to origin
            // and absorbs the landing floor item (number-swap push). No blocking here.

            // ── Execute push ──────────────────────────────────────────────────
            ExecutePush(targetCell, targetPos, landingCell, landingPos);
            return true;
        }

        // ─── Private ──────────────────────────────────────────────────────────

        private void ExecutePush(
            GameGridCell pushedCell,  Vector2Int fromPos,
            GameGridCell landingCell, Vector2Int toPos)
        {
            // Track any content that should be dropped to the origin floor
            CellContent droppedToFloor = CellContent.Empty;

            // 1a. Empty box lands on floor item → absorb the floor item into box
            if (pushedCell.IsEmptyPushable && landingCell.HasFloorItem)
            {
                pushedCell.SetContent(landingCell.FloorItem);
                landingCell.ClearFloorItem();
            }
            // 1b. Valued box lands on floor item → swap:
            //     drop box number to origin floor, absorb landing floor item into box
            else if (!pushedCell.Content.IsEmpty && landingCell.HasFloorItem)
            {
                droppedToFloor = pushedCell.Content;
                pushedCell.SetContent(landingCell.FloorItem);
                landingCell.ClearFloorItem();
            }

            // 2. Move the cell data to the landing position
            var newCell = new GameGridCell(toPos, pushedCell.CellType, pushedCell.Content);
            newCell.View = pushedCell.View;
            _grid.SetCell(newCell);

            // 3. Replace the old position with an empty floor cell
            //    If a number was dropped (swap-push), leave it as a floor item here
            var emptyCell = new GameGridCell(fromPos, CellType.Empty);
            if (!droppedToFloor.IsEmpty) emptyCell.SetFloorItem(droppedToFloor);
            _grid.SetCell(emptyCell);

            KNLVN.GameDebug.Log($"[PushSystem] Pushed {pushedCell.CellType} from {fromPos} → {toPos}. Content={newCell.Content}" +
                      (droppedToFloor.IsEmpty ? "" : $" | Dropped {droppedToFloor} to floor at {fromPos}"));
        }
    }
}
