using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuzzleGame.Core
{
    /// <summary>
    /// Owns the single global level sequence that stitches all mini-games together.
    ///
    /// Responsibilities:
    ///   * know which global level (1..N) is active
    ///   * load the next / previous / a specific level, by scene PATH (no scene renaming needed)
    ///   * hand off between mini-games at chapter boundaries (with an intro card)
    ///   * persist progress through <see cref="SaveManager"/>
    ///
    /// The per-game scripts never talk to SceneManager for progression any more; they call
    /// <see cref="NotifyLevelCompleted"/> / <see cref="RequestNextLevel"/> / <see cref="RequestRestart"/> /
    /// <see cref="RequestMainMenu"/> on this controller.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class LevelFlowController : MonoBehaviour
    {
        public static LevelFlowController Instance { get; private set; }

        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private string mainMenuScenePath = "Assets/_Shared/Scenes/MainMenu.unity";

        public LevelCatalog Catalog => catalog;
        public string MainMenuScenePath => mainMenuScenePath;

        private static GameConfig Config => GameConfig.Instance;
        private bool ShowChapterIntro => Config.showChapterIntro;
        private int ChapterCap => Mathf.Max(0, Config.playableLevelsPerChapter);

        /// <summary>1-based global index of the level currently loaded, or 0 when in a menu.</summary>
        public int CurrentGlobalIndex { get; private set; }

        public LevelCatalog.LevelRef Current =>
            catalog != null ? catalog.Get(CurrentGlobalIndex) : default;

        public int TotalLevels => catalog != null ? catalog.TotalLevels : 0;

        /// <summary>Fired after a level scene has finished loading. Arg = the level just loaded.</summary>
        public event Action<LevelCatalog.LevelRef> LevelLoaded;

        /// <summary>Fired when the active mini-game changes. Args = (previous, next).</summary>
        public event Action<GameId, GameId> ChapterChanged;

        /// <summary>Fired after the very last level in the catalog is completed.</summary>
        public event Action AllLevelsCompleted;

        private GameId _lastGame = GameId.None;

        // per-level failure tracking (drives the SKIP button + Connect Balls stars)
        private int _failIndex = -1;
        private int _fails;

        /// <summary>How many times the player has failed the level they are currently on.</summary>
        public int CurrentFails => (_failIndex == CurrentGlobalIndex) ? _fails : 0;

        /// <summary>Whether the SKIP button should currently be offered.</summary>
        public bool SkipAvailable
        {
            get
            {
                if (CurrentGlobalIndex < 1) return false;
                int after = GameConfig.Instance.skipAfterFailedAttempts;
                if (after < 0) return false;
                return CurrentFails >= after;
            }
        }

        /// <summary>3 / 2 / 1 stars from a clean / few-fail / many-fail run — for games with no
        /// score system of their own (Connect Balls).</summary>
        public int StarsFromFails()
        {
            int f = CurrentFails;
            if (f == 0) return 3;
            if (f <= 2) return 2;
            return 1;
        }

        // ------------------------------------------------------------------
        internal void Configure(LevelCatalog cat)
        {
            if (catalog == null) catalog = cat;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (catalog == null)
                catalog = Resources.Load<LevelCatalog>("LevelCatalog");
            if (catalog == null)
            {
                Debug.LogWarning("[LevelFlow] No LevelCatalog asset found in a Resources folder — using built-in defaults.");
                catalog = LevelCatalog.BuildDefault();
            }
            catalog.Rebuild();

            SaveManager.EnsureInitialised();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // ------------------------------------------------------------------
        // Public entry points
        // ------------------------------------------------------------------

        /// <summary>Play from level 1 (does not touch saved progress).</summary>
        public void StartNewGame() => LoadLevel(1);

        /// <summary>The level the player is currently "on": the loaded level if in one,
        /// otherwise the highest unlocked level (clamped to what is currently playable).</summary>
        public int ResumeTargetIndex
        {
            get
            {
                if (CurrentGlobalIndex >= 1) return CurrentGlobalIndex;
                int target = Mathf.Clamp(SaveManager.HighestUnlocked, 1, Mathf.Max(1, TotalLevels));
                if (!IsLevelUnlocked(target))
                    target = IsLevelUnlocked(1) ? 1 : Mathf.Max(1, NextPlayableIndex(0));
                return target;
            }
        }

        /// <summary>Continue from <see cref="ResumeTargetIndex"/>.</summary>
        public void Resume() => LoadLevel(ResumeTargetIndex);

        public void LoadLevel(int globalIndex)
        {
            if (catalog == null || catalog.TotalLevels == 0)
            {
                Debug.LogError("[LevelFlow] Catalog is empty — nothing to load.");
                return;
            }

            globalIndex = Mathf.Clamp(globalIndex, 1, catalog.TotalLevels);
            var target = catalog.Get(globalIndex);
            if (!target.IsValid)
            {
                Debug.LogError($"[LevelFlow] No level at global index {globalIndex}.");
                return;
            }

            // moving to a different level clears the fail streak; reloading the same
            // level (a restart) keeps it, so SKIP stays available.
            if (globalIndex != _failIndex)
            {
                _failIndex = globalIndex;
                _fails = 0;
            }

            CurrentGlobalIndex = globalIndex;

            var ui = SharedUIReference;
            if (ui != null) ui.CoverThenLoad(() => LoadSceneSafe(target.ScenePath, target.SceneName));
            else LoadSceneSafe(target.ScenePath, target.SceneName);
        }

        /// <summary>Call when the current level is beaten. Records progress + stars, and (for
        /// games with no star UI of their own) shows a star strip over their complete screen.</summary>
        public void NotifyLevelCompleted(int stars = 0)
        {
            if (CurrentGlobalIndex < 1) return;

            SaveManager.UnlockThrough(CurrentGlobalIndex);
            if (stars > 0) SaveManager.SetStars(CurrentGlobalIndex, stars);

            SharedUIReference?.ShowLevelCompleteStars(stars, Current.Game);
        }

        /// <summary>Call when the player fails / loses the current level.</summary>
        public void NotifyLevelFailed()
        {
            if (CurrentGlobalIndex < 1) return;
            if (_failIndex != CurrentGlobalIndex) { _failIndex = CurrentGlobalIndex; _fails = 0; }
            _fails++;
        }

        /// <summary>Advance to the next global level, crossing chapter boundaries automatically.
        /// Respects <see cref="GameConfig.playableLevelsPerChapter"/> (skips the rest of a
        /// capped chapter straight to the next mini-game).</summary>
        public void RequestNextLevel()
        {
            int next = NextPlayableIndex(CurrentGlobalIndex);

            if (next < 1 || next > catalog.TotalLevels)
            {
                AllLevelsCompleted?.Invoke();
                var ui = SharedUIReference;
                if (ui != null) ui.ShowAllComplete(RequestMainMenu);
                else RequestMainMenu();
                return;
            }

            var upcoming = catalog.Get(next);
            bool crossingChapter = upcoming.Game != Current.Game && Current.Game != GameId.None;

            if (crossingChapter && ShowChapterIntro && SharedUIReference != null)
            {
                var from = Current.Game;

                if (catalog.InterleaveBlockSize > 0)
                {
                    // Interleaved play: a chapter switch happens every few levels, so a
                    // blocking "tap to continue" card would be constant noise. Flash a quick
                    // non-blocking banner during the load instead and keep moving.
                    ChapterChanged?.Invoke(from, upcoming.Game);
                    SharedUIReference.FlashChapterBanner(upcoming.ChapterName);
                    LoadLevel(next);
                    return;
                }

                SharedUIReference.ShowChapterIntro(
                    finishedChapter: Current.ChapterName,
                    nextChapter: upcoming.ChapterName,
                    onContinue: () =>
                    {
                        ChapterChanged?.Invoke(from, upcoming.Game);
                        LoadLevel(next);
                    });
                return;
            }

            LoadLevel(next);
        }

        public void RequestRestart()
        {
            if (CurrentGlobalIndex >= 1) LoadLevel(CurrentGlobalIndex);
            else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Leave the current level without finishing it and go to the next one.
        /// Unlocks the current level (no stars) so progression still advances, then follows the
        /// same next/hand-off/cap rules as a normal completion.</summary>
        public void SkipLevel()
        {
            if (CurrentGlobalIndex >= 1)
                SaveManager.UnlockThrough(CurrentGlobalIndex);
            RequestNextLevel();
        }

        public void RequestPreviousLevel()
        {
            if (CurrentGlobalIndex > 1) LoadLevel(CurrentGlobalIndex - 1);
        }

        public void RequestMainMenu()
        {
            CurrentGlobalIndex = 0;
            Time.timeScale = 1f;
            var ui = SharedUIReference;
            if (ui != null) ui.CoverThenLoad(() => LoadSceneSafe(mainMenuScenePath, "MainMenu"));
            else LoadSceneSafe(mainMenuScenePath, "MainMenu");
        }

        // ------------------------------------------------------------------
        // Progression / unlock queries (used by the level-select grid)
        // ------------------------------------------------------------------

        /// <summary>True if the player is allowed to enter this global level right now.</summary>
        public bool IsLevelUnlocked(int globalIndex)
        {
            var lvl = catalog != null ? catalog.Get(globalIndex) : default;
            if (!lvl.IsValid) return false;

            int cap = ChapterCap;
            if (cap > 0 && lvl.ChapterLevelNumber > cap) return false;

            if (Config.unlockAllLevels) return true;
            return globalIndex <= SaveManager.HighestUnlocked;
        }

        /// <summary>Next global index the player may play after <paramref name="fromIndex"/>,
        /// honouring the per-chapter cap. Returns -1 when nothing is left.</summary>
        public int NextPlayableIndex(int fromIndex)
        {
            if (catalog == null) return -1;
            int cap = ChapterCap;

            for (int i = fromIndex + 1; i <= catalog.TotalLevels; i++)
            {
                var l = catalog.Get(i);
                if (!l.IsValid) return -1;
                if (cap > 0 && l.ChapterLevelNumber > cap) continue; // skip the capped tail of this chapter
                return i;
            }
            return -1;
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private UI.SharedUI SharedUIReference => UI.SharedUI.Instance;

        private void LoadSceneSafe(string path, string fallbackName)
        {
            Time.timeScale = 1f;
            try
            {
                SceneManager.LoadScene(path);
            }
            catch (Exception)
            {
                // Scene not registered by path (e.g. not in Build Settings yet) — try by name.
                SceneManager.LoadScene(fallbackName);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single || catalog == null) return;

            // If we were told which level we're loading, trust that. Otherwise (e.g. the
            // developer pressed Play directly inside a level scene) resolve it from the path.
            var resolved = catalog.Get(CurrentGlobalIndex);
            if (!resolved.IsValid || !ScenePathMatches(resolved, scene))
            {
                if (TryResolveByScene(scene, out var byScene))
                {
                    CurrentGlobalIndex = byScene.GlobalIndex;
                    resolved = byScene;
                }
            }

            if (!resolved.IsValid) return; // a menu / non-level scene

            if (_lastGame != resolved.Game)
            {
                var prev = _lastGame;
                _lastGame = resolved.Game;
                if (prev != GameId.None)
                    ChapterChanged?.Invoke(prev, resolved.Game);
            }

            LevelLoaded?.Invoke(resolved);
        }

        private static bool ScenePathMatches(LevelCatalog.LevelRef r, Scene scene)
        {
            if (!string.IsNullOrEmpty(scene.path) && scene.path == r.ScenePath) return true;
            return scene.name == r.SceneName;
        }

        private bool TryResolveByScene(Scene scene, out LevelCatalog.LevelRef match)
        {
            foreach (var l in catalog.Levels)
            {
                if ((!string.IsNullOrEmpty(scene.path) && scene.path == l.ScenePath) ||
                    (scene.name == l.SceneName))
                {
                    // scene.name alone is ambiguous across chapters (both use "1".."51");
                    // prefer a full-path hit, else accept the first name hit.
                    if (!string.IsNullOrEmpty(scene.path) && scene.path == l.ScenePath)
                    {
                        match = l;
                        return true;
                    }
                    match = l;
                    return true;
                }
            }
            match = default;
            return false;
        }
    }
}
