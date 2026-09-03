using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleGame.Core
{
    /// <summary>
    /// Ordered description of every playable level across every mini-game.
    /// One <see cref="Chapter"/> == one mini-game's block of levels. The chapters are
    /// concatenated in array order to produce a single global 1..N level sequence.
    ///
    /// To add the third game later: add a <see cref="Chapter"/> entry (or fill in the
    /// existing Rolling Maze one) and nothing else in the flow code has to change.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "PuzzleGame/Level Catalog")]
    public class LevelCatalog : ScriptableObject
    {
        [Serializable]
        public class Chapter
        {
            public GameId gameId = GameId.None;

            [Tooltip("Shown on the chapter intro card, e.g. \"Connect Balls\".")]
            public string displayName = "";

            [Tooltip("Project-relative folder that holds this chapter's level scenes, " +
                     "e.g. \"Assets/Connect Balls/Scenes/Levels\".")]
            public string sceneFolder = "";

            [Tooltip("Scenes are assumed to be named \"{firstNumber}\" .. \"{firstNumber + levelCount - 1}\" " +
                     "(matches the original asset packs). Leave explicitSceneNames empty to use this.")]
            public int firstNumber = 1;

            public int levelCount = 0;

            [Tooltip("Optional. If set, these scene names are used verbatim instead of the numbered range. " +
                     "Order defines play order.")]
            public string[] explicitSceneNames = Array.Empty<string>();

            [Tooltip("Optional background sprite for this chapter (used by BackgroundController later).")]
            public Sprite background;

            public IEnumerable<string> SceneNames()
            {
                if (explicitSceneNames != null && explicitSceneNames.Length > 0)
                {
                    foreach (var n in explicitSceneNames)
                        yield return n;
                    yield break;
                }

                for (int i = 0; i < levelCount; i++)
                    yield return (firstNumber + i).ToString();
            }
        }

        /// <summary>A single resolved level in the global sequence.</summary>
        public readonly struct LevelRef
        {
            public readonly int GlobalIndex;        // 1-based position in the whole game
            public readonly GameId Game;
            public readonly string ChapterName;
            public readonly int ChapterLevelNumber; // 1-based position inside its own mini-game
            public readonly int ChapterLevelCount;
            public readonly string SceneName;       // e.g. "1"
            public readonly string ScenePath;       // e.g. "Assets/Connect Balls/Scenes/Levels/1.unity"

            public LevelRef(int globalIndex, GameId game, string chapterName,
                            int chapterLevelNumber, int chapterLevelCount,
                            string sceneName, string scenePath)
            {
                GlobalIndex = globalIndex;
                Game = game;
                ChapterName = chapterName;
                ChapterLevelNumber = chapterLevelNumber;
                ChapterLevelCount = chapterLevelCount;
                SceneName = sceneName;
                ScenePath = scenePath;
            }

            public bool IsChapterFinale => ChapterLevelNumber >= ChapterLevelCount;
            public bool IsValid => GlobalIndex >= 1 && !string.IsNullOrEmpty(SceneName);
        }

        [SerializeField]
        private List<Chapter> chapters = new List<Chapter>();

        public IReadOnlyList<Chapter> Chapters => chapters;

        // ------------------------------------------------------------------
        // Resolved (flattened) sequence — rebuilt lazily from the chapters.
        // ------------------------------------------------------------------
        private List<LevelRef> _flat;

        public IReadOnlyList<LevelRef> Levels
        {
            get
            {
                if (_flat == null) Rebuild();
                return _flat;
            }
        }

        public int TotalLevels => Levels.Count;

        public void Rebuild()
        {
            _flat = new List<LevelRef>(128);
            int global = 0;

            foreach (var ch in chapters)
            {
                if (ch == null || ch.levelCount <= 0 && (ch.explicitSceneNames == null || ch.explicitSceneNames.Length == 0))
                    continue;

                var names = new List<string>();
                foreach (var n in ch.SceneNames()) names.Add(n);

                for (int i = 0; i < names.Count; i++)
                {
                    global++;
                    string sceneName = names[i];
                    string path = string.IsNullOrEmpty(ch.sceneFolder)
                        ? sceneName
                        : $"{ch.sceneFolder.TrimEnd('/')}/{sceneName}.unity";

                    _flat.Add(new LevelRef(
                        globalIndex: global,
                        game: ch.gameId,
                        chapterName: string.IsNullOrEmpty(ch.displayName) ? ch.gameId.ToString() : ch.displayName,
                        chapterLevelNumber: i + 1,
                        chapterLevelCount: names.Count,
                        sceneName: sceneName,
                        scenePath: path));
                }
            }
        }

        /// <summary>1-based lookup. Returns an invalid ref if out of range.</summary>
        public LevelRef Get(int globalIndex)
        {
            var levels = Levels;
            if (globalIndex < 1 || globalIndex > levels.Count)
                return default;
            return levels[globalIndex - 1];
        }

        public bool TryGet(int globalIndex, out LevelRef level)
        {
            level = Get(globalIndex);
            return level.IsValid;
        }

        /// <summary>Global index of the first level of a given game, or -1.</summary>
        public int FirstIndexOf(GameId game)
        {
            foreach (var l in Levels)
                if (l.Game == game) return l.GlobalIndex;
            return -1;
        }

        // ------------------------------------------------------------------
        // Fallback used when no .asset has been created yet, so the game is
        // always runnable. Keep these numbers in sync with the real packs.
        // ------------------------------------------------------------------
        public static LevelCatalog BuildDefault()
        {
            var c = CreateInstance<LevelCatalog>();
            c.chapters = new List<Chapter>
            {
                new Chapter
                {
                    gameId = GameId.ConnectBalls,
                    displayName = "Connect Balls",
                    sceneFolder = "Assets/Connect Balls/Scenes/Levels",
                    firstNumber = 1,
                    levelCount = 60,
                },
                new Chapter
                {
                    gameId = GameId.FindTheHole,
                    displayName = "Find The Hole",
                    sceneFolder = "Assets/Find The Hole/Scenes/Levels",
                    firstNumber = 1,
                    levelCount = 51,
                },
                new Chapter
                {
                    gameId = GameId.RollingMaze,
                    displayName = "Rolling Maze",
                    sceneFolder = "Assets/Rolling Maze/Scenes/Levels",
                    firstNumber = 1,
                    levelCount = 0, // filled in once the pack is imported
                },
            };
            c.Rebuild();
            return c;
        }

#if UNITY_EDITOR
        private void OnValidate() => _flat = null;
#endif
    }
}
