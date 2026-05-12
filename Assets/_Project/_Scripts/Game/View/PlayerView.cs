using System.Collections;
using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// Renders the player and animates movement using AnimationCurve + Coroutine.
    ///
    /// ── UniTask migration guide ────────────────────────────────────────────────
    /// When UniTask is added to the project:
    ///   1. Add:  using Cysharp.Threading.Tasks;
    ///   2. Add:  private CancellationTokenSource _moveCts;
    ///   3. Replace <see cref="AnimateMove"/> + <see cref="MoveRoutine"/> with an
    ///      async UniTask version (see class-level comment in original file).
    ///   4. In OnDisable: _moveCts?.Cancel(); _moveCts?.Dispose();
    /// ──────────────────────────────────────────────────────────────────────────
    /// </summary>
    public class PlayerView : MonoBehaviour
    {
        // ─── Inspector ────────────────────────────────────────────────────────
        [SerializeField] private LevelManager      _levelManager;
        [SerializeField] private PlayerController  _player;
        [SerializeField] private EventBusComponent _eventBus;
        [SerializeField] private GameVisualConfig  _visualConfig;

        // Move animation is driven by GameVisualConfig (MoveDuration / MoveCurve).

        // ─── Runtime refs ─────────────────────────────────────────────────────
        private SpriteRenderer _sprite;
        private TextMesh       _heldLabel;
        private GameObject     _bubble;

        // ─── Facing-cell marker (sibling, not child — doesn't move with player) ─
        private GameObject     _facingMarkerGo;
        private SpriteRenderer _facingMarkerSr;

        // ─── Animation state ─────────────────────────────────────────────────
        private Coroutine _moveCoroutine;
        private Coroutine _walkCoroutine;
        private Coroutine _popupCoroutine;

        // ─── Cached event handlers ────────────────────────────────────────────
        private System.Action<LevelLoadedEvent>         _onLevelLoaded;
        private System.Action<LevelResetEvent>          _onLevelReset;
        private System.Action<UndoPerformedEvent>       _onUndoPerformed;
        private System.Action<EquationSolvedEvent>      _onEquationSolved;

        // ─── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            BuildVisuals();

            _onLevelLoaded   = _ => SnapToPlayer(useStartPos: true);
            _onLevelReset    = _ => SnapToPlayer(useStartPos: true);

            // FIX: undo animates back (like a normal move) instead of snapping.
            _onUndoPerformed = _ =>
            {
                var grid = _levelManager?.Grid;
                if (grid == null || _player == null) return;
                Vector3 target = GridToWorld(grid, _player.GridPos);
                AnimateMove(transform.position, target);
                RefreshFacingSprite();
                UpdateFacingMarker();
            };

            _onEquationSolved = evt => ShowEquationPopup(evt.EquationText);
        }

        private void OnEnable()
        {
            _eventBus?.Subscribe(_onLevelLoaded);
            _eventBus?.Subscribe(_onLevelReset);
            _eventBus?.Subscribe<PlayerMovedEvent>(OnPlayerMoved);
            _eventBus?.Subscribe<PlayerHeldItemChangedEvent>(OnHeldItemChanged);
            _eventBus?.Subscribe(_onUndoPerformed);
            _eventBus?.Subscribe(_onEquationSolved);
        }

        private void OnDisable()
        {
            _eventBus?.Unsubscribe(_onLevelLoaded);
            _eventBus?.Unsubscribe(_onLevelReset);
            _eventBus?.Unsubscribe<PlayerMovedEvent>(OnPlayerMoved);
            _eventBus?.Unsubscribe<PlayerHeldItemChangedEvent>(OnHeldItemChanged);
            _eventBus?.Unsubscribe(_onUndoPerformed);
            _eventBus?.Unsubscribe(_onEquationSolved);

            if (_moveCoroutine  != null) { StopCoroutine(_moveCoroutine);  _moveCoroutine  = null; }
            if (_walkCoroutine  != null) { StopCoroutine(_walkCoroutine);  _walkCoroutine  = null; }
            if (_popupCoroutine != null) { StopCoroutine(_popupCoroutine); _popupCoroutine = null; }
        }

        // ─── Event handlers ───────────────────────────────────────────────────

        private void OnPlayerMoved(PlayerMovedEvent evt)
        {
            var grid = _levelManager?.Grid;
            if (grid == null) return;

            Vector3 target = GridToWorld(grid, evt.NewPos);
            AnimateMove(transform.position, target);
            PlayWalkAnimation(_player.Facing);
            UpdateFacingMarker();
        }

        private void OnHeldItemChanged(PlayerHeldItemChangedEvent evt)
        {
            bool hasItem = !evt.HeldItem.IsEmpty;
            _bubble.SetActive(hasItem);
            if (hasItem) _heldLabel.text = evt.HeldItem.RawValue;
        }

        // ─── Snap ─────────────────────────────────────────────────────────────

        /// <param name="useStartPos">
        /// True → read PlayerStartPos from LevelData (level load / reset).
        /// False → read _player.GridPos directly (undo).
        /// </param>
        private void SnapToPlayer(bool useStartPos)
        {
            if (_levelManager?.Grid == null) return;

            if (_moveCoroutine != null) { StopCoroutine(_moveCoroutine); _moveCoroutine = null; }

            var grid = _levelManager.Grid;

            Vector2Int snapPos = useStartPos && _levelManager.CurrentLevel != null
                ? _levelManager.CurrentLevel.PlayerStartPos
                : (_player != null ? _player.GridPos : Vector2Int.zero);

            transform.position = GridToWorld(grid, snapPos);

            bool hasItem = _player != null && !_player.HeldItem.IsEmpty;
            _bubble.SetActive(hasItem);
            if (hasItem) _heldLabel.text = _player.HeldItem.RawValue;

            RefreshFacingSprite();
            UpdateFacingMarker();
        }

        // ─── Walk animation ───────────────────────────────────────────────────

        /// <summary>
        /// Plays the walk-cycle for <paramref name="facing"/>.
        /// If no frames are configured, instantly shows the idle directional sprite.
        /// Runs concurrently with the position MoveRoutine.
        /// </summary>
        private void PlayWalkAnimation(FacingDirection facing)
        {
            if (_walkCoroutine != null) StopCoroutine(_walkCoroutine);

            var frames = _visualConfig != null ? _visualConfig.GetWalkFrames(facing) : null;

            if (frames == null || frames.Length == 0)
            {
                // No walk frames — just snap to idle directional sprite
                RefreshFacingSprite();
                return;
            }

            _walkCoroutine = StartCoroutine(WalkRoutine(frames, facing));
        }

        private IEnumerator WalkRoutine(Sprite[] frames, FacingDirection facing)
        {
            float frameDur = _visualConfig != null ? _visualConfig.WalkFrameDuration : 0.06f;
            float moveDur  = _visualConfig != null ? _visualConfig.MoveDuration      : 0.12f;
            float elapsed  = 0f;
            int   idx      = 0;

            while (elapsed < moveDur)
            {
                _sprite.sprite = frames[idx % frames.Length];
                yield return new WaitForSeconds(frameDur);
                elapsed += frameDur;
                idx++;
            }

            // Return to idle directional sprite when move finishes
            if (_sprite != null && _visualConfig != null)
                _sprite.sprite = _visualConfig.GetPlayerSprite(facing);

            _walkCoroutine = null;
        }

        // ─── Facing sprite ────────────────────────────────────────────────────

        /// <summary>
        /// Swaps the player body sprite to match the current facing direction.
        /// Called after every move, snap, and undo.
        /// </summary>
        private void RefreshFacingSprite()
        {
            if (_sprite == null || _visualConfig == null || _player == null) return;
            _sprite.sprite = _visualConfig.GetPlayerSprite(_player.Facing);
        }

        // ─── Equation popup ───────────────────────────────────────────────────

        /// <summary>
        /// Spawns a floating text label above the player showing the solved equation.
        /// Uses the SimplePool system to handle rapid spawning (spamming).
        /// </summary>
        private void ShowEquationPopup(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            Vector3 spawnPos = transform.position + new Vector3(0f, 1.2f, -0.5f);
            EquationPopup popup = SimplePool.Spawn<EquationPopup>(PoolType.EquationPopup, spawnPos, Quaternion.identity);
            
            if (popup != null)
            {
                popup.Setup(text, _visualConfig?.LabelFont, _visualConfig != null ? _visualConfig.LabelFontSize : 100);
            }
        }

        // ─── Facing-cell marker ───────────────────────────────────────────────

        private void UpdateFacingMarker()
        {
            if (_facingMarkerGo == null) return;
            var grid = _levelManager?.Grid;
            if (grid == null || _player == null) { _facingMarkerGo.SetActive(false); return; }

            Vector2Int facingPos  = _player.GridPos + _player.Facing.ToVector();
            var        facingCell = grid.GetCell(facingPos);

            if (facingCell == null) { _facingMarkerGo.SetActive(false); return; }

            // Hide marker on cells that can never be interacted with
            bool isInteractable = facingCell.CellType != CellType.Wall
                               && facingCell.CellType != CellType.Empty;

            if (!isInteractable) { _facingMarkerGo.SetActive(false); return; }

            _facingMarkerGo.SetActive(true);
            float cellSize = grid.GetCellSize();
            _facingMarkerGo.transform.position   = grid.GetCenterWorldPosition(facingPos.x, facingPos.y)
                                                   + new Vector3(0f, 0f, -0.4f);
            _facingMarkerGo.transform.localScale = Vector3.one * cellSize;
        }

        // ─── Animation ────────────────────────────────────────────────────────

        private void AnimateMove(Vector3 from, Vector3 to)
        {
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(MoveRoutine(from, to));
        }

        private IEnumerator MoveRoutine(Vector3 from, Vector3 to)
        {
            float elapsed  = 0f;
            float duration = _visualConfig != null ? _visualConfig.MoveDuration : 0.12f;
            var   curve    = _visualConfig?.MoveCurve ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
                transform.position = Vector3.LerpUnclamped(from, to, t);
                yield return null;
            }
            transform.position = to;
            _moveCoroutine = null;
        }

        // ─── Utility ──────────────────────────────────────────────────────────

        private static Vector3 GridToWorld(GameGrid grid, Vector2Int pos)
            => grid.GetCenterWorldPosition(pos.x, pos.y) + new Vector3(0, 0, -0.5f);

        // ─── Build visuals ────────────────────────────────────────────────────

        private void BuildVisuals()
        {
            var cfg = _visualConfig;

            // Read layout values from config (fall back to safe defaults if SO not assigned)
            float bodyScale    = cfg != null ? cfg.PlayerBodyScale      : 0.8f;
            float bubbleY      = cfg != null ? cfg.BubbleOffsetY        : 0.75f;
            float bubbleScale  = cfg != null ? cfg.BubbleScale          : 0.45f;
            float heldCharSize = cfg != null ? cfg.HeldLabelCharSize    : 0.216f;
            int   fontSize     = cfg != null ? cfg.LabelFontSize        : 100;
            Font  font         = cfg?.LabelFont;
            Color markerColor  = cfg != null ? cfg.FacingMarkerColor    : new Color(1.00f, 0.90f, 0.30f, 0.75f);

            // ── Player body ───────────────────────────────────────────────────
            var bodyGo = new GameObject("PlayerBody");
            bodyGo.transform.SetParent(transform, false);
            bodyGo.transform.localScale = new Vector3(bodyScale, bodyScale, 1f);

            _sprite              = bodyGo.AddComponent<SpriteRenderer>();
            _sprite.sprite       = cfg != null ? cfg.GetPlayerSprite() : SpriteFactory.CreateCircle(64);
            _sprite.color        = cfg != null ? cfg.PlayerColor : new Color(0.18f, 0.72f, 0.90f);
            _sprite.sortingOrder = 10;

            // ── Held-item badge (above player head) ───────────────────────────
            _bubble = new GameObject("HeldBubble");
            _bubble.transform.SetParent(transform, false);
            _bubble.transform.localPosition = new Vector3(0f, bubbleY, -0.2f);
            _bubble.transform.localScale    = new Vector3(bubbleScale, bubbleScale, 1f);

            var bubbleSr          = _bubble.AddComponent<SpriteRenderer>();
            bubbleSr.sprite       = cfg != null ? cfg.GetHeldBubbleSprite() : SpriteFactory.CreateCircle(64);
            bubbleSr.color        = cfg != null ? cfg.GetHeldBubbleColor()  : new Color(0.20f, 0.80f, 1.00f);
            bubbleSr.sortingOrder = 12;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(_bubble.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0f, -0.1f);

            _heldLabel               = labelGo.AddComponent<TextMesh>();
            if (font != null) _heldLabel.font = font;
            _heldLabel.characterSize = heldCharSize;
            _heldLabel.fontSize      = fontSize;
            _heldLabel.anchor        = TextAnchor.MiddleCenter;
            _heldLabel.alignment     = TextAlignment.Center;
            _heldLabel.color         = Color.white;
            _heldLabel.fontStyle     = FontStyle.Bold;
            if (font != null) labelGo.GetComponent<MeshRenderer>().material = font.material;
            labelGo.GetComponent<MeshRenderer>().sortingOrder = 13;

            _bubble.SetActive(false);

            // ── Facing-cell marker (sibling so it doesn't move with the player) ─
            var markerGo = new GameObject("FacingMarker");
            markerGo.transform.SetParent(transform.parent, false);  // sibling!

            _facingMarkerSr              = markerGo.AddComponent<SpriteRenderer>();
            _facingMarkerSr.sprite       = cfg != null ? cfg.GetFacingMarkerSprite() : SpriteFactory.CreateTargetMarker();
            _facingMarkerSr.color        = markerColor;
            _facingMarkerSr.sortingOrder = 8;

            _facingMarkerGo = markerGo;
            _facingMarkerGo.SetActive(false);
        }

        // ─── Procedural sprites ───────────────────────────────────────────────
        // (kept for completeness — SpriteFactory is preferred)

        private static Sprite CreateCircleSprite(int res)
        {
            var tex    = new Texture2D(res, res, TextureFormat.RGBA32, false);
            float half = res / 2f;
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dx = (x + 0.5f) - half, dy = (y + 0.5f) - half;
                float a  = (dx * dx + dy * dy) <= (half - 0.5f) * (half - 0.5f) ? 1f : 0f;
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
        }
    }
}
