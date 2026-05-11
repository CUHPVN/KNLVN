using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>The four cardinal directions the player can face.</summary>
    public enum FacingDirection { Right, Left, Up, Down }

    public static class FacingDirectionExtensions
    {
        public static Vector2Int ToVector(this FacingDirection dir) => dir switch
        {
            FacingDirection.Right => Vector2Int.right,
            FacingDirection.Left  => Vector2Int.left,
            FacingDirection.Up    => Vector2Int.up,
            FacingDirection.Down  => Vector2Int.down,
            _                     => Vector2Int.zero
        };

        public static FacingDirection FromVector(Vector2Int v)
        {
            if (v == Vector2Int.right) return FacingDirection.Right;
            if (v == Vector2Int.left)  return FacingDirection.Left;
            if (v == Vector2Int.up)    return FacingDirection.Up;
            return FacingDirection.Down;
        }
    }
}
