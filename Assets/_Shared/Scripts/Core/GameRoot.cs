using UnityEngine;

namespace PuzzleGame.Core
{
    /// <summary>
    /// Persistent "systems" object. Spawns itself before the first scene loads (so pressing
    /// Play inside any level scene still boots the shared framework) and survives scene loads.
    ///
    /// Holds: <see cref="LevelFlowController"/> + <see cref="UI.SharedUI"/>.
    /// </summary>
    public class GameRoot : MonoBehaviour
    {
        private static GameRoot _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;

            var catalog = Resources.Load<LevelCatalog>("LevelCatalog");
            if (catalog == null)
            {
                Debug.LogWarning("[GameRoot] LevelCatalog not found in Resources — using built-in defaults. " +
                                 "Run  PuzzleGame ▸ Setup ▸ Create / Refresh Level Catalog  to author it.");
                catalog = LevelCatalog.BuildDefault();
            }

            var go = new GameObject("[GameRoot]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<GameRoot>();

            var flow = go.AddComponent<LevelFlowController>();
            flow.Configure(catalog);

            var uiGo = new GameObject("[SharedUI]", typeof(RectTransform));
            uiGo.transform.SetParent(go.transform, false);
            uiGo.AddComponent<UI.SharedUI>();

            Application.targetFrameRate = 300; // both original games asked for this
        }
    }
}
