using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// Centers the main camera on the grid after a level is loaded.
    /// Uses orthographic size to fit the entire grid on screen.
    /// Attach to the Main Camera GameObject.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        // ─── Inspector ────────────────────────────────────────────────────────
        [SerializeField] private LevelManager      _levelManager;
        [SerializeField] private EventBusComponent _eventBus;

        /// <summary>Extra padding (in world units) around the grid.</summary>
        [SerializeField] private float _padding = 1f;

        private Camera _cam;

        // ─── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            _eventBus?.Subscribe<LevelLoadedEvent>(_ => FitToGrid());
            _eventBus?.Subscribe<LevelResetEvent>(_ => FitToGrid());
        }

        private void OnDisable()
        {
            _eventBus?.Unsubscribe<LevelLoadedEvent>(_ => FitToGrid());
            _eventBus?.Unsubscribe<LevelResetEvent>(_ => FitToGrid());
        }

        // ─── Fit logic ────────────────────────────────────────────────────────

        private void FitToGrid()
        {
            var grid = _levelManager?.Grid;
            if (grid == null) return;

            float cellSize = grid.GetCellSize();
            float gridW    = grid.Width  * cellSize;
            float gridH    = grid.Height * cellSize;

            // Centre of the grid in world space
            Vector3 center = new Vector3(gridW * 0.5f, gridH * 0.5f, transform.position.z);
            transform.position = center;

            // Orthographic size to fit the taller dimension with padding
            float aspect = _cam.aspect;
            float sizeByH = gridH * 0.5f + _padding;
            float sizeByW = (gridW * 0.5f + _padding) / aspect;
            _cam.orthographicSize = Mathf.Max(sizeByH, sizeByW);
        }
    }
}
