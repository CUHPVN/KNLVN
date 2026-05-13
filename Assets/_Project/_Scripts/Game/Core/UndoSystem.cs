using System.Collections.Generic;
using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// Manages the undo history.
    /// A snapshot is pushed before every player action.
    /// Only one consecutive undo is allowed (must perform a new action before undoing again).
    /// </summary>
    public class UndoSystem
    {
        // ─── Snapshot record ──────────────────────────────────────────────────

        private struct Snapshot
        {
            public Vector2Int          PlayerPos;
            public FacingDirection     PlayerFacing;
            public CellContent         PlayerHeld;
            public List<GameGridCellSnapshot> GridSnap;
        }

        // ─── State ────────────────────────────────────────────────────────────

        private readonly Stack<Snapshot> _history = new Stack<Snapshot>();
        private bool _lastActionWasUndo = false;

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>Call before every player action to record the current state.</summary>
        public void Push(
            Vector2Int       playerPos,
            FacingDirection  playerFacing,
            CellContent      playerHeld,
            GameGrid         grid)
        {
            _history.Push(new Snapshot
            {
                PlayerPos    = playerPos,
                PlayerFacing = playerFacing,
                PlayerHeld   = playerHeld,
                GridSnap     = grid.TakeSnapshot()
            });
            _lastActionWasUndo = false;
        }

        /// <summary>
        /// Restores the previous state.
        /// Returns true if undo was performed, false if not allowed.
        /// </summary>
        public bool TryUndo(
            out Vector2Int      playerPos,
            out FacingDirection playerFacing,
            out CellContent     playerHeld,
            GameGrid            grid)
        {
            playerPos    = default;
            playerFacing = default;
            playerHeld   = CellContent.Empty;

            if (_lastActionWasUndo)
            {
                KNLVN.GameDebug.Log("[UndoSystem] Cannot undo twice in a row.");
                return false;
            }

            if (_history.Count == 0)
            {
                KNLVN.GameDebug.Log("[UndoSystem] Nothing to undo.");
                return false;
            }

            var snap = _history.Pop();
            playerPos    = snap.PlayerPos;
            playerFacing = snap.PlayerFacing;
            playerHeld   = snap.PlayerHeld;
            grid.RestoreSnapshot(snap.GridSnap);

            _lastActionWasUndo = true;
            return true;
        }

        /// <summary>Clears all history (used on level reset).</summary>
        public void Clear()
        {
            _history.Clear();
            _lastActionWasUndo = false;
        }

        public int Count => _history.Count;
    }
}
