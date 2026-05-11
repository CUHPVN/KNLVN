using UnityEngine;
using UnityEngine.UI;

namespace KNLVN.Game
{
    /// <summary>
    /// Simple HUD overlay (Canvas) showing:
    ///   - Held item token
    ///   - Equation valid/invalid indicator (door status)
    ///   - Controls reminder
    ///   - Win message when player enters door
    ///
    /// Attach to a Canvas GameObject and wire the EventBus in the Inspector.
    /// All UI elements are created procedurally — no prefab needed.
    /// </summary>
    public class HUDView : MonoBehaviour
    {
        // ─── Inspector ────────────────────────────────────────────────────────
        [SerializeField] private EventBusComponent _eventBus;

        // ─── Runtime UI refs ─────────────────────────────────────────────────
        private Text  _heldItemText;
        private Text  _doorStatusText;
        private Text  _winText;
        private Image _doorStatusIcon;

        // ─── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            BuildHUD();
        }

        private void OnEnable()
        {
            _eventBus?.Subscribe<PlayerHeldItemChangedEvent>(OnHeldItemChanged);
            _eventBus?.Subscribe<EquationChangedEvent>(OnEquationChanged);
            _eventBus?.Subscribe<DoorEnteredEvent>(_ => ShowWin());
            _eventBus?.Subscribe<LevelResetEvent>(_ => ResetHUD());
            _eventBus?.Subscribe<LevelLoadedEvent>(_ => ResetHUD());
        }

        private void OnDisable()
        {
            _eventBus?.Unsubscribe<PlayerHeldItemChangedEvent>(OnHeldItemChanged);
            _eventBus?.Unsubscribe<EquationChangedEvent>(OnEquationChanged);
            _eventBus?.Unsubscribe<DoorEnteredEvent>(_ => ShowWin());
            _eventBus?.Unsubscribe<LevelResetEvent>(_ => ResetHUD());
            _eventBus?.Unsubscribe<LevelLoadedEvent>(_ => ResetHUD());
        }

        // ─── Event handlers ───────────────────────────────────────────────────

        private void OnHeldItemChanged(PlayerHeldItemChangedEvent evt)
        {
            _heldItemText.text = evt.HeldItem.IsEmpty
                ? "Holding: —"
                : $"Holding: [ {evt.HeldItem.RawValue} ]";
        }

        private void OnEquationChanged(EquationChangedEvent evt)
        {
            if (evt.IsValid)
            {
                _doorStatusText.text  = "✓ Equation valid — door OPEN";
                _doorStatusText.color = new Color(0.1f, 0.85f, 0.1f);
                _doorStatusIcon.color = new Color(0.1f, 0.85f, 0.1f);
            }
            else
            {
                _doorStatusText.text  = "✗ Equation invalid";
                _doorStatusText.color = new Color(0.9f, 0.2f, 0.2f);
                _doorStatusIcon.color = new Color(0.9f, 0.2f, 0.2f);
            }
        }

        private void ShowWin()
        {
            _winText.gameObject.SetActive(true);
        }

        private void ResetHUD()
        {
            _winText.gameObject.SetActive(false);
            _heldItemText.text    = "Holding: —";
            _doorStatusText.text  = "✗ Equation invalid";
            _doorStatusText.color = new Color(0.9f, 0.2f, 0.2f);
            _doorStatusIcon.color = new Color(0.9f, 0.2f, 0.2f);
        }

        // ─── Procedural UI builder ────────────────────────────────────────────

        private void BuildHUD()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas             = gameObject.AddComponent<Canvas>();
                canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // ── Background panel (top-left) ──────────────────────────────────
            var panel = CreatePanel("InfoPanel",
                new Vector2(0, 1), new Vector2(0, 1),  // top-left anchor
                new Vector2(10, -10),                   // anchor position offset
                new Vector2(320, 110));

            // Door status icon (coloured square)
            _doorStatusIcon = CreateImage("DoorIcon", panel.transform,
                new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(16, -16), new Vector2(14, 14));
            _doorStatusIcon.color = new Color(0.9f, 0.2f, 0.2f);

            // Door status label
            _doorStatusText = CreateText("DoorStatus", panel.transform,
                new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(36, -8), new Vector2(280, 24),
                "✗ Equation invalid", 14, new Color(0.9f, 0.2f, 0.2f));

            // Held item label
            _heldItemText = CreateText("HeldItem", panel.transform,
                new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(8, -38), new Vector2(300, 24),
                "Holding: —", 14, Color.white);

            // Controls reminder
            CreateText("Controls", panel.transform,
                new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(8, -64), new Vector2(300, 44),
                "WASD/Arrows: Move   F: Interact\nZ: Undo   R: Reset", 11,
                new Color(0.8f, 0.8f, 0.8f));

            // ── Win message (centre screen, hidden by default) ───────────────
            var winGo = new GameObject("WinText");
            winGo.transform.SetParent(transform, false);
            var winRect = winGo.AddComponent<RectTransform>();
            winRect.anchorMin        = new Vector2(0.5f, 0.5f);
            winRect.anchorMax        = new Vector2(0.5f, 0.5f);
            winRect.anchoredPosition = Vector2.zero;
            winRect.sizeDelta        = new Vector2(500, 100);

            _winText = winGo.AddComponent<Text>();
            _winText.text      = "🎉 Level Complete! Press R to restart.";
            _winText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _winText.fontSize  = 32;
            _winText.fontStyle = FontStyle.Bold;
            _winText.color     = new Color(1f, 0.9f, 0.1f);
            _winText.alignment = TextAnchor.MiddleCenter;

            // Shadow
            var shadow = winGo.AddComponent<Shadow>();
            shadow.effectColor    = new Color(0, 0, 0, 0.8f);
            shadow.effectDistance = new Vector2(2, -2);

            winGo.SetActive(false);
        }

        // ─── UI factory helpers ───────────────────────────────────────────────

        private RectTransform CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax,
                                          Vector2 anchoredPos, Vector2 size)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(transform, false);

            var img        = go.AddComponent<Image>();
            img.color      = new Color(0, 0, 0, 0.55f);

            var rt               = go.GetComponent<RectTransform>();
            rt.anchorMin         = anchorMin;
            rt.anchorMax         = anchorMax;
            rt.anchoredPosition  = anchoredPos;
            rt.sizeDelta         = size;
            rt.pivot             = new Vector2(0, 1);
            return rt;
        }

        private Image CreateImage(string name, Transform parent,
                                  Vector2 anchorMin, Vector2 anchorMax,
                                  Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            var rt  = go.GetComponent<RectTransform>();
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;
            rt.pivot            = new Vector2(0, 1);
            return img;
        }

        private Text CreateText(string name, Transform parent,
                                Vector2 anchorMin, Vector2 anchorMax,
                                Vector2 anchoredPos, Vector2 size,
                                string content, int fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var txt        = go.AddComponent<Text>();
            txt.font       = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text       = content;
            txt.fontSize   = fontSize;
            txt.color      = color;
            txt.alignment  = TextAnchor.UpperLeft;

            var rt               = go.GetComponent<RectTransform>();
            rt.anchorMin         = anchorMin;
            rt.anchorMax         = anchorMax;
            rt.anchoredPosition  = anchoredPos;
            rt.sizeDelta         = size;
            rt.pivot             = new Vector2(0, 1);

            return txt;
        }
    }
}
