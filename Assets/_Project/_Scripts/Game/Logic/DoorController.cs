using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// Tracks the open/locked state of the exit door (Red cell).
    /// Responds to equation validity changes and exposes the state to other systems.
    /// </summary>
    public class DoorController
    {
        // ─── State ────────────────────────────────────────────────────────────
        public bool IsOpen { get; private set; } = false;

        private readonly EventBusComponent _eventBus;

        public DoorController(EventBusComponent eventBus) => _eventBus = eventBus;

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>Called by GameManager after every equation evaluation.</summary>
        public void ApplyEquationResult(bool equationValid)
        {
            if (equationValid && !IsOpen)
            {
                Open();
            }
            else if (!equationValid && IsOpen)
            {
                Close();
            }
            // No change if state already matches
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            _eventBus?.Publish(new DoorOpenedEvent());
            Debug.Log("[DoorController] Door OPENED.");
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            _eventBus?.Publish(new DoorClosedEvent());
            Debug.Log("[DoorController] Door CLOSED.");
        }
    }
}
