using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

namespace KNLVN.Game
{
    /// <summary>
    /// Procedural Main Menu View.
    /// Level list is dynamic; locks/unlocks driven by LevelProgressManager (PlayerPrefs).
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class MainMenuView : MonoBehaviour
    {
        // ─── Inspector ────────────────────────────────────────────────────────
        [Header("UI Settings")]
        [Tooltip("Thay đổi thông số này để làm UI to/nhỏ tùy ý")]
        [SerializeField] [Range(0.5f, 3f)] private float _uiScale = 1.5f;
        [SerializeField] private Font _customFont;
        [SerializeField] private Sprite _buttonBgSprite;       // Play / Back / Select Level buttons
        [SerializeField] private Sprite _levelButtonSprite;    // Unlocked level buttons
        [SerializeField] private Sprite _levelButtonLockedSprite; // Locked level buttons
        [SerializeField] private Sprite _panelBgSprite;

        [Header("Title Settings")]
        [SerializeField] private string _titleText = "MATH SOKOBAN";
        [SerializeField] private Color _titleColor = new Color(0.9f, 0.8f, 0.2f);
        [SerializeField] private int _titleFontSize = 48;

        [Header("Colors")]
        [SerializeField] private Color _colorButtonBg   = new Color(0.25f, 0.25f, 0.25f, 1f);
        [SerializeField] private Color _colorButtonText  = Color.white;
        [SerializeField] private Color _colorLockedBg    = new Color(0.15f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color _colorLockedText  = new Color(0.4f, 0.4f, 0.4f, 1f);

        [Header("Level Grid Layout")]
        [Tooltip("Kéo thả các LevelData SO vào đây (giống thứ tự trong LevelManager). Số lượng sẽ tự đếm.")]
        [SerializeField] private LevelData[] _levels;
        [Tooltip("Khoảng cách ngang giữa các nút level (px)")]
        [SerializeField] private float _spacingX = 80f;
        [Tooltip("Margin thu hai bên của vùng scroll (px mỗi bên)")]
        [SerializeField] private float _scrollSideMargin = 60f;

        // Derived at runtime from _levels.Length (or fallback 20)
        private int TotalLevels => (_levels != null && _levels.Length > 0) ? _levels.Length : 20;

        [Header("Icons (optional)")]
        [Tooltip("Icon hiển thị trên các nút level đã mở")]
        [SerializeField] private Sprite _levelIconSprite;
        [Tooltip("Icon ổ khóa hiển thị trên level chưa mở")]
        [SerializeField] private Sprite _lockIconSprite;
        [Tooltip("Kích thước icon (px)")]
        [SerializeField] private float _iconSize = 28f;

        // ─── Runtime refs ──────────────────────────────────────────────────────
        private GameObject _mainPanelGo;
        private GameObject _levelsPanelGo;
        private GameObject _loadingPanelGo;
        private Text       _loadingText;
        private ScrollRect _levelScroll;  // cached so ShowLevelsPanel can re-scroll

        // ─── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            BuildMainMenu();
        }

        // ─── Build ─────────────────────────────────────────────────────────────

        private void BuildMainMenu()
        {
            // Canvas / scaler — components guaranteed by [RequireComponent], just configure them
            var canvas = GetComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = _uiScale;

            // ── 1. Loading screen (always on top, hidden by default) ───────────
            _loadingPanelGo = CreateFullscreenPanel("LoadingPanel", Color.black);
            _loadingText = CreateText("LoadingText", _loadingPanelGo.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(400, 100),
                "Loading...", 32, Color.white, TextAnchor.MiddleCenter);
            _loadingPanelGo.SetActive(false);

            // ── 2. Main panel ──────────────────────────────────────────────────
            _mainPanelGo = CreateFullscreenPanel("MainPanel", Color.clear, addImage: false);

            CreateText("Title", _mainPanelGo.transform,
                new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f),
                Vector2.zero, new Vector2(600, 100),
                _titleText, _titleFontSize, _titleColor, TextAnchor.MiddleCenter);

            CreateButton("PlayButton", _mainPanelGo.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 30), new Vector2(240, 60),
                "Play", () =>
                {
                    // Cap to actual level count so we never exceed the playlist
                    int recommended = Mathf.Clamp(LevelProgressManager.RecommendedLevelIndex,
                                                  0, TotalLevels - 1);
                    StartGame(recommended);
                });

