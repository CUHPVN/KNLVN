using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// Top-level game orchestrator.
    /// Holds shared dependencies (EventBus) and kicks off the level on Start.
    /// Add to the root "GameManager" GameObject in the scene.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        // ─── Shared dependencies ──────────────────────────────────────────────
        [SerializeField] private EventBusComponent _eventBus;
        [SerializeField] private LevelManager       _levelManager;

        // ─── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            // EventBus must exist before any subscriber Awake runs.
            // Add EventBus as a MonoBehaviour component and assign it here,
            // or create it programmatically:
            if (_eventBus == null)
                _eventBus = gameObject.AddComponent<EventBusComponent>();

            Application.targetFrameRate = 60;
        }

        private void OnEnable()
        {
            _eventBus?.Subscribe<DoorEnteredEvent>(OnDoorEntered);
        }

        private void OnDisable()
        {
            _eventBus?.Unsubscribe<DoorEnteredEvent>(OnDoorEntered);
        }

        private void OnDoorEntered(DoorEnteredEvent evt)
        {
            Debug.Log("[GameManager] Player entered door — level complete!");
            // TODO: show win screen / load next level
        }
    }
}
