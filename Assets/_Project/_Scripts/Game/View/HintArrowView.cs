using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// Shows a small arrow indicator next to the player when they are:
    /// - Holding an item AND facing a Yellow/Star cell (valid placement target).
    ///
    /// The arrow is hidden in all other cases.
    /// Attach to any root GameObject; it manages its own visuals.
    /// </summary>
    public class HintArrowView : MonoBehaviour
    {
        // ─── Inspector ────────────────────────────────────────────────────────
        [SerializeField] private LevelManager      _levelManager;
        [SerializeField] private PlayerController  _player;
        [SerializeField] private EventBusComponent _eventBus;
        [SerializeField] private GameVisualConfig  _visualConfig;

        // ─── Runtime ─────────────────────────────────────────────────────────
        private SpriteRenderer _arrow;

        // ─── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            BuildArrow();
        }

        private void OnEnable()
        {
            _eventBus?.Subscribe<PlayerMovedEvent>(_ => Refresh());
            _eventBus?.Subscribe<PlayerHeldItemChangedEvent>(_ => Refresh());
            _eventBus?.Subscribe<LevelLoadedEvent>(_ => Refresh());
            _eventBus?.Subscribe<LevelResetEvent>(_ => Refresh());
            _eventBus?.Subscribe<UndoPerformedEvent>(_ => Refresh());
            _eventBus?.Subscribe<EquationChangedEvent>(_ => Refresh());
        }

        private void OnDisable()
        {
            _eventBus?.Unsubscribe<PlayerMovedEvent>(_ => Refresh());
            _eventBus?.Unsubscribe<PlayerHeldItemChangedEvent>(_ => Refresh());
            _eventBus?.Unsubscribe<LevelLoadedEvent>(_ => Refresh());
            _eventBus?.Unsubscribe<LevelResetEvent>(_ => Refresh());
            _eventBus?.Unsubscribe<UndoPerformedEvent>(_ => Refresh());
            _eventBus?.Unsubscribe<EquationChangedEvent>(_ => Refresh());
        }

        // ─── Core logic ───────────────────────────────────────────────────────

        private void Refresh()
        {
            if (_player == null || _levelManager?.Grid == null)
            {
                _arrow.enabled = false;
                return;
            }

            var grid = _levelManager.Grid;

            // Only show arrow when player holds something
            if (_player.HeldItem.IsEmpty)
            {
                _arrow.enabled = false;
                return;
            }

            // Check if facing cell is a Yellow or Star
            Vector2Int facingPos = _player.GridPos + _player.Facing.ToVector();
            var facingCell = grid.GetCell(facingPos);

            bool validTarget = facingCell != null && facingCell.IsPushable;
            _arrow.enabled = validTarget;

            if (validTarget)
            {
                // Position arrow between player and target cell
                Vector3 playerWorld  = grid.GetCenterWorldPosition(_player.GridPos.x, _player.GridPos.y);
                Vector3 facingWorld  = grid.GetCenterWorldPosition(facingPos.x, facingPos.y);

                transform.position = (playerWorld + facingWorld) * 0.5f + new Vector3(0, 0, -0.8f);

                // Rotate to point toward target
                Vector2 dir = (Vector2)(facingWorld - playerWorld);
                float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        // ─── Build visuals ────────────────────────────────────────────────────

        private void BuildArrow()
        {
            Color arrowColor = _visualConfig != null
                ? _visualConfig.HintArrowColor
                : new Color(1f, 0.9f, 0.2f, 0.9f);

            var go = new GameObject("ArrowSprite");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;

            _arrow = go.AddComponent<SpriteRenderer>();
            _arrow.sprite       = CreateArrowSprite();
            _arrow.color        = arrowColor;
            _arrow.sortingOrder = 15;
            _arrow.enabled      = false;
        }

        private static Sprite CreateArrowSprite()
        {
            int res = 32;
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            var pixels = new Color[res * res];

            // Draw a simple right-pointing arrow in pixel art style
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float nx = x / (float)(res - 1);  // 0→1
                float ny = y / (float)(res - 1) - 0.5f; // -0.5→0.5

                // Shaft: left 60% of width, centre 30% height
                bool inShaft = nx < 0.65f && Mathf.Abs(ny) < 0.15f;

                // Arrowhead: triangle in right 40%
                float tipProgress = (nx - 0.55f) / 0.45f; // 0→1 over tip region
                bool inHead = nx >= 0.55f && Mathf.Abs(ny) < (1f - tipProgress) * 0.5f;

                pixels[y * res + x] = (inShaft || inHead) ? Color.white : Color.clear;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0f, 0.5f), res);
        }
    }
}
