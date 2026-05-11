using UnityEngine;
using UnityEngine.InputSystem;

namespace KNLVN.Game
{
    /// <summary>
    /// Orchestrates all player actions: movement, pushing, and F-key interaction.
    ///
    /// Each action follows this sequence:
    ///   1. Push undo snapshot
    ///   2. Apply action
    ///   3. Run equation evaluator → update door state
    ///   4. Notify views via EventBus
    ///
    /// Input is read via Unity's new Input System (keyboard polling).
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        // ─── Inspector refs ───────────────────────────────────────────────────
        [SerializeField] private LevelManager      _levelManager;
        [SerializeField] private EventBusComponent _eventBus;

        // ─── Runtime state ────────────────────────────────────────────────────
        public Vector2Int      GridPos  { get; private set; }
        public FacingDirection Facing   { get; private set; } = FacingDirection.Down;
        public CellContent     HeldItem { get; private set; } = CellContent.Empty;

        // ─── Sub-systems (created after level load) ───────────────────────────
        private PushSystem         _pushSystem;
        private InteractionSystem  _interactionSystem;
        private EquationEvaluator  _evaluator;
        private DoorController     _door;
        private UndoSystem         _undo;

        // ─── Input repeat throttling ──────────────────────────────────────────
        private float _moveRepeatTimer;
        private const float MoveRepeatDelay    = 0.18f;
        private const float MoveRepeatInterval = 0.10f;
        private Vector2Int _lastMoveDir;

