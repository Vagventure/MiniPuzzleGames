using PuzzleGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleGame.UI
{
    /// <summary>
    /// The menu scene has two pages, built entirely from code:
    ///
    ///   START page   — title + PLAY / OPTIONS / QUIT (the game's front door)
    ///   LEVEL SELECT — BACK / CONTINUE / OPTIONS top bar over the scrollable level grid
    ///
    ///   Start.PLAY        -> open Level Select
    ///   LevelSelect.BACK   -> back to Start
    ///   LevelSelect.CONTINUE -> highest unlocked global level (jumps straight into gameplay)
    ///   a level button     -> jump straight to it (if unlocked / testing-unlocked)
    ///   OPTIONS (either page) -> sound toggle + reset progress
    ///   Start.QUIT         -> exit
    ///
    /// Unlock rules (incl. testing overrides) live in <see cref="GameConfig"/> —
    /// set <c>unlockAllLevels</c> or <c>playableLevelsPerChapter</c> on
    /// <c>Assets/_Shared/Resources/GameConfig.asset</c>.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private GameObject _startPanel;
        private GameObject _levelSelectPanel;
        private GameObject _optionsPanel;

        private Text _continueLabel;
        private Text _soundLabel;
        private RectTransform _body;

        private void Start()
        {
            SaveManager.EnsureInitialised();
            BuildUi();
            ShowStart();
        }

        // ------------------------------------------------------------------
        // Button handlers
        // ------------------------------------------------------------------

        public void OnPlay() => ShowLevelSelect();

        public void OnBackToStart() => ShowStart();

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
        // Page switching
        // ------------------------------------------------------------------

        private void ShowStart()
        {
            _levelSelectPanel.SetActive(false);
            _optionsPanel.SetActive(false);
            _startPanel.SetActive(true);
        }

        private void ShowLevelSelect()
        {
            _startPanel.SetActive(false);
            _optionsPanel.SetActive(false);
            _levelSelectPanel.SetActive(true);
            RefreshContinueLabel();
            RebuildGrid();
        }

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

        // ------------------------------------------------------------------
        // UI construction
        // ------------------------------------------------------------------

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

            BuildStartPanel(root);
            BuildLevelSelectPanel(root);
            BuildOptionsPanel(root); // drawn last -> always on top of whichever page is active
        }

        private void BuildStartPanel(Transform root)
        {
            _startPanel = SharedUI.NewUiObject("StartPanel", root);
            SharedUI.Stretch(_startPanel.GetComponent<RectTransform>());

            var bg = SharedUI.NewUiObject("Bg", _startPanel.transform);
            SharedUI.Stretch(bg.GetComponent<RectTransform>());
            bg.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.16f, 1f);

            var title = SharedUI.NewUiObject("Title", _startPanel.transform);
            var tt = title.AddComponent<Text>();
            tt.font = SharedUI.LegacyFont();
            tt.text = "PUZZLE COLLECTION";
            tt.fontSize = 66;
            tt.alignment = TextAnchor.MiddleCenter;
            tt.color = Color.white;
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.1f, 0.72f);
            trt.anchorMax = new Vector2(0.9f, 0.88f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            var playBtn = SharedUI.MakeButton("PlayButton", _startPanel.transform, "PLAY", OnPlay);
            PlaceButtonColumn(playBtn.GetComponent<RectTransform>(), 0);

            var optionsBtn = SharedUI.MakeButton("OptionsButton", _startPanel.transform, "OPTIONS", OnOptions);
            PlaceButtonColumn(optionsBtn.GetComponent<RectTransform>(), 1);
            optionsBtn.GetComponent<Image>().color = new Color(0.30f, 0.34f, 0.42f, 1f);

            var quitBtn = SharedUI.MakeButton("QuitButton", _startPanel.transform, "QUIT", OnQuit);
            PlaceButtonColumn(quitBtn.GetComponent<RectTransform>(), 2);
            quitBtn.GetComponent<Image>().color = new Color(0.55f, 0.25f, 0.25f, 1f);
        }

        // 3 buttons stacked vertically, centred on screen
        private static void PlaceButtonColumn(RectTransform rt, int row)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(560, 130);
            rt.anchoredPosition = new Vector2(0, 120 - row * 170);
        }

        private void BuildLevelSelectPanel(Transform root)
        {
            _levelSelectPanel = SharedUI.NewUiObject("LevelSelectPanel", root);
            SharedUI.Stretch(_levelSelectPanel.GetComponent<RectTransform>());
            _levelSelectPanel.SetActive(false);

            var bg = SharedUI.NewUiObject("Bg", _levelSelectPanel.transform);
            SharedUI.Stretch(bg.GetComponent<RectTransform>());
            bg.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.16f, 1f);

            var bar = SharedUI.NewUiObject("TopBar", _levelSelectPanel.transform);
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

            var backBtn = SharedUI.MakeButton("BackButton", bar.transform, "BACK", OnBackToStart);
            PlaceInRow(backBtn.GetComponent<RectTransform>(), 0);
            backBtn.GetComponent<Image>().color = new Color(0.30f, 0.34f, 0.42f, 1f);

            var continueBtn = SharedUI.MakeButton("ContinueButton", bar.transform, "CONTINUE", OnContinue);
            PlaceInRow(continueBtn.GetComponent<RectTransform>(), 1);
            _continueLabel = continueBtn.GetComponentInChildren<Text>();

            var optionsBtn = SharedUI.MakeButton("OptionsButton", bar.transform, "OPTIONS", OnOptions);
            PlaceInRow(optionsBtn.GetComponent<RectTransform>(), 2);
            optionsBtn.GetComponent<Image>().color = new Color(0.30f, 0.34f, 0.42f, 1f);

            // scrolling body (below the top bar)
            var bodyGo = SharedUI.NewUiObject("Body", _levelSelectPanel.transform);
            _body = bodyGo.GetComponent<RectTransform>();
            _body.anchorMin = new Vector2(0, 0);
            _body.anchorMax = new Vector2(1, 1);
            _body.offsetMin = new Vector2(0, 0);
            _body.offsetMax = new Vector2(0, -280); // reserve top bar height
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

            var close = SharedUI.MakeButton("CloseOptionsButton", _optionsPanel.transform, "BACK", OnOptions);
            CentreButton(close.GetComponent<RectTransform>(), -190);

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
