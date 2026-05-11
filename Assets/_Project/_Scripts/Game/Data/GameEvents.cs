namespace KNLVN.Game
{
    /// <summary>
    /// All game-wide events published through the EventBus.
    /// </summary>

    // ── Level ─────────────────────────────────────────────────────────────────

    public struct LevelLoadedEvent
    {
        public LevelData Data;
    }

    public struct LevelResetEvent { }

    // ── Equation ──────────────────────────────────────────────────────────────

    /// <summary>Fired every time the equation validity changes.</summary>
    public struct EquationChangedEvent
    {
        /// <summary>True when the equation is currently valid (door opens).</summary>
        public bool IsValid;
    }

    // ── Door ──────────────────────────────────────────────────────────────────

    public struct DoorOpenedEvent   { }
    public struct DoorClosedEvent   { }
    public struct DoorEnteredEvent  { }   // player stepped onto open red cell

    // ── Player ────────────────────────────────────────────────────────────────

    public struct PlayerMovedEvent
    {
        public UnityEngine.Vector2Int  NewPos;
        /// <summary>
        /// Set when the move involved a push. Null for normal moves.
        /// </summary>
        public UnityEngine.Vector2Int? PushedBoxFromPos;
        public UnityEngine.Vector2Int? PushedBoxToPos;
    }

    public struct PlayerHeldItemChangedEvent
    {
        public CellContent HeldItem;   // CellContent.Empty = nothing held
    }

    // ── Undo ──────────────────────────────────────────────────────────────────

    public struct UndoPerformedEvent
    {
        /// <summary>
        /// If the undone action involved a push, these hold where the box WAS
        /// (visual start = push toPos) and where it returned to (push fromPos).
        /// Null when no box was involved.
        /// </summary>
        public UnityEngine.Vector2Int? BoxWasAt;    // visual FROM (current pos before undo)
        public UnityEngine.Vector2Int? BoxNowAt;    // visual TO   (restored pos after undo)
    }
}
