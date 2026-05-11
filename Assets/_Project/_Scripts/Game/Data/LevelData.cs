using System;
using System.Collections.Generic;
using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// ScriptableObject that stores the layout of a single level.
    /// Designer-editable in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "KNLVN/Level Data", fileName = "Level_")]
    public class LevelData : ScriptableObject
    {
        [Header("Grid Dimensions")]
        public int Width  = 10;
        public int Height = 8;

        [Header("Player")]
        public Vector2Int PlayerStartPos = new Vector2Int(1, 1);

        [Header("Cells")]
        public List<CellDefinition> Cells = new List<CellDefinition>();
    }

    /// <summary>
    /// Defines a single cell in the level layout.
    /// </summary>
    [Serializable]
    public class CellDefinition
    {
        public Vector2Int Pos;
        public CellType   Type;
        /// <summary>
        /// Raw content string: "0"–"9", "+", "-", "*", "/", "=", or blank.
        /// For Yellow/Star cells this is their starting content (optional).
        /// </summary>
        public string     Content;
    }
}
