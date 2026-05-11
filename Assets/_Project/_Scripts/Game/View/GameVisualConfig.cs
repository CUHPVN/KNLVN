using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// Single source of truth for every visual constant used in the Game View layer.
    ///
    /// Covers:
    ///   • Sprites for each cell type and UI element
    ///   • Colors for cells, player, overlays, and UI
    ///   • Layout sizes (scales, offsets) for PlayerView and CellView children
    ///   • Animation parameters (duration, curve) shared by CellView and PlayerView
    ///   • TextMesh font settings to keep text crisp
    ///   • HintArrow appearance
    ///
    /// Create via: Assets → right-click → Create → KNLVN → Visual Config
    /// Then assign the asset in GridView, PlayerView, and HintArrowView Inspector fields.
    /// </summary>
    [CreateAssetMenu(menuName = "KNLVN/Visual Config", fileName = "GameVisualConfig")]
    public class GameVisualConfig : ScriptableObject
    {
        // ─── Sprites ──────────────────────────────────────────────────────────

        [Header("Cell Sprites (leave null = procedural fallback)")]
        public Sprite WallSprite;
        public Sprite EmptySprite;
        public Sprite BlueSprite;
        public Sprite YellowSprite;
        public Sprite StarSprite;
        public Sprite RedLockedSprite;
        public Sprite RedOpenSprite;

        [Header("Overlay Sprites")]
        [Tooltip("Inner panel shown inside cells that carry a number or operator.")]
        public Sprite ContentPanelSprite;
        [Tooltip("Small token shown for floor items on Empty cells.")]
        public Sprite FloorTokenSprite;
        [Tooltip("Tiling floor sprite drawn behind the whole grid. Leave null for procedural wood planks.")]
        public Sprite FloorTileSprite;
        [Tooltip("Background sprite of the held-item bubble above the player. Leave null for circle.")]
        public Sprite HeldBubbleSprite;

        [Header("Player Sprite")]
        public Sprite PlayerBodySprite;
        [Tooltip("Sprite for the corner-bracket marker shown on the cell the player faces. Leave null for procedural brackets.")]
        public Sprite FacingMarkerSprite;

        // ─── Colors ───────────────────────────────────────────────────────────

        [Header("Cell Colors")]
        public Color WallColor    = new Color(0.22f, 0.22f, 0.25f);
        public Color EmptyColor   = new Color(0.82f, 0.80f, 0.75f);
        public Color BlueColor    = new Color(0.27f, 0.52f, 0.90f);
        public Color YellowColor  = new Color(0.98f, 0.84f, 0.20f);
        public Color StarColor    = new Color(1.00f, 0.72f, 0.10f);
        public Color RedLockColor = new Color(0.85f, 0.22f, 0.22f);
        public Color RedOpenColor = new Color(0.22f, 0.80f, 0.22f);

        [Header("Player Colors")]
        public Color PlayerColor     = new Color(0.18f, 0.72f, 0.90f);
        [Tooltip("Background colour of the held-item bubble shown above the player.")]
        public Color HeldBubbleColor = new Color(0.20f, 0.80f, 1.00f);

        [Header("Overlay Colors")]
        [Tooltip("Tint of the white content panel sprite (controls opacity).")]
        public Color ContentPanelColor = new Color(1f, 1f, 1f, 0.92f);
        [Tooltip("Color of the floor-item token badge.")]
        public Color FloorTokenColor   = new Color(0.95f, 0.75f, 0.20f);

        [Header("Facing Marker Color")]
        [Tooltip("Color of the corner-bracket marker highlighting the cell the player faces.")]
        public Color FacingMarkerColor = new Color(1.00f, 0.90f, 0.30f, 0.75f);

        [Header("Hint Arrow Color")]
        public Color HintArrowColor = new Color(1f, 0.9f, 0.2f, 0.9f);

        // ─── Layout — Player ──────────────────────────────────────────────────

        [Header("Player Layout")]
        [Tooltip("Uniform scale of the player body sprite relative to one grid cell.")]
        [Range(0.3f, 1.2f)] public float PlayerBodyScale = 0.8f;

        [Tooltip("Local Y offset of the held-item bubble above the player pivot.")]
        [Range(0.2f, 1.5f)] public float BubbleOffsetY = 0.75f;

        [Tooltip("Uniform scale of the held-item bubble.")]
        [Range(0.1f, 1.0f)] public float BubbleScale = 0.45f;

        [Tooltip("characterSize used by the TextMesh inside the held-item bubble. " +
                 "Formula: visual_size ≈ characterSize × (fontSize / 10).")]
        [Range(0.01f, 0.5f)] public float HeldLabelCharSize = 0.216f;

        // ─── Layout — Cell overlays ───────────────────────────────────────────

        [Header("Cell Overlay Layout")]
        [Tooltip("Uniform local scale of the content panel (number/operator badge) inside a cell.")]
        [Range(0.2f, 1.0f)] public float ContentPanelScale = 0.62f;

        [Tooltip("characterSize of the TextMesh inside the content panel.")]
        [Range(0.01f, 0.5f)] public float ContentLabelCharSize = 0.154f;

        [Tooltip("Uniform local scale of the floor-item token badge inside a cell.")]
        [Range(0.1f, 0.8f)] public float FloorTokenScale = 0.32f;

        [Tooltip("characterSize of the TextMesh inside the floor-item token.")]
        [Range(0.01f, 0.5f)] public float FloorLabelCharSize = 0.168f;

        // ─── Font settings (shared) ───────────────────────────────────────────

        [Header("TextMesh Font (shared by all labels)")]
        [Tooltip("Bitmap resolution of the font. Higher = sharper text. Keep at 100 to avoid blur.")]
        public int LabelFontSize = 100;

        // ─── Animation ────────────────────────────────────────────────────────

        [Header("Animation")]
        [Tooltip("Duration (seconds) of box-push and player-move animations.")]
        [Range(0.05f, 0.8f)] public float MoveDuration = 0.12f;

        [Tooltip("Easing curve applied to box-push and player-move animations.")]
        public AnimationCurve MoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // ─── Runtime sprite getters (generate if not assigned) ────────────────

        public Sprite GetBgSprite(CellType type, bool doorOpen)
        {
            // Use == null (not ??) so Unity's fake-null unassigned fields are detected correctly.
            return type switch
            {
                CellType.Wall   => WallSprite   == null ? SpriteFactory.CreateWallBrick()      : WallSprite,
                CellType.Empty  => EmptySprite  == null ? SpriteFactory.CreateSquare()         : EmptySprite,
                CellType.Blue   => BlueSprite   == null ? SpriteFactory.CreateSquare()         : BlueSprite,
                CellType.Yellow => YellowSprite == null ? SpriteFactory.CreateRoundedRect()    : YellowSprite,
                CellType.Star   => StarSprite   == null ? SpriteFactory.CreateStar()           : StarSprite,
                CellType.Red    => doorOpen
                                   ? (RedOpenSprite   == null ? SpriteFactory.CreateDoor() : RedOpenSprite)
                                   : (RedLockedSprite == null ? SpriteFactory.CreateDoor() : RedLockedSprite),
                _               => SpriteFactory.CreateSquare()
            };
        }

        public Color GetBgColor(CellType type, bool doorOpen) => type switch
        {
            CellType.Wall   => WallColor,
            CellType.Empty  => EmptyColor,
            CellType.Blue   => BlueColor,
            CellType.Yellow => YellowColor,
            CellType.Star   => StarColor,
            CellType.Red    => doorOpen ? RedOpenColor : RedLockColor,
            _               => EmptyColor
        };

        public Sprite GetContentPanelSprite() =>
            ContentPanelSprite == null ? SpriteFactory.CreateRoundedRect(32, 0.28f) : ContentPanelSprite;

        public Sprite GetFloorTokenSprite() =>
            FloorTokenSprite == null ? SpriteFactory.CreateCircle(32) : FloorTokenSprite;

        public Sprite GetFloorTileSprite() =>
            FloorTileSprite == null ? SpriteFactory.CreateFloorTile() : FloorTileSprite;

        public Sprite GetPlayerSprite() =>
            PlayerBodySprite == null ? SpriteFactory.CreateCircle(64) : PlayerBodySprite;

        public Color GetHeldBubbleColor() => HeldBubbleColor;

        public Sprite GetHeldBubbleSprite() =>
            HeldBubbleSprite == null ? SpriteFactory.CreateCircle(64) : HeldBubbleSprite;

        public Sprite GetFacingMarkerSprite() =>
            FacingMarkerSprite == null ? SpriteFactory.CreateTargetMarker() : FacingMarkerSprite;
    }
}