        // ─── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _undo = new UndoSystem();
        }

        private void OnEnable()
        {
            _eventBus?.Subscribe<LevelLoadedEvent>(OnLevelLoaded);
            _eventBus?.Subscribe<LevelResetEvent>(OnLevelReset);
        }

        private void OnDisable()
        {
            _eventBus?.Unsubscribe<LevelLoadedEvent>(OnLevelLoaded);
            _eventBus?.Unsubscribe<LevelResetEvent>(OnLevelReset);
        }

        private void OnLevelLoaded(LevelLoadedEvent evt)
        {
            var grid = _levelManager.Grid;
            GridPos           = _levelManager.CurrentLevel.PlayerStartPos;
            Facing            = FacingDirection.Down;
            HeldItem          = CellContent.Empty;

            _pushSystem        = new PushSystem(grid);
            _interactionSystem = new InteractionSystem(grid);
            _evaluator         = new EquationEvaluator(grid);
            _door              = new DoorController(_eventBus);

            _undo.Clear();
            RefreshEquation();
        }

        private void OnLevelReset(LevelResetEvent evt)
        {
            // LevelManager already rebuilt the grid; re-init sub-systems
            OnLevelLoaded(new LevelLoadedEvent { Data = _levelManager.CurrentLevel });
        }

        // ─── Update ───────────────────────────────────────────────────────────

        private void Update()
        {
            HandleMovementInput();
            HandleInteractionInput();
            HandleUndoInput();
            HandleResetInput();
        }

        // ─── Input handlers ───────────────────────────────────────────────────

        private void HandleMovementInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            Vector2Int dir = Vector2Int.zero;
            if      (kb.wKey.isPressed || kb.upArrowKey.isPressed)    dir = Vector2Int.up;
            else if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  dir = Vector2Int.down;
            else if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  dir = Vector2Int.left;
            else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir = Vector2Int.right;

            if (dir == Vector2Int.zero)
            {
                _moveRepeatTimer = 0f;
                _lastMoveDir     = Vector2Int.zero;
                return;
            }

            bool isNewKey = (dir != _lastMoveDir);
            _lastMoveDir  = dir;

            if (isNewKey)
            {
                _moveRepeatTimer = -MoveRepeatDelay;
                TryMove(dir);
            }
            else
            {
                _moveRepeatTimer += Time.deltaTime;
                if (_moveRepeatTimer >= MoveRepeatInterval)
                {
                    _moveRepeatTimer = 0f;
                    TryMove(dir);
                }
            }
        }

        private void HandleInteractionInput()
        {
            if (Keyboard.current?.fKey.wasPressedThisFrame == true)
                TryInteract();
        }

        private void HandleUndoInput()
        {
            if (Keyboard.current?.zKey.wasPressedThisFrame == true)
                TryUndo();
        }

        private void HandleResetInput()
        {
            if (Keyboard.current?.rKey.wasPressedThisFrame == true)
                _levelManager.ResetLevel();
        }

        // ─── Action implementations ───────────────────────────────────────────

        private void TryMove(Vector2Int dir)
        {
            var grid   = _levelManager.Grid;
            var facing = FacingDirectionExtensions.FromVector(dir);

            // Always update facing direction
            bool facingChanged = facing != Facing;
            Facing = facing;

            Vector2Int targetPos  = GridPos + dir;
            var targetCell = grid.GetCell(targetPos);
            //Debug.Log($"[PlayerController] Target cell: {targetCell}, x: {targetPos.x}, y: {targetPos.y}");
            if (targetCell == null) return;

            // ── If target is pushable ─────────────────────────────────────────
            if (targetCell.IsPushable)
            {
                PushSnapshot();
                bool pushed = _pushSystem.TryPush(GridPos, dir, _door.IsOpen);
                if (pushed)
                {
                    Vector2Int boxFrom = targetPos;
                    Vector2Int boxTo   = targetPos + dir;
                    GridPos = targetPos;
                    PostActionWithPush(boxFrom, boxTo);
                }
                else if (facingChanged)
                {
                    // Just a facing update — notify view
                    NotifyPlayerMoved();
                }
                return;
            }

            // ── Normal move ───────────────────────────────────────────────────
            if (!targetCell.IsWalkable(_door.IsOpen)) 
            {
                if (facingChanged) NotifyPlayerMoved();
                return;
            }

            PushSnapshot();
            GridPos = targetPos;

            // Check if player walked onto open door
            if (targetCell.IsRed && _door.IsOpen)
                _eventBus?.Publish(new DoorEnteredEvent());

            PostAction();
        }

        private void TryInteract()
        {
            var grid = _levelManager.Grid;
            var held = HeldItem;

            PushSnapshot();
            bool changed = _interactionSystem.TryInteract(GridPos, Facing, ref held);

            if (changed)
            {
                HeldItem = held;
                _eventBus?.Publish(new PlayerHeldItemChangedEvent { HeldItem = HeldItem });
                PostAction();
            }
        }

        private void TryUndo()
        {
            var grid = _levelManager.Grid;

            // ── Snapshot pushable positions BEFORE restore ────────────────────
            var pushablesBefore = new System.Collections.Generic.HashSet<UnityEngine.Vector2Int>();
            for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
            {
                var c = grid.GetCell(x, y);
                if (c != null && c.IsPushable)
                    pushablesBefore.Add(new UnityEngine.Vector2Int(x, y));
            }

            bool ok = _undo.TryUndo(out var pos, out var facing, out var held, grid);
            if (!ok) return;

            GridPos  = pos;
            Facing   = facing;
            HeldItem = held;

            // ── Diff pushable positions AFTER restore ─────────────────────────
            UnityEngine.Vector2Int? boxWasAt = null, boxNowAt = null;
            for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
            {
                var p = new UnityEngine.Vector2Int(x, y);
                bool isPushableNow = grid.GetCell(x, y)?.IsPushable == true;
                bool wasPushable   = pushablesBefore.Contains(p);

                if ( isPushableNow && !wasPushable) boxNowAt = p;   // box returned here
                if (!isPushableNow &&  wasPushable) boxWasAt = p;   // box left here
            }

            _eventBus?.Publish(new PlayerMovedEvent          { NewPos    = GridPos  });
            _eventBus?.Publish(new PlayerHeldItemChangedEvent { HeldItem  = HeldItem });
            _eventBus?.Publish(new UndoPerformedEvent        { BoxWasAt  = boxWasAt, BoxNowAt = boxNowAt });

            RefreshEquation();
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private void PushSnapshot()
        {
            _undo.Push(GridPos, Facing, HeldItem, _levelManager.Grid);
        }

        private void PostAction()
        {
            NotifyPlayerMoved();
            RefreshEquation();
        }

        private void PostActionWithPush(Vector2Int boxFrom, Vector2Int boxTo)
        {
            _eventBus?.Publish(new PlayerMovedEvent
            {
                NewPos           = GridPos,
                PushedBoxFromPos = boxFrom,
                PushedBoxToPos   = boxTo,
            });
            RefreshEquation();
        }

        private void NotifyPlayerMoved()
        {
            _eventBus?.Publish(new PlayerMovedEvent { NewPos = GridPos });
        }

        private void RefreshEquation()
        {
            bool valid = _evaluator.Evaluate();
            _door.ApplyEquationResult(valid);
            _eventBus?.Publish(new EquationChangedEvent { IsValid = valid });
        }
    }
}
