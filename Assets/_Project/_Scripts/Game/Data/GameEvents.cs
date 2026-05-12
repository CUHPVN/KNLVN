namespace KNLVN.Game
{
    /// <summary>
    /// All game-wide events published through the EventBus.
    /// </summary>

    // ── Level ─────────────────────────────────────────────────────────────────

    public struct LevelLoadedEvent
    {
        public LevelData Data;
        /// <summary>Zero-based index in the LevelManager playlist.</summary>
        public int LevelIndex;
    }

    public struct LevelResetEvent { }

    /// <summary>Fired when <see cref="LevelManager.LoadNextLevel"/> is called on the last level.</summary>
    public struct AllLevelsCompleteEvent
    {
        public int TotalLevels;
    }

    // ── Equation ──────────────────────────────────────────────────────────────

    /// <summary>Fired every time the equation validity changes.</summary>
    public struct EquationChangedEvent
    {
        /// <summary>True when the equation is currently valid (door opens).</summary>
        public bool IsValid;
    }

    /// <summary>
    /// Fired ONCE when the equation first becomes valid (not re-fired while it stays valid).
    /// Carries the equation string and the grid positions of every cell in the chain,
    /// so the view layer can play celebration animations.
    /// </summary>
    public struct EquationSolvedEvent
    {
        /// <summary>Human-readable equation, e.g. "2 + 3 = 5".</summary>
        public string EquationText;
        /// <summary>Grid positions of every cell in the solved chain.</summary>
        public System.Collections.Generic.List<UnityEngine.Vector2Int> ChainPositions;
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
