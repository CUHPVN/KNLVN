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
            if (_eventBus == null)
                _eventBus = gameObject.AddComponent<EventBusComponent>();

            Application.targetFrameRate = 60;
        }

        private void OnEnable()
        {
            _eventBus?.Subscribe<DoorEnteredEvent>(OnDoorEntered);
            _eventBus?.Subscribe<AllLevelsCompleteEvent>(OnAllLevelsComplete);
        }

        private void OnDisable()
        {
            _eventBus?.Unsubscribe<DoorEnteredEvent>(OnDoorEntered);
            _eventBus?.Unsubscribe<AllLevelsCompleteEvent>(OnAllLevelsComplete);
        }

        private void OnDoorEntered(DoorEnteredEvent evt)
        {
            int cleared = _levelManager.CurrentLevelIndex;
            KNLVN.GameDebug.Log($"[GameManager] Level {cleared} complete! Loading next...");

            // Unlock next level in persistent progress
            LevelProgressManager.UnlockNextLevel(cleared, _levelManager.LevelCount);

            TransitionOverlay.Instance.PlayTransition(() => 
            {
                if (_levelManager != null)
                    _levelManager.LoadNextLevel();
            });
        }

        private void OnAllLevelsComplete(AllLevelsCompleteEvent evt)
        {
            KNLVN.GameDebug.Log($"[GameManager] All {evt.TotalLevels} levels complete! Show win screen.");
            // TODO: show win / credits screen
        }
    }
}
