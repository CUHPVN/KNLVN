namespace KNLVN.Game
{
    /// <summary>
    /// Defines the structural type of a grid cell.
    /// </summary>
    public enum CellType
    {
        /// <summary>Walkable empty floor (may contain a floor item).</summary>
        Empty,

        /// <summary>Impassable wall that borders the map.</summary>
        Wall,

        /// <summary>Fixed blue cell — cannot be pushed, forms the equation anchor.</summary>
        Blue,

        /// <summary>Pushable yellow cell — can carry a number/operator value.</summary>
        Yellow,

        /// <summary>Exit door (red) — locked until the equation is satisfied.</summary>
        Red,

        /// <summary>Special pushable star cell — its numeric value is doubled in the equation.</summary>
        Star,
    }
}