            CreateButton("SelectLevelButton", _mainPanelGo.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -50), new Vector2(240, 60),
                "Select Level", () => ShowLevelsPanel(true));

            // ── 3. Levels panel ────────────────────────────────────────────────
            _levelsPanelGo = CreateFullscreenPanel("LevelsPanel", new Color(0.1f, 0.1f, 0.1f, 0.95f));
            if (_panelBgSprite != null)
            {
                var bg = _levelsPanelGo.GetComponent<Image>();
                bg.sprite = _panelBgSprite;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }

            CreateText("LevelsTitle", _levelsPanelGo.transform,
                new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f),
                Vector2.zero, new Vector2(400, 60),
                "SELECT LEVEL", 36, Color.white, TextAnchor.MiddleCenter);

            BuildLevelGrid();

            CreateButton("BackButton", _levelsPanelGo.transform,
                new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f),
                Vector2.zero, new Vector2(160, 50),
                "Back", () => ShowLevelsPanel(false));

            _levelsPanelGo.SetActive(false);
        }

        private void BuildLevelGrid()
        {
            // ── ScrollRect container ──────────────────────────────────────────
            var scrollGo = new GameObject("LevelScrollView");
            scrollGo.transform.SetParent(_levelsPanelGo.transform, false);

            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin        = new Vector2(0f, 0.3f);
            scrollRt.anchorMax        = new Vector2(1f, 0.75f);
            scrollRt.anchoredPosition = Vector2.zero;
            // Inset from both sides by _scrollSideMargin
            scrollRt.sizeDelta        = new Vector2(-_scrollSideMargin * 2f, 0f);

            // Mask – Image must be opaque white for the stencil mask to work correctly
            var maskImg = scrollGo.AddComponent<Image>();
            maskImg.color = Color.white;
            var mask = scrollGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // ── Content (the actual list inside the scroll) ───────────────────
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);

            var contentRt = contentGo.AddComponent<RectTransform>();
            // Anchored left, stretches vertically
            contentRt.anchorMin = new Vector2(0f, 0f);
            contentRt.anchorMax = new Vector2(0f, 1f);
            contentRt.pivot     = new Vector2(0f, 0.5f);
            contentRt.anchoredPosition = Vector2.zero;

            float padding = _spacingX * 0.5f;
            float totalW  = padding + TotalLevels * _spacingX + padding;
            contentRt.sizeDelta = new Vector2(totalW, 0);

            // Populate level buttons
            for (int i = 0; i < TotalLevels; i++)
            {
                int levelIndex = i;
                bool unlocked  = LevelProgressManager.IsUnlocked(i);
                float xPos     = padding + i * _spacingX + 30f; // 30 = half button width

                var anchoredPos = new Vector2(xPos, 0);
                CreateLevelButton($"Level_{i + 1}", contentGo.transform, anchoredPos, levelIndex, unlocked);
            }

            // ── ScrollRect component ──────────────────────────────────────────
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.content    = contentRt;
            scroll.horizontal = true;
            scroll.vertical   = false;
            scroll.movementType        = ScrollRect.MovementType.Elastic;
            scroll.elasticity          = 0.1f;
            scroll.inertia             = true;
            scroll.decelerationRate    = 0.135f;
            scroll.scrollSensitivity   = 10f;
            scroll.horizontalScrollbar  = null;
            scroll.verticalScrollbar    = null;

            // Cache ref and do NOT auto-scroll here —
            // ScrollToUnlocked is called each time the panel becomes visible.
            _levelScroll = scroll;
        }

        /// Scroll to show the last unlocked level button after layout is ready.
        private System.Collections.IEnumerator ScrollToUnlocked(ScrollRect scroll)
        {
            // Wait two frames so Unity finishes laying out the RectTransforms
            yield return null;
            yield return null;

            int maxUnlocked  = LevelProgressManager.MaxUnlockedIndex;
            float padding    = _spacingX * 0.5f;
            float contentW   = padding + TotalLevels * _spacingX + padding;

            // Center of the target button within the content
            float targetX = padding + maxUnlocked * _spacingX + 30f;

            // Viewport width
            float viewportW = scroll.GetComponent<RectTransform>().rect.width;
            if (viewportW <= 0) viewportW = Screen.width; // fallback

            // normalizedPosition 0 = left end, 1 = right end
            float scrollable = contentW - viewportW;
            if (scrollable <= 0f)
            {
                scroll.horizontalNormalizedPosition = 0f;
            }
            else
            {
                // Center the target button in the viewport
                float ideal = (targetX - viewportW * 0.5f) / scrollable;
                scroll.horizontalNormalizedPosition = Mathf.Clamp01(ideal);
            }
        }

        private void CreateLevelButton(string name, Transform parent, Vector2 anchoredPos,
                                       int levelIndex, bool unlocked)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            var rt  = go.GetComponent<RectTransform>();
            // In a horizontal scroll, anchor to left-center of its own pivot
            rt.anchorMin        = new Vector2(0f, 0.5f);
            rt.anchorMax        = new Vector2(0f, 0.5f);
            rt.pivot            = new Vector2(0f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = new Vector2(60, 60);

            if (unlocked)
            {
                Sprite sp = _levelButtonSprite != null ? _levelButtonSprite : _buttonBgSprite;
                if (sp != null)
                {
                    img.sprite = sp;
                    img.type   = Image.Type.Simple;
                    img.preserveAspect = true;
                    img.color  = Color.white;
                }
                else
                {
                    img.color = _colorButtonBg;
                }
            }
            else
            {
                Sprite sp = _levelButtonLockedSprite != null ? _levelButtonLockedSprite
                          : (_levelButtonSprite != null ? _levelButtonSprite : _buttonBgSprite);
                if (sp != null)
                {
                    img.sprite = sp;
                    img.type   = Image.Type.Simple;
                    img.preserveAspect = true;
                    img.color  = _colorLockedBg;
                }
                else
                {
                    img.color = _colorLockedBg;
                }
            }

            if (unlocked)
            {
                var btn = go.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.onClick.AddListener(() => SoundManager.Instance?.PlayButtonClick());
                btn.onClick.AddListener(() => StartGame(levelIndex));

                // Level icon — centered in button
                if (_levelIconSprite != null)
                    AddIcon(go.transform, _levelIconSprite, Vector2.zero);

                // Level number — drawn on top of icon, centered
                CreateText("Label", go.transform,
                    Vector2.zero, Vector2.one,
                    Vector2.zero, Vector2.zero,
                    $"{levelIndex + 1}", 20, _colorButtonText, TextAnchor.MiddleCenter);
            }
            else
            {
                // Lock icon
                if (_lockIconSprite != null)
                    AddIcon(go.transform, _lockIconSprite, Vector2.zero);
                else
                    CreateText("Label", go.transform,
                        Vector2.zero, Vector2.one,
                        Vector2.zero, Vector2.zero,
                        "🔒", 18, _colorLockedText, TextAnchor.MiddleCenter);
            }
        }

        private void AddIcon(Transform parent, Sprite sprite, Vector2 offset)
        {
            var iconGo  = new GameObject("Icon");
            iconGo.transform.SetParent(parent, false);

            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite         = sprite;
            iconImg.type           = Image.Type.Simple;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget  = false;

            var iconRt              = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin        = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax        = new Vector2(0.5f, 0.5f);
            iconRt.pivot            = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = offset;
            iconRt.sizeDelta        = new Vector2(_iconSize, _iconSize);
        }

        // ─── Panel helpers ─────────────────────────────────────────────────────

        private GameObject CreateFullscreenPanel(string name, Color bg, bool addImage = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = Vector2.zero;

            if (addImage)
            {
                var img   = go.AddComponent<Image>();
                img.color = bg;
            }
            return go;
        }

        // ─── Navigation ────────────────────────────────────────────────────────

        private void ShowLevelsPanel(bool show)
        {
            _mainPanelGo.SetActive(!show);
            _levelsPanelGo.SetActive(show);

            if (show && _levelScroll != null)
                StartCoroutine(ScrollToUnlocked(_levelScroll));
        }

        private void StartGame(int levelIndex)
        {
            GameContext.SelectedLevelIndex = levelIndex;
            StartCoroutine(LoadGameRoutine());
        }

        private IEnumerator LoadGameRoutine()
        {
            _mainPanelGo.SetActive(false);
            _levelsPanelGo.SetActive(false);

            bool fadeDone = false;
            TransitionOverlay.Instance.FadeToBlack(() => fadeDone = true);
            yield return new WaitUntil(() => fadeDone);

            _loadingPanelGo.SetActive(true);
            yield return new WaitForSeconds(0.15f);

            var asyncLoad = SceneManager.LoadSceneAsync("Game");
            if (asyncLoad == null)
            {
                Debug.LogError("[MainMenuView] Cannot load scene 'Game'. Check Build Settings.");
                _loadingText.text = "Scene 'Game' not found!";
                yield break;
            }

            while (!asyncLoad.isDone)
            {
                _loadingText.text = $"Loading... {Mathf.RoundToInt(asyncLoad.progress * 100)}%";
                yield return null;
            }
        }

        // ─── UI factory helpers ────────────────────────────────────────────────

        private Button CreateButton(string name, Transform parent,
                                    Vector2 anchorMin, Vector2 anchorMax,
                                    Vector2 anchoredPos, Vector2 size,
                                    string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            if (_buttonBgSprite != null)
            {
                img.sprite         = _buttonBgSprite;
                img.type           = Image.Type.Simple;
                img.preserveAspect = true;
                img.color          = Color.white;
            }
            else
            {
                img.color = _colorButtonBg;
            }

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => SoundManager.Instance?.PlayButtonClick());
            btn.onClick.AddListener(onClick);

            var rt              = go.GetComponent<RectTransform>();
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;
            rt.pivot            = new Vector2(0.5f, 0.5f);

            if (!string.IsNullOrEmpty(label))
            {
                CreateText("Text", go.transform,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                    label, 20, _colorButtonText, TextAnchor.MiddleCenter);
            }

            return btn;
        }

        private Text CreateText(string name, Transform parent,
                                Vector2 anchorMin, Vector2 anchorMax,
                                Vector2 anchoredPos, Vector2 size,
                                string content, int fontSize, Color color,
                                TextAnchor alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var txt       = go.AddComponent<Text>();
            txt.font      = _customFont != null ? _customFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text      = content;
            txt.fontSize  = fontSize;
            txt.color     = color;
            txt.alignment = alignment;

            var rt              = go.GetComponent<RectTransform>();
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;
            rt.pivot            = new Vector2(0.5f, 0.5f);

            return txt;
        }
    }
}
