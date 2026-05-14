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
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class HUDView : MonoBehaviour
    {
        // ─── Inspector ────────────────────────────────────────────────────────
        [SerializeField] private EventBusComponent _eventBus;
        
        [Header("Sprites (Optional)")]
        [SerializeField] private Sprite _resetButtonIcon;
        [SerializeField] private Sprite _mainMenuButtonIcon;
        [SerializeField] private Sprite _buttonBgSprite;
        [SerializeField] private Sprite _panelBgSprite;

        [Header("UI Settings")]
        [Tooltip("Thay đổi thông số này để làm UI to/nhỏ tùy ý")]
        [SerializeField] [Range(0.5f, 3f)] private float _uiScale = 1.5f;

        [Header("Colors")]
        [SerializeField] private Color _colorValid = new Color(0.1f, 0.85f, 0.1f);
        [SerializeField] private Color _colorInvalid = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color _colorControls = new Color(0.8f, 0.8f, 0.8f);
        [SerializeField] private Color _colorPanelBg = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField] private Color _colorButtonBg = new Color(0.3f, 0.3f, 0.3f, 0.9f);

        [Header("Text Settings")]
        [SerializeField] private Font _customFont;

        [Header("Text Content")]
        [SerializeField] private string _textDoorValid = "✓ Equation valid — door OPEN";
        [SerializeField] private string _textDoorInvalid = "✗ Equation invalid";
        [SerializeField] private string _textHoldingEmpty = "Holding: —";
        [SerializeField] private string _textHoldingFormat = "Holding: [ {0} ]";
        [SerializeField] [TextArea] private string _textControls = "WASD/Arrows: Move   F: Interact\nZ: Undo   R: Reset";
        [SerializeField] private string _textResetBtn = "";
        [SerializeField] private string _textMenuBtn = "";

        // ─── Runtime UI refs ─────────────────────────────────────────────────
        private Text  _heldItemText;
        private Text  _doorStatusText;

        // ─── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            BuildHUD();
        }

        private void OnEnable()
        {
            _eventBus?.Subscribe<PlayerHeldItemChangedEvent>(OnHeldItemChanged);
            _eventBus?.Subscribe<EquationChangedEvent>(OnEquationChanged);
            _eventBus?.Subscribe<LevelResetEvent>(_ => ResetHUD());
            _eventBus?.Subscribe<LevelLoadedEvent>(_ => ResetHUD());
        }

        private void OnDisable()
        {
            _eventBus?.Unsubscribe<PlayerHeldItemChangedEvent>(OnHeldItemChanged);
            _eventBus?.Unsubscribe<EquationChangedEvent>(OnEquationChanged);
            _eventBus?.Unsubscribe<LevelResetEvent>(_ => ResetHUD());
            _eventBus?.Unsubscribe<LevelLoadedEvent>(_ => ResetHUD());
        }

        // ─── Event handlers ───────────────────────────────────────────────────

        private void OnHeldItemChanged(PlayerHeldItemChangedEvent evt)
        {
            _heldItemText.text = evt.HeldItem.IsEmpty
                ? _textHoldingEmpty
                : string.Format(_textHoldingFormat, evt.HeldItem.RawValue);
        }

        private void OnEquationChanged(EquationChangedEvent evt)
        {
            if (evt.IsValid)
            {
                _doorStatusText.text  = _textDoorValid;
                _doorStatusText.color = _colorValid;
            }
            else
            {
                _doorStatusText.text  = _textDoorInvalid;
                _doorStatusText.color = _colorInvalid;
            }
        }

        private void ResetHUD()
        {
            _heldItemText.text    = _textHoldingEmpty;
            _doorStatusText.text  = _textDoorInvalid;
            _doorStatusText.color = _colorInvalid;
        }

        // ─── Procedural UI builder ────────────────────────────────────────────

        private void BuildHUD()
        {
            // Components guaranteed by [RequireComponent] — just configure
            var canvas = GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = _uiScale;

            // ── Background panel (top-left) ──────────────────────────────────
            var panel = CreatePanel("InfoPanel",
                new Vector2(0, 1), new Vector2(0, 1),  // top-left anchor
                new Vector2(10, -10),                   // anchor position offset
                new Vector2(200, 140));

            // Door status label
            _doorStatusText = CreateText("DoorStatus", panel.transform,
                new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(10, -10), new Vector2(180, 40),
                _textDoorInvalid, 14, _colorInvalid);

            // Held item label
            _heldItemText = CreateText("HeldItem", panel.transform,
                new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(10, -50), new Vector2(180, 24),
                _textHoldingEmpty, 14, Color.white);

            // Controls reminder
            CreateText("Controls", panel.transform,
                new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(10, -80), new Vector2(180, 50),
                _textControls, 11, _colorControls);

            // ── Buttons panel (top-right) ──────────────────────────────────
            var buttonsPanel = CreatePanel("ButtonsPanel",
                new Vector2(1, 1), new Vector2(1, 1),  // top-right anchor
                new Vector2(-10, -10),                 // anchor position offset
                new Vector2(100, 60));
            buttonsPanel.pivot = new Vector2(1, 1);

            CreateButton("ResetButton", buttonsPanel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), // center of panel
                new Vector2(-25, 0), new Vector2(40, 40),
                _textResetBtn, _resetButtonIcon, () => TransitionOverlay.Instance.PlayTransition(() => LevelManager.Instance.ResetLevel()));

            CreateButton("MainMenuButton", buttonsPanel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(25, 0), new Vector2(40, 40),
                _textMenuBtn, _mainMenuButtonIcon, () => TransitionOverlay.Instance.FadeToBlack(() => UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu")));
        }

        // ─── UI factory helpers ───────────────────────────────────────────────

        private RectTransform CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax,
                                          Vector2 anchoredPos, Vector2 size)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(transform, false);

            var img        = go.AddComponent<Image>();
            if (_panelBgSprite != null)
            {
                img.sprite = _panelBgSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else
            {
                img.color = _colorPanelBg;
            }

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

        private Button CreateButton(string name, Transform parent,
                                    Vector2 anchorMin, Vector2 anchorMax,
                                    Vector2 anchoredPos, Vector2 size,
                                    string label, Sprite icon, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            if (_buttonBgSprite != null)
            {
                img.sprite = _buttonBgSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else
            {
                img.color = _colorButtonBg;
            }

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            rt.pivot = new Vector2(0.5f, 0.5f);

            bool hasText = !string.IsNullOrEmpty(label);

            // Icon
            if (icon != null)
            {
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(go.transform, false);
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = icon;
                iconImg.preserveAspect = true;
                
                var iconRt = iconGo.GetComponent<RectTransform>();
                if (hasText)
                {
                    iconRt.anchorMin = new Vector2(0, 0.5f);
                    iconRt.anchorMax = new Vector2(0, 0.5f);
                    iconRt.anchoredPosition = new Vector2(15, 0);
                    iconRt.sizeDelta = new Vector2(20, 20);
                }
                else
                {
                    iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                    iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                    iconRt.anchoredPosition = Vector2.zero;
                    iconRt.sizeDelta = new Vector2(24, 24); // bigger if no text
                }
                iconRt.pivot = new Vector2(0.5f, 0.5f);
            }

            // Text
            if (hasText)
            {
                var txtGo = new GameObject("Text");
                txtGo.transform.SetParent(go.transform, false);
                
                var txt = txtGo.AddComponent<Text>();
                txt.font = _customFont != null ? _customFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.text = label;
                txt.fontSize = 14;
                txt.color = Color.white;
                txt.alignment = TextAnchor.MiddleCenter;

                var txtRt = txtGo.GetComponent<RectTransform>();
                if (icon != null)
                {
                    txtRt.anchorMin = Vector2.zero;
                    txtRt.anchorMax = Vector2.one;
                    txtRt.anchoredPosition = new Vector2(12, 0); // shift right
                    txtRt.sizeDelta = new Vector2(-24, 0);
                }
                else
                {
                    txtRt.anchorMin = Vector2.zero;
                    txtRt.anchorMax = Vector2.one;
                    txtRt.anchoredPosition = Vector2.zero;
                    txtRt.sizeDelta = Vector2.zero;
                }
            }

            return btn;
        }

        private Text CreateText(string name, Transform parent,
                                Vector2 anchorMin, Vector2 anchorMax,
                                Vector2 anchoredPos, Vector2 size,
                                string content, int fontSize, Color color,
                                TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var txt        = go.AddComponent<Text>();
            txt.font       = _customFont != null ? _customFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text       = content;
            txt.fontSize   = fontSize;
            txt.color      = color;
            txt.alignment  = alignment;

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
