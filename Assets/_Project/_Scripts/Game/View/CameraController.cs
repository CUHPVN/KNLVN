using System.Collections;
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

        /// <summary>Duration of the smooth camera adjust animation when switching levels.</summary>
        [SerializeField] private float _adjustDuration = 0.4f;

        private Camera    _cam;
        private Coroutine _adjustCoroutine;
        private bool      _firstLoad = true; // snap on first load, animate afterwards

        // Cache handlers so Subscribe/Unsubscribe use the exact same delegate instance
        private System.Action<LevelLoadedEvent> _onLevelLoaded;
        private System.Action<LevelResetEvent>  _onLevelReset;

        // ─── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _cam = GetComponent<Camera>();

            _onLevelLoaded = _ => FitToGrid();
            _onLevelReset  = _ => FitToGrid();
        }

        private void OnEnable()
        {
            _eventBus?.Subscribe(_onLevelLoaded);
            _eventBus?.Subscribe(_onLevelReset);
        }

        private void OnDisable()
        {
            _eventBus?.Unsubscribe(_onLevelLoaded);
            _eventBus?.Unsubscribe(_onLevelReset);
        }

        // ─── Fit logic ────────────────────────────────────────────────────────

        private void FitToGrid()
        {
            var grid = _levelManager?.Grid;
            if (grid == null) return;

            float cellSize = grid.GetCellSize();
            float gridW    = grid.Width  * cellSize;
            float gridH    = grid.Height * cellSize;

            // Read the grid's actual world origin via GetWorldPosition(0,0)
            Vector3 origin = grid.GetWorldPosition(0, 0);

            // Centre of the grid in world space
            Vector3 targetPos = new Vector3(
                origin.x + gridW * 0.5f,
                origin.y + gridH * 0.5f,
                transform.position.z);

            // Orthographic size: fit the larger dimension + padding
            float aspect    = _cam.aspect;
            float sizeByH   = gridH * 0.5f + _padding;
            float sizeByW   = (gridW * 0.5f + _padding) / aspect;
            float targetSize = Mathf.Max(sizeByH, sizeByW);

            KNLVN.GameDebug.Log($"[Camera] Adjust to {grid.Width}x{grid.Height} grid. OrthoSize={targetSize:F2}");

            if (_adjustCoroutine != null) StopCoroutine(_adjustCoroutine);

            if (_firstLoad)
            {
                // Snap immediately on first load — no animation
                transform.position        = targetPos;
                _cam.orthographicSize     = targetSize;
                _firstLoad                = false;
            }
            else
            {
                _adjustCoroutine = StartCoroutine(AdjustRoutine(targetPos, targetSize));
            }
        }

        private IEnumerator AdjustRoutine(Vector3 targetPos, float targetSize)
        {
            Vector3 startPos  = transform.position;
            float   startSize = _cam.orthographicSize;
            float   elapsed   = 0f;

            while (elapsed < _adjustDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _adjustDuration);
                transform.position    = Vector3.Lerp(startPos, targetPos, t);
                _cam.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
                yield return null;
            }

            transform.position    = targetPos;
            _cam.orthographicSize = targetSize;
            _adjustCoroutine      = null;
        }
    }
}
