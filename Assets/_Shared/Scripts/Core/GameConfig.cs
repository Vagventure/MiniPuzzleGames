using UnityEngine;

namespace PuzzleGame.Core
{
    /// <summary>
    /// Project-wide tuning + testing switches. One asset at
    /// <c>Assets/_Shared/Resources/GameConfig.asset</c> (created by
    /// <c>PuzzleGame ▸ Setup</c>). Edit it in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "PuzzleGame/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Testing")]
        [Tooltip("Every level is selectable and playable from the main menu, ignoring saved progress.")]
        public bool unlockAllLevels = false;

        [Tooltip("0 = play the whole chapter. Otherwise only the first N levels of EACH mini-game are " +
                 "playable; finishing level N jumps straight to the next mini-game. Great for testing hand-offs.")]
        [Min(0)]
        public int playableLevelsPerChapter = 0;

        [Header("Flow")]
        [Tooltip("Show the \"<chapter> complete — next up …\" card when crossing between mini-games.")]
        public bool showChapterIntro = true;

        [Tooltip("SKIP button on the shared HUD appears once the player has failed the CURRENT level " +
                 "this many times. 0 = always show, -1 = never show. Only visible inside a level.")]
        public int skipAfterFailedAttempts = 5;

        [Tooltip("Seconds for the fade between levels / mini-games.")]
        [Min(0f)]
        public float fadeDuration = 0.35f;

        // ------------------------------------------------------------------
        private static GameConfig _instance;

        public static GameConfig Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = Resources.Load<GameConfig>("GameConfig");
                if (_instance == null)
                {
                    _instance = CreateInstance<GameConfig>();
                    _instance.name = "GameConfig (defaults)";
                }
                return _instance;
            }
        }

        /// <summary>Editor/tests only.</summary>
        public static void OverrideInstance(GameConfig cfg) => _instance = cfg;
    }
}
