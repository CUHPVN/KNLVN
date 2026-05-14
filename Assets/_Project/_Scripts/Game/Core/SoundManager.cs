using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// Central Sound Manager — survives scene changes (DontDestroyOnLoad).
    /// Listens to the EventBus and plays the matching AudioClip.
    ///
    /// Setup:
    ///   1. Create an empty GameObject in any scene and attach this script.
    ///   2. Assign AudioClip slots in the Inspector.
    ///   3. Assign the EventBus reference.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance => _instance;
        private static SoundManager _instance;

        [Header("Dependencies")]
        [Tooltip("Nếu để trống, sẽ tự tìm trong Scene")]
        [SerializeField] private EventBusComponent _eventBus;

        [Header("Player")]
        [Tooltip("Nhân vật di chuyển bình thường")]
        [SerializeField] private AudioClip _sfxMove;
        [Tooltip("Nhân vật đẩy hộp")]
        [SerializeField] private AudioClip _sfxPush;
        [Tooltip("Nhặt / đặt item xuống")]
        [SerializeField] private AudioClip _sfxInteract;
        [Tooltip("Undo")]
        [SerializeField] private AudioClip _sfxUndo;

        [Header("Equation")]
        [Tooltip("Phương trình trở nên hợp lệ (cửa mở)")]
        [SerializeField] private AudioClip _sfxEquationSolved;
        [Tooltip("Phương trình trở nên không hợp lệ (cửa đóng)")]
        [SerializeField] private AudioClip _sfxEquationBroken;

        [Header("Door")]
        [Tooltip("Cửa mở")]
        [SerializeField] private AudioClip _sfxDoorOpen;
        [Tooltip("Cửa đóng")]
        [SerializeField] private AudioClip _sfxDoorClose;
        [Tooltip("Bước vào cửa (qua level)")]
        [SerializeField] private AudioClip _sfxDoorEnter;

        [Header("Level")]
        [Tooltip("Reset level")]
        [SerializeField] private AudioClip _sfxLevelReset;

        [Header("UI")]
        [Tooltip("Sound khi nhấn bất kỳ nút UI nào")]
        [SerializeField] private AudioClip _sfxButtonClick;

        [Header("Volume")]
        [SerializeField] [Range(0f, 1f)] private float _masterVolume = 1f;

        // ─── AudioSource ───────────────────────────────────────────────────────
        private AudioSource _source;

        // ─── Singleton ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            _source = GetComponent<AudioSource>();
            if (_source == null)
                _source = gameObject.AddComponent<AudioSource>();

            _source.playOnAwake = false;

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene Scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            TryBindEventBus();
        }

        private void TryBindEventBus()
        {
            if (_eventBus == null)
            {
                _eventBus = FindAnyObjectByType<EventBusComponent>();
            }
            
            if (_eventBus != null)
            {
                // Unsubscribe first to avoid double subscription
                UnsubscribeAll();
                SubscribeAll();
            }
        }

        // ─── Event wiring ──────────────────────────────────────────────────────
        private void OnEnable()
        {
            TryBindEventBus();
        }

        private void OnDisable()
        {
            UnsubscribeAll();
        }

        private void SubscribeAll()
        {
            if (_eventBus == null) return;
            _eventBus.Subscribe<PlayerMovedEvent>(OnPlayerMoved);
            _eventBus.Subscribe<PlayerHeldItemChangedEvent>(OnHeldItemChanged);
            _eventBus.Subscribe<UndoPerformedEvent>(_ => Play(_sfxUndo));
            _eventBus.Subscribe<EquationChangedEvent>(OnEquationChanged);
            _eventBus.Subscribe<EquationSolvedEvent>(_ => Play(_sfxEquationSolved));
            _eventBus.Subscribe<DoorOpenedEvent>(_ => Play(_sfxDoorOpen));
            _eventBus.Subscribe<DoorClosedEvent>(_ => Play(_sfxDoorClose));
            _eventBus.Subscribe<DoorEnteredEvent>(_ => Play(_sfxDoorEnter));
            _eventBus.Subscribe<LevelResetEvent>(_ => Play(_sfxLevelReset));
            _eventBus.Subscribe<LevelLoadedEvent>(OnLevelLoaded);
        }

        private void UnsubscribeAll()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<PlayerMovedEvent>(OnPlayerMoved);
            _eventBus.Unsubscribe<PlayerHeldItemChangedEvent>(OnHeldItemChanged);
            _eventBus.Unsubscribe<UndoPerformedEvent>(_ => Play(_sfxUndo));
            _eventBus.Unsubscribe<EquationChangedEvent>(OnEquationChanged);
            _eventBus.Unsubscribe<EquationSolvedEvent>(_ => Play(_sfxEquationSolved));
            _eventBus.Unsubscribe<DoorOpenedEvent>(_ => Play(_sfxDoorOpen));
            _eventBus.Unsubscribe<DoorClosedEvent>(_ => Play(_sfxDoorClose));
            _eventBus.Unsubscribe<DoorEnteredEvent>(_ => Play(_sfxDoorEnter));
            _eventBus.Unsubscribe<LevelResetEvent>(_ => Play(_sfxLevelReset));
            _eventBus.Unsubscribe<LevelLoadedEvent>(OnLevelLoaded);
        }

        // ─── Handlers ──────────────────────────────────────────────────────────
        private void OnPlayerMoved(PlayerMovedEvent evt)
        {
            if (evt.PushedBoxFromPos.HasValue)
                Play(_sfxPush);
            else
                Play(_sfxMove);
        }

        private void OnHeldItemChanged(PlayerHeldItemChangedEvent evt)
        {
            Play(_sfxInteract);
        }

        private bool _wasValid = false;
        private void OnEquationChanged(EquationChangedEvent evt)
        {
            // Bỏ qua mọi âm thanh phương trình nếu đang trong quá trình load màn mới hoặc fade màn hình
            if (TransitionOverlay.Instance != null && TransitionOverlay.Instance.IsTransitioning)
            {
                _wasValid = evt.IsValid;
                return;
            }

            // EquationSolvedEvent handles the "first valid" sound;
            // here we only react to the equation becoming INVALID again
            if (_wasValid && !evt.IsValid)
                Play(_sfxEquationBroken);
            _wasValid = evt.IsValid;
        }

        private void OnLevelLoaded(LevelLoadedEvent evt)
        {
            // Khi load một level mới (kể cả qua màn hoặc quay lại từ Menu),
            // reset trạng thái equation để không bị lọt âm thanh "vỡ phương trình"
            _wasValid = false;
        }

        // ─── Public API ────────────────────────────────────────────────────────
        /// <summary>Call from any UI button to play the click sound.</summary>
        public void PlayButtonClick() => Play(_sfxButtonClick);

        // ─── Core playback ─────────────────────────────────────────────────────
        private void Play(AudioClip clip)
        {
            if (clip == null || _source == null) return;
            _source.PlayOneShot(clip, _masterVolume);
        }
    }
}
