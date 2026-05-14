using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// Stores and retrieves level unlock progress using PlayerPrefs.
    /// Key: "MaxUnlockedLevel" → the highest level index accessible.
    /// Level 0 is always unlocked.
    /// </summary>
    public static class LevelProgressManager
    {
        private const string PrefKey = "MaxUnlockedLevel";

        /// <summary>The highest level index the player has unlocked.</summary>
        public static int MaxUnlockedIndex
        {
            get => PlayerPrefs.GetInt(PrefKey, 0);
            private set
            {
                PlayerPrefs.SetInt(PrefKey, value);
                PlayerPrefs.Save();
            }
        }

        /// <summary>True if the given level index is currently accessible.</summary>
        public static bool IsUnlocked(int index) => index <= MaxUnlockedIndex;

        /// <summary>
        /// Called after clearing a level. Unlocks the next level if not already unlocked.
        /// Returns true if a new level was unlocked.
        /// </summary>
        public static bool UnlockNextLevel(int clearedIndex, int totalLevels)
        {
            int nextIndex = clearedIndex + 1;
            if (nextIndex >= totalLevels) return false;          // no more levels
            if (nextIndex <= MaxUnlockedIndex) return false;     // already unlocked

            MaxUnlockedIndex = nextIndex;
            return true;
        }

        /// <summary>
        /// The recommended level to send the player to when they press "Play":
        /// the highest unlocked level.
        /// </summary>
        public static int RecommendedLevelIndex => MaxUnlockedIndex;

        /// <summary>Wipes all progress (use carefully — for debug/testing only).</summary>
        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(PrefKey);
            PlayerPrefs.Save();
        }
    }
}
