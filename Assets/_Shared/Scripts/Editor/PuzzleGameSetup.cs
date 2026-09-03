using System.Collections.Generic;
using System.IO;
using PuzzleGame.Core;
using PuzzleGame.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PuzzleGame.EditorTools
{
    /// <summary>
    /// One-click wiring for the merged project. Menu:  PuzzleGame ▸ Setup.
    ///
    /// 1. Create / refresh  Assets/_Shared/Resources/LevelCatalog.asset
    /// 2. Create            Assets/_Shared/Scenes/MainMenu.unity
    /// 3. Rewrite Build Settings  =  MainMenu  +  every level scene the catalog points at
    ///
    /// Re-runnable and non-destructive to the mini-game scenes themselves.
    /// </summary>
    public static class PuzzleGameSetup
    {
        private const string CatalogDir  = "Assets/_Shared/Resources";
        private const string CatalogPath = CatalogDir + "/LevelCatalog.asset";
        private const string ConfigPath  = CatalogDir + "/GameConfig.asset";
        private const string ScenesDir   = "Assets/_Shared/Scenes";
        private const string MenuScene   = ScenesDir + "/MainMenu.unity";

        [MenuItem("PuzzleGame/Setup/Run Full Setup", priority = 0)]
        public static void RunFullSetup()
        {
            var catalog = CreateOrRefreshCatalog();
            CreateConfigIfMissing();
            CreateMainMenuScene();
            SyncBuildSettings(catalog);
            EditorUtility.DisplayDialog("PuzzleGame",
                $"Setup complete.\n\n" +
                $"Catalog levels: {catalog.TotalLevels}\n" +
                $"Build Settings scenes: {EditorBuildSettings.scenes.Length}\n\n" +
                $"Open {MenuScene} and press Play.", "OK");
        }

        // ------------------------------------------------------------------
        [MenuItem("PuzzleGame/Setup/1. Create or Refresh Level Catalog", priority = 20)]
        public static LevelCatalog CreateOrRefreshCatalog()
        {
            Directory.CreateDirectory(CatalogDir);

            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            if (catalog == null)
            {
                // Seed from the built-in defaults, then persist.
                var seed = LevelCatalog.BuildDefault();
                catalog = Object.Instantiate(seed);
                catalog.name = "LevelCatalog";
                AssetDatabase.CreateAsset(catalog, CatalogPath);
                Object.DestroyImmediate(seed);
                Debug.Log($"[PuzzleGameSetup] Created {CatalogPath}");
            }

            // Auto-correct chapter level counts to what's actually on disk.
            foreach (var ch in catalog.Chapters)
            {
                if (string.IsNullOrEmpty(ch.sceneFolder)) continue;
                if (ch.explicitSceneNames != null && ch.explicitSceneNames.Length > 0) continue;

                int found = 0;
                for (int n = ch.firstNumber; ; n++)
                {
                    if (File.Exists($"{ch.sceneFolder}/{n}.unity")) found++;
                    else break;
                }
                if (found != ch.levelCount)
                {
                    Debug.Log($"[PuzzleGameSetup] {ch.displayName}: level count {ch.levelCount} -> {found}");
                    ch.levelCount = found;
                }
            }

            catalog.Rebuild();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        // ------------------------------------------------------------------
        [MenuItem("PuzzleGame/Setup/Create GameConfig (testing flags)", priority = 23)]
        public static void CreateConfigIfMissing()
        {
            Directory.CreateDirectory(CatalogDir);
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<GameConfig>();
                cfg.name = "GameConfig";
                AssetDatabase.CreateAsset(cfg, ConfigPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[PuzzleGameSetup] Created {ConfigPath} " +
                          "(set unlockAllLevels / playableLevelsPerChapter here for testing).");
            }
            Selection.activeObject = cfg;
        }

        // ------------------------------------------------------------------
        [MenuItem("PuzzleGame/Setup/2. Create MainMenu Scene", priority = 21)]
        public static void CreateMainMenuScene()
        {
            Directory.CreateDirectory(ScenesDir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.10f, 0.16f, 1f);
            cam.orthographic = true;
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0, 0, -10);

            var menuGo = new GameObject("MainMenu");
            menuGo.AddComponent<MainMenuController>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MenuScene);
            AssetDatabase.Refresh();
            Debug.Log($"[PuzzleGameSetup] Wrote {MenuScene}");
        }

        // ------------------------------------------------------------------
        [MenuItem("PuzzleGame/Setup/3. Sync Build Settings From Catalog", priority = 22)]
        public static void SyncBuildSettingsMenu() => SyncBuildSettings(CreateOrRefreshCatalog());

        public static void SyncBuildSettings(LevelCatalog catalog)
        {
            var list = new List<EditorBuildSettingsScene>();

            if (File.Exists(MenuScene))
                list.Add(new EditorBuildSettingsScene(MenuScene, true));
            else
                Debug.LogWarning($"[PuzzleGameSetup] {MenuScene} missing — run step 2 first.");

            var seen = new HashSet<string>();
            int missing = 0;
            foreach (var lvl in catalog.Levels)
            {
                var path = lvl.ScenePath;
                if (string.IsNullOrEmpty(path) || !seen.Add(path)) continue;

                if (File.Exists(path))
                    list.Add(new EditorBuildSettingsScene(path, true));
                else
                {
                    missing++;
                    Debug.LogWarning($"[PuzzleGameSetup] catalog scene not found on disk: {path}");
                }
            }

            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log($"[PuzzleGameSetup] Build Settings now has {list.Count} scenes " +
                      $"(1 menu + {list.Count - 1} levels, {missing} missing).");
        }

        // ------------------------------------------------------------------
        [MenuItem("PuzzleGame/Setup/Print Catalog", priority = 40)]
        public static void PrintCatalog()
        {
            var catalog = CreateOrRefreshCatalog();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"LevelCatalog — {catalog.TotalLevels} levels");
            GameId last = GameId.None;
            foreach (var l in catalog.Levels)
            {
                if (l.Game != last)
                {
                    last = l.Game;
                    sb.AppendLine($"  ── {l.ChapterName} ──");
                }
                sb.AppendLine($"  {l.GlobalIndex,4}  {l.Game,-14} {l.ChapterLevelNumber,3}/{l.ChapterLevelCount}  {l.ScenePath}");
            }
            Debug.Log(sb.ToString());
        }
    }
}
