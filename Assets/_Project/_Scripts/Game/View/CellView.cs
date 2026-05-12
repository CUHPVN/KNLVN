using System.Collections;
using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// Renders the grid cell at position (_x, _y).
    /// Reads state fresh from the <see cref="GameGrid"/> on every <see cref="Refresh"/> call.
    /// This means it always reflects the current grid data regardless of pushes or swaps.
    ///
    /// Door open/close state is tracked locally via EventBus subscriptions.
    /// All other refreshes are driven externally by <see cref="GridView"/>.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CellView : MonoBehaviour
    {
        // ─── State ────────────────────────────────────────────────────────────
        private int              _x, _y;
        private GameGrid         _grid;
        private GameVisualConfig _cfg;
        private bool             _doorOpen;

        private SpriteRenderer _bgSprite;
        private SpriteRenderer _contentPanel;
        private TextMesh       _contentLabel;
        private SpriteRenderer _floorToken;
        private TextMesh       _floorLabel;
        private Vector3        _originalScale; // Cached to prevent "growth bug" on spam


        // ─── Push / Bounce animation ──────────────────────────────────────────
        // Duration and curve are driven by GameVisualConfig (assigned in Init).
        private Coroutine _pushCoroutine;
        private Coroutine _bounceCoroutine;

        // ─── Door event handlers (cached so we can unsubscribe on Destroy) ────
        private EventBusComponent              _bus;
        private System.Action<DoorOpenedEvent> _onDoorOpened;
        private System.Action<DoorClosedEvent> _onDoorClosed;


        // ─── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _bgSprite              = GetComponent<SpriteRenderer>();
            _bgSprite.sortingOrder = 0;
            // BuildContentPanel / BuildFloorToken are called in Init() once _cfg is set.
        }

        private void OnDisable()
        {
            if (_pushCoroutine   != null) { StopCoroutine(_pushCoroutine);   _pushCoroutine   = null; }
            if (_bounceCoroutine != null) { StopCoroutine(_bounceCoroutine); _bounceCoroutine = null; }
        }

        private void OnDestroy()
        {
            // IMPORTANT: unsubscribe door handlers so the EventBus does not
            // hold dead references after this cell is destroyed (e.g. level load).
            _bus?.Unsubscribe(_onDoorOpened);
            _bus?.Unsubscribe(_onDoorClosed);
        }

        // ─── Init ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Bind this view to a grid position.
        /// Only door events are subscribed here; all other refreshes come from GridView.
        /// </summary>
        public void Init(int x, int y, GameGrid grid, GameVisualConfig cfg,
                         EventBusComponent bus, Sprite contentPanelSprite, Sprite floorTokenSprite)
        {
            _x    = x;
            _y    = y;
            _grid = grid;
            _cfg  = cfg;   // must be set BEFORE Build helpers

            _originalScale = transform.localScale;

            BuildContentPanel();
            BuildFloorToken();

            // Override procedural sprites with shared pre-generated ones from GridView
            _contentPanel.sprite = contentPanelSprite;
            _floorToken.sprite   = floorTokenSprite;

            // Cache bus ref + handlers so we can unsubscribe on Destroy
            _bus          = bus;
            _onDoorOpened = _ => { _doorOpen = true;  Refresh(); };
            _onDoorClosed = _ => { _doorOpen = false; Refresh(); };
            bus?.Subscribe(_onDoorOpened);
            bus?.Subscribe(_onDoorClosed);

            Refresh();
        }

        // ─── Public refresh ───────────────────────────────────────────────────

        /// <summary>
        /// Re-read from grid and update all visual layers.
        /// Called by <see cref="GridView"/> after any game action.
        /// </summary>
        public void Refresh()
        {
            var cell = _grid?.GetCell(_x, _y);
            if (cell == null || _cfg == null) return;

            // Layer 0: background sprite + colour
            _bgSprite.sprite = _cfg.GetBgSprite(cell.CellType, _doorOpen);
            _bgSprite.color  = _cfg.GetBgColor(cell.CellType, _doorOpen);

            // Layer 1+2: content panel (number / operator)
            bool hasContent = !cell.Content.IsEmpty;
            _contentPanel.gameObject.SetActive(hasContent);
            if (hasContent)
            {
                _contentLabel.text          = cell.Content.RawValue;
                _contentLabel.characterSize = _cfg != null ? _cfg.ContentLabelCharSize : 0.154f;
            }

            // Layer 3+4: floor item token
            bool hasFloor = cell.HasFloorItem;
            _floorToken.gameObject.SetActive(hasFloor);
            if (hasFloor) _floorLabel.text = cell.FloorItem.RawValue;
        }

        // ─── Push animation ───────────────────────────────────────────────────

        /// <summary>
        /// Smoothly moves this cell view from <paramref name="from"/> to <paramref name="to"/>
        /// using the same animation curve as the player.
        /// Called by <see cref="GridView"/> when this cell was pushed.
        /// </summary>
        public void AnimatePush(Vector3 from, Vector3 to)
        {
            if (_pushCoroutine != null)
            {
                StopCoroutine(_pushCoroutine);
                transform.position = to; // Force to target position if interrupted
            }

            _pushCoroutine = StartCoroutine(PushRoutine(from, to));
        }

        private IEnumerator PushRoutine(Vector3 from, Vector3 to)
        {
            float elapsed  = 0f;
            float duration = _cfg != null ? _cfg.MoveDuration : 0.12f;
            var   curve    = _cfg?.MoveCurve ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
                transform.position = Vector3.LerpUnclamped(from, to, t);
                yield return null;
            }

            transform.position = to;
            _pushCoroutine = null;
        }

        // ─── Build helpers ────────────────────────────────────────────────────

        private void BuildContentPanel()
        {
            float scale    = _cfg != null ? _cfg.ContentPanelScale    : 0.62f;
            float charSize = _cfg != null ? _cfg.ContentLabelCharSize  : 0.154f;
            int   fontSize = _cfg != null ? _cfg.LabelFontSize         : 100;
            Color panelClr = _cfg != null ? _cfg.ContentPanelColor     : new Color(1f, 1f, 1f, 0.92f);
            Font  font     = _cfg?.LabelFont;

            var go = new GameObject("ContentPanel");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            go.transform.localScale    = new Vector3(scale, scale, 1f);

            _contentPanel              = go.AddComponent<SpriteRenderer>();
            _contentPanel.color        = panelClr;
            _contentPanel.sortingOrder = 1;

            var lbl = new GameObject("Label");
            lbl.transform.SetParent(go.transform, false);
            lbl.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            _contentLabel               = lbl.AddComponent<TextMesh>();
            if (font != null) _contentLabel.font = font;
            _contentLabel.characterSize = charSize;
            _contentLabel.fontSize      = fontSize;
            _contentLabel.anchor        = TextAnchor.MiddleCenter;
            _contentLabel.alignment     = TextAlignment.Center;
            _contentLabel.color         = Color.white;
            _contentLabel.fontStyle     = FontStyle.Bold;
            if (font != null) lbl.GetComponent<MeshRenderer>().material = font.material;
            lbl.GetComponent<MeshRenderer>().sortingOrder = 2;

            go.SetActive(false);
        }

        private void BuildFloorToken()
        {
            float scale    = _cfg != null ? _cfg.FloorTokenScale    : 0.32f;
            float charSize = _cfg != null ? _cfg.FloorLabelCharSize  : 0.168f;
            int   fontSize = _cfg != null ? _cfg.LabelFontSize       : 100;
            Color tokenClr = _cfg != null ? _cfg.FloorTokenColor     : new Color(0.95f, 0.75f, 0.20f);
            Font  font     = _cfg?.LabelFont;

            var go = new GameObject("FloorToken");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, -0.15f);
            go.transform.localScale    = new Vector3(scale, scale, 1f);

            _floorToken              = go.AddComponent<SpriteRenderer>();
            _floorToken.color        = tokenClr;
            _floorToken.sortingOrder = 3;

            var lbl = new GameObject("Label");
            lbl.transform.SetParent(go.transform, false);
            lbl.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            _floorLabel               = lbl.AddComponent<TextMesh>();
            if (font != null) _floorLabel.font = font;
            _floorLabel.characterSize = charSize;
            _floorLabel.fontSize      = fontSize;
            _floorLabel.anchor        = TextAnchor.MiddleCenter;
            _floorLabel.alignment     = TextAlignment.Center;
            _floorLabel.color         = Color.white;
            _floorLabel.fontStyle     = FontStyle.Bold;
            if (font != null) lbl.GetComponent<MeshRenderer>().material = font.material;
            lbl.GetComponent<MeshRenderer>().sortingOrder = 4;

            go.SetActive(false);
        }

        // ─── Bounce animation (equation solved) ───────────────────────────────

        /// <summary>
        /// Plays a pop/bounce scale animation on this cell.
        /// Called by GridView when EquationSolvedEvent is received.
        /// </summary>
        public void PlayBounce(float delay = 0f)
        {
            // IMPORTANT: If a previous bounce was running, stop it AND 
            // force scale back to original immediately before starting the next one.
            if (_bounceCoroutine != null)
            {
                StopCoroutine(_bounceCoroutine);
                transform.localScale = _originalScale;
            }
            _bounceCoroutine = StartCoroutine(BounceRoutine(delay));
        }

        private IEnumerator BounceRoutine(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            Vector3 startScale = _originalScale;
            const float duration = 0.4f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t     = elapsed / duration;
                // sin curve: peaks at t=0.5, returns to 1 at t=1
                float scale = 1f + 0.35f * Mathf.Sin(t * Mathf.PI);
                transform.localScale = startScale * scale;
                yield return null;
            }

            transform.localScale = startScale;
            _bounceCoroutine     = null;
        }
    }
}
