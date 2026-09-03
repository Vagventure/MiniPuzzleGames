using UnityEngine;

namespace PuzzleGame.Core
{
    /// <summary>
    /// Single source of truth for cross-game progression.
    ///
    /// Progress is stored as one global "highest unlocked level" pointer (1-based) in the
    /// PlayerPrefs key "levelUnlock" — the same key both original games already used, so
    /// legacy read sites keep working. After the games are wired through
    /// <see cref="LevelFlowController"/> the value means the GLOBAL level index.
    /// </summary>
    public static class SaveManager
    {
        private const string KeyUnlock  = "levelUnlock";
        private const string KeyVersion = "saveVersion";

        /// <summary>Bump this whenever the meaning of stored keys changes; old saves are wiped.</summary>
        private const int CurrentSaveVersion = 2;

        private static bool _checked;

        public static void EnsureInitialised()
        {
            if (_checked) return;
            _checked = true;

            int stored = PlayerPrefs.GetInt(KeyVersion, 0);
            if (stored != CurrentSaveVersion)
            {
                // Meaning of "levelUnlock" changed from per-game to global — start clean.
                PlayerPrefs.DeleteAll();
                PlayerPrefs.SetInt(KeyVersion, CurrentSaveVersion);
                PlayerPrefs.Save();
            }

            if (PlayerPrefs.GetInt(KeyUnlock, 0) < 1)
                PlayerPrefs.SetInt(KeyUnlock, 1);
        }

        /// <summary>Highest global level index the player may enter (>= 1).</summary>
        public static int HighestUnlocked
        {
            get
            {
                EnsureInitialised();
                return Mathf.Max(1, PlayerPrefs.GetInt(KeyUnlock, 1));
            }
        }

        /// <summary>Marks every level up to and including <paramref name="globalIndex"/> as unlocked.</summary>
        public static void UnlockThrough(int globalIndex)
        {
            EnsureInitialised();
            if (globalIndex + 1 > PlayerPrefs.GetInt(KeyUnlock, 1))
            {
                PlayerPrefs.SetInt(KeyUnlock, globalIndex + 1);
                PlayerPrefs.Save();
            }
        }

        public static bool IsUnlocked(int globalIndex) => globalIndex <= HighestUnlocked;

        public static void ResetProgress()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetInt(KeyVersion, CurrentSaveVersion);
            PlayerPrefs.SetInt(KeyUnlock, 1);
            PlayerPrefs.Save();
            _checked = true;
        }

        // ---- per-level stars (Find The Hole style), namespaced by global index ----
        public static int GetStars(int globalIndex) => PlayerPrefs.GetInt($"level{globalIndex}Stars", 0);

        public static void SetStars(int globalIndex, int stars)
        {
            stars = Mathf.Clamp(stars, 0, 3);
            if (stars > GetStars(globalIndex))
            {
                PlayerPrefs.SetInt($"level{globalIndex}Stars", stars);
                PlayerPrefs.Save();
            }
        }
    }
}
