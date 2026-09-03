using PuzzleGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleGame.UI
{
    /// <summary>
    /// The single combined main menu for every mini-game. It IS the level select:
    /// a top bar (CONTINUE / OPTIONS / QUIT) over one scrollable grid of every level in the
    /// <see cref="LevelCatalog"/>, grouped by mini-game.
    ///
    ///   CONTINUE -> highest unlocked global level (Connect Balls, then Find The Hole, then …)
    ///   a level   -> jump straight to it (if unlocked / testing-unlocked)
    ///   OPTIONS   -> sound toggle + reset progress
    ///
    /// Unlock rules (incl. testing overrides) live in <see cref="GameConfig"/> —
    /// set <c>unlockAllLevels</c> or <c>playableLevelsPerChapter</c> on
    /// <c>Assets/_Shared/Resources/GameConfig.asset</c>.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private Text _continueLabel;
        private GameObject _optionsPanel;
        private Text _soundLabel;
        private RectTransform _body;

        private void Start()
        {
            SaveManager.EnsureInitialised();
            BuildUi();
        }

        // ------------------------------------------------------------------
        // Button handlers
        // ------------------------------------------------------------------

        public void OnContinue()
        {
            var flow = LevelFlowController.Instance;
            if (flow == null)
            {
                Debug.LogError("[MainMenu] LevelFlowController missing — GameRoot should self-spawn. " +
                               "Check the console for compile errors.");
                return;
            }
            flow.Resume();
        }

        public void OnOptions() => _optionsPanel.SetActive(!_optionsPanel.activeSelf);

        public void OnToggleSound()
        {
            AudioListener.volume = AudioListener.volume > 0.5f ? 0f : 1f;
            RefreshSoundLabel();
        }

        public void OnResetProgress()
        {
            SaveManager.ResetProgress();
            RefreshContinueLabel();
            RebuildGrid();
        }

        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------
        // UI construction
        // ------------------------------------------------------------------

        private void RefreshContinueLabel()
        {
            _continueLabel.text = SaveManager.HighestUnlocked > 1 ? "CONTINUE" : "PLAY";
        }

        private void RefreshSoundLabel()
        {
            _soundLabel.text = AudioListener.volume > 0.5f ? "SOUND: ON" : "SOUND: OFF";
        }

        private void RebuildGrid()
        {
            if (_body != null) LevelSelectView.Build(_body);
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("MainMenuCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            SharedUI.EnsureEventSystemPublic();

            var root = canvasGo.transform;

            // background
            var bg = SharedUI.NewUiObject("Bg", root);
            SharedUI.Stretch(bg.GetComponent<RectTransform>());
            bg.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.16f, 1f);

            BuildTopBar(root);

            // scrolling body (below the top bar)
            var bodyGo = SharedUI.NewUiObject("Body", root);
            _body = bodyGo.GetComponent<RectTransform>();
            _body.anchorMin = new Vector2(0, 0);
            _body.anchorMax = new Vector2(1, 1);
            _body.offsetMin = new Vector2(0, 0);
            _body.offsetMax = new Vector2(0, -280); // reserve top bar height

            LevelSelectView.Build(_body);

            BuildOptionsPanel(root);

            RefreshContinueLabel();
        }

        private void BuildTopBar(Transform root)
        {
            var bar = SharedUI.NewUiObject("TopBar", root);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 1);
            brt.anchorMax = new Vector2(1, 1);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.sizeDelta = new Vector2(0, 280);
            brt.anchoredPosition = Vector2.zero;
            bar.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.10f, 1f);

            var title = SharedUI.NewUiObject("Title", bar.transform);
            var tt = title.AddComponent<Text>();
            tt.font = SharedUI.LegacyFont();
            tt.text = "SELECT A LEVEL";
            tt.fontSize = 52;
            tt.alignment = TextAnchor.MiddleCenter;
            tt.color = Color.white;
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 1);
            trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.sizeDelta = new Vector2(0, 100);
            trt.anchoredPosition = new Vector2(0, -20);

            var continueBtn = SharedUI.MakeButton("ContinueButton", bar.transform, "CONTINUE", OnContinue);
            PlaceInRow(continueBtn.GetComponent<RectTransform>(), 0);
            _continueLabel = continueBtn.GetComponentInChildren<Text>();

            var optionsBtn = SharedUI.MakeButton("OptionsButton", bar.transform, "OPTIONS", OnOptions);
            PlaceInRow(optionsBtn.GetComponent<RectTransform>(), 1);
            optionsBtn.GetComponent<Image>().color = new Color(0.30f, 0.34f, 0.42f, 1f);

            var quitBtn = SharedUI.MakeButton("QuitButton", bar.transform, "QUIT", OnQuit);
            PlaceInRow(quitBtn.GetComponent<RectTransform>(), 2);
            quitBtn.GetComponent<Image>().color = new Color(0.55f, 0.25f, 0.25f, 1f);
        }

        // 3 buttons in a row across the bottom of the top bar
        private static void PlaceInRow(RectTransform rt, int col)
        {
            const int count = 3;
            rt.anchorMin = new Vector2((float)col / count, 0f);
            rt.anchorMax = new Vector2((float)(col + 1) / count, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(16, 16);
            rt.offsetMax = new Vector2(-16, 16 + 120);
        }

        private void BuildOptionsPanel(Transform root)
        {
            _optionsPanel = SharedUI.NewUiObject("OptionsPanel", root);
            SharedUI.Stretch(_optionsPanel.GetComponent<RectTransform>());
            _optionsPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.9f);

            var sound = SharedUI.MakeButton("SoundButton", _optionsPanel.transform, "SOUND: ON", OnToggleSound);
            CentreButton(sound.GetComponent<RectTransform>(), 150);
            _soundLabel = sound.GetComponentInChildren<Text>();

            var reset = SharedUI.MakeButton("ResetButton", _optionsPanel.transform, "RESET PROGRESS", OnResetProgress);
            CentreButton(reset.GetComponent<RectTransform>(), -20);
            reset.GetComponent<Image>().color = new Color(0.8f, 0.3f, 0.25f, 1f);

            var back = SharedUI.MakeButton("BackButton", _optionsPanel.transform, "BACK", OnOptions);
            CentreButton(back.GetComponent<RectTransform>(), -190);

            RefreshSoundLabel();
            _optionsPanel.SetActive(false);
        }

        private static void CentreButton(RectTransform rt, float y)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(560, 130);
            rt.anchoredPosition = new Vector2(0, y);
        }
    }
}
