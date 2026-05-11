using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// Handles the F-key interaction: pickup from floor/yellow, place, and swap.
    /// Works on the <see cref="GameGrid"/> directly; the caller owns the player's
    /// held item state.
    /// </summary>
    public class InteractionSystem
    {
        private readonly GameGrid _grid;

        public InteractionSystem(GameGrid grid) => _grid = grid;

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Attempt an F-key interaction.
        /// </summary>
        /// <param name="playerPos">Player's current grid position.</param>
        /// <param name="facing">Direction the player is facing.</param>
        /// <param name="heldItem">Current held item (mutated by this method).</param>
        /// <returns>True if any state change occurred.</returns>
        public bool TryInteract(Vector2Int playerPos, FacingDirection facing, ref CellContent heldItem)
        {
            var playerCell = _grid.GetCell(playerPos);
            var facingPos  = playerPos + facing.ToVector();
            var facingCell = _grid.GetCell(facingPos);

            // ── Priority 1: pick up from floor the player is standing on ──────
            if (playerCell != null && playerCell.HasFloorItem && heldItem.IsEmpty)
            {
                heldItem = playerCell.FloorItem;
                playerCell.ClearFloorItem();
                Debug.Log($"[Interaction] Picked up floor item: {heldItem} at {playerPos}");
                return true;
            }

            // ── Priority 2: interact with facing Yellow/Star cell ─────────────
            if (facingCell != null && facingCell.IsPushable)
            {
                return InteractWithPushableCell(facingCell, ref heldItem);
            }

            // ── Priority 3: pick up floor item from the facing cell ───────────
            if (facingCell != null && facingCell.IsEmpty && facingCell.HasFloorItem && heldItem.IsEmpty)
            {
                heldItem = facingCell.FloorItem;
                facingCell.ClearFloorItem();
                Debug.Log($"[Interaction] Picked up floor item: {heldItem} from {facingPos}");
                return true;
            }

            Debug.Log("[Interaction] No interaction target.");
            return false;
        }

        // ─── Private helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Interact with a Yellow or Star cell:
        /// - If empty cell + holding item → place
        /// - If cell has value + holding item → swap
        /// - If not holding anything → no-op (hint is shown by view, not here)
        /// </summary>
        private bool InteractWithPushableCell(GameGridCell cell, ref CellContent heldItem)
        {
            if (heldItem.IsEmpty)
            {
                // No item in hand — pick from Yellow if it has content
                if (!cell.Content.IsEmpty)
                {
                    heldItem = cell.Content;
                    cell.ClearContent();
                    Debug.Log($"[Interaction] Picked from Yellow/Star at {cell.GridPos}: {heldItem}");
                    return true;
                }
                Debug.Log("[Interaction] Facing empty Yellow/Star with empty hands — no-op.");
                return false;
            }

            if (cell.Content.IsEmpty)
            {
                // Place held item onto empty yellow/star
                cell.SetContent(heldItem);
                heldItem = CellContent.Empty;
                Debug.Log($"[Interaction] Placed {cell.Content} onto Yellow/Star at {cell.GridPos}");
                return true;
            }
            else
            {
                // Swap
                var temp = cell.Content;
                cell.SetContent(heldItem);
                heldItem = temp;
                Debug.Log($"[Interaction] Swapped: now holding {heldItem}, cell has {cell.Content} at {cell.GridPos}");
                return true;
            }
        }
    }
}
