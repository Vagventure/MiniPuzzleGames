using System;
using System.Collections;
using PuzzleGame.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PuzzleGame.UI
{
    /// <summary>
    /// The one UI that lives on top of every mini-game. Built entirely from code so there is
    /// no prefab to author and nothing per-scene to wire.
    ///
    /// Provides:
    ///   * a light persistent HUD (chapter + level label, Menu button, conditional Skip button)
    ///   * a full-screen fade that covers every level / mini-game transition
    ///   * the chapter hand-off intro card and the "you finished everything" card
    ///
    /// The HUD and Skip button only appear while a catalog level is loaded — never on the menu.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public class SharedUI : MonoBehaviour
    {
        public static SharedUI Instance { get; private set; }

        [SerializeField] private bool showHud = true;

        private Canvas _canvas;
        private Text _levelLabel;
        private GameObject _hudRoot;
        private GameObject _skipButton;
        private bool _skipShown;

        private Image _fade;
        private bool _pendingUncover;

        // intro / complete card
        private GameObject _cardRoot;
        private Text _cardTitle;
        private Text _cardBody;
        private Text _cardButtonLabel;
        private Action _cardAction;

        // in-level "you earned N stars" strip, for games with no star UI of their own
        private GameObject _starsRoot;
        private Text[] _stars;

        private static float FadeDuration => Mathf.Max(0f, GameConfig.Instance.fadeDuration);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildCanvas();
            BuildFade();
            BuildHud();
            BuildStars();
            BuildCard();
            HideCard();

            SceneManager.sceneLoaded += OnSceneLoadedRaw;

            if (LevelFlowController.Instance != null)
            {
                LevelFlowController.Instance.LevelLoaded += OnLevelLoaded;
                var cur = LevelFlowController.Instance.Current;
                if (cur.IsValid) OnLevelLoaded(cur);
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoadedRaw;
            if (LevelFlowController.Instance != null)
                LevelFlowController.Instance.LevelLoaded -= OnLevelLoaded;
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // Skip button: only inside a level, and only after enough failed attempts.
            var f = LevelFlowController.Instance;
            bool show = f != null && f.CurrentGlobalIndex >= 1 && f.SkipAvailable;
            if (show != _skipShown && _skipButton != null)
            {
                _skipShown = show;
                _skipButton.SetActive(show);
            }
        }

        // ------------------------------------------------------------------
        // Public API used by LevelFlowController
        // ------------------------------------------------------------------

        /// <summary>Fade to opaque, run <paramref name="load"/> (the scene load), then fade back
        /// in once the new scene is up. Nothing from the old scene shows through the swap.</summary>
        public void CoverThenLoad(Action load)
        {
            StopAllCoroutines();
            StartCoroutine(CoCoverThenLoad(load));
        }

        public void ShowChapterIntro(string finishedChapter, string nextChapter, Action onContinue)
        {
            _cardTitle.text = string.IsNullOrEmpty(finishedChapter)
                ? "GET READY"
                : finishedChapter.ToUpperInvariant() + " COMPLETE!";
            _cardBody.text = "Next up:\n" + (nextChapter ?? "").ToUpperInvariant();
            _cardButtonLabel.text = "CONTINUE";
            ShowCard(onContinue);
        }

        public void ShowAllComplete(Action onMenu)
        {
            _cardTitle.text = "ALL LEVELS COMPLETE!";
            _cardBody.text = "Thanks for playing.";
            _cardButtonLabel.text = "MAIN MENU";
            ShowCard(onMenu);
        }

        /// <summary>Show a 3-star strip over the current level's own "level complete" screen.
        /// Only used for mini-games that have no star UI of their own (Connect Balls); Find The
        /// Hole and Rolling Maze draw their own.</summary>
        public void ShowLevelCompleteStars(int earned, GameId game)
        {
            if (game != GameId.ConnectBalls) return;
            StartCoroutine(CoShowStars(Mathf.Clamp(earned, 0, 3)));
        }

        private IEnumerator CoShowStars(int earned)
        {
            // wait for the game's own "LEVEL COMPLETE" panel to pop in (~1s Invoke)
            yield return new WaitForSecondsRealtime(0.9f);
            for (int i = 0; i < _stars.Length; i++)
            {
                bool on = i < earned;
                _stars[i].color = on ? new Color(1f, 0.82f, 0.15f, 1f)
                                     : new Color(1f, 1f, 1f, 0.18f);
                _stars[i].fontSize = on ? 132 : 104;
            }
            _starsRoot.SetActive(true);
            _starsRoot.transform.SetAsLastSibling();
            _fade.transform.SetAsLastSibling();
        }

        private void HideStars()
        {
            if (_starsRoot != null) _starsRoot.SetActive(false);
        }

        public void SetHudVisible(bool visible)
        {
            showHud = visible;
            if (_hudRoot != null) _hudRoot.SetActive(visible && InLevel);
        }

        // ------------------------------------------------------------------
        // Transitions
        // ------------------------------------------------------------------

        private IEnumerator CoCoverThenLoad(Action load)
        {
            _fade.transform.SetAsLastSibling();
            _fade.raycastTarget = true;
            yield return Fade(1f);
            _pendingUncover = true;
            load?.Invoke();
            // uncover is triggered from OnSceneLoadedRaw once the new scene finishes loading
        }

        private IEnumerator Fade(float target)
        {
            float dur = FadeDuration;
            Color c = _fade.color;
            float start = c.a;
            if (dur <= 0f)
            {
                c.a = target;
                _fade.color = c;
            }
            else
            {
                for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
                {
                    c.a = Mathf.Lerp(start, target, t / dur);
                    _fade.color = c;
                    yield return null;
                }
                c.a = target;
                _fade.color = c;
            }
            _fade.raycastTarget = target > 0.01f;
        }

        private void OnSceneLoadedRaw(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;

            DedupeEventSystems();

            bool inLevel = InLevel;
            if (_hudRoot != null) _hudRoot.SetActive(showHud && inLevel);
            HideStars();
            if (!inLevel) HideCard();

            if (_pendingUncover)
            {
                _pendingUncover = false;
                StopAllCoroutines();
                StartCoroutine(Fade(0f));
            }
        }

        private void OnLevelLoaded(LevelCatalog.LevelRef level)
        {
            HideCard();
            HideStars();
            if (_levelLabel != null)
                _levelLabel.text = level.IsValid
                    ? $"{level.ChapterName}   {level.ChapterLevelNumber}/{level.ChapterLevelCount}"
                    : "";
            if (_hudRoot != null) _hudRoot.SetActive(showHud && level.IsValid);
        }

        private static bool InLevel =>
            LevelFlowController.Instance != null && LevelFlowController.Instance.CurrentGlobalIndex >= 1;

        // ------------------------------------------------------------------
        // EventSystem: keep exactly one
        // ------------------------------------------------------------------

        private static void DedupeEventSystems()
        {
            var all = FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 1; i < all.Length; i++)
                all[i].gameObject.SetActive(false);
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            // NOT DontDestroyOnLoad — it should die with the scene that needed it, so the
            // next scene's own EventSystem takes over cleanly.
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        /// <summary>Lets a scene without its own EventSystem (e.g. the generated menu) get one.</summary>
        public static void EnsureEventSystemPublic() => EnsureEventSystem();

        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        private void BuildCanvas()
        {
            _canvas = gameObject.GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 5000; // above every scene canvas

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();
        }

        private void BuildFade()
        {
            var go = NewUiObject("Fade", _canvas.transform);
            Stretch(go.GetComponent<RectTransform>());
            _fade = go.AddComponent<Image>();
            _fade.color = new Color(0f, 0f, 0f, 0f);
            _fade.raycastTarget = false;
        }

        private void BuildStars()
        {
            _starsRoot = NewUiObject("LevelCompleteStars", _canvas.transform);
            var rt = _starsRoot.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(560, 200);
            rt.anchoredPosition = Vector2.zero; // screen centre

            _stars = new Text[3];
            float[] x = { -190f, 0f, 190f };
            float[] y = { 0f, 34f, 0f }; // classic slight arc
            for (int i = 0; i < 3; i++)
            {
                var s = NewUiObject("Star" + i, _starsRoot.transform);
                var t = s.AddComponent<Text>();
                t.font = LegacyFont();
                t.text = "★"; // ★
                t.fontSize = 120;
                t.alignment = TextAnchor.MiddleCenter;
                t.raycastTarget = false;
                t.color = new Color(1f, 1f, 1f, 0.18f);
                var srt = s.GetComponent<RectTransform>();
                srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
                srt.pivot = new Vector2(0.5f, 0.5f);
                srt.sizeDelta = new Vector2(170, 170);
                srt.anchoredPosition = new Vector2(x[i], y[i]);
                _stars[i] = t;
            }
            _starsRoot.SetActive(false);
        }

        private void BuildHud()
        {
            _hudRoot = NewUiObject("HUD", _canvas.transform);
            Stretch(_hudRoot.GetComponent<RectTransform>());

            var label = NewUiObject("LevelLabel", _hudRoot.transform);
            _levelLabel = label.AddComponent<Text>();
            _levelLabel.font = LegacyFont();
            _levelLabel.fontSize = 34;
            _levelLabel.alignment = TextAnchor.MiddleCenter;
            _levelLabel.color = new Color(1f, 1f, 1f, 0.85f);
            _levelLabel.raycastTarget = false;
            var lrt = label.GetComponent<RectTransform>();
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 1f);
            lrt.pivot = new Vector2(0.5f, 1f);
            lrt.sizeDelta = new Vector2(700, 70);
            lrt.anchoredPosition = new Vector2(0, -18);

            var menu = MakeButton("MenuButton", _hudRoot.transform, "MENU", () =>
                LevelFlowController.Instance?.RequestMainMenu());
            var brt = menu.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(0f, 1f);
            brt.pivot = new Vector2(0f, 1f);
            brt.sizeDelta = new Vector2(180, 70);
            brt.anchoredPosition = new Vector2(24, -18);

            _skipButton = MakeButton("SkipButton", _hudRoot.transform, "SKIP", () =>
                LevelFlowController.Instance?.SkipLevel());
            _skipButton.GetComponent<Image>().color = new Color(0.55f, 0.45f, 0.15f, 1f);
            var srt = _skipButton.GetComponent<RectTransform>();
            srt.anchorMin = srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(1f, 1f);
            srt.sizeDelta = new Vector2(180, 70);
            srt.anchoredPosition = new Vector2(-24, -18);
            _skipButton.SetActive(false);

            _hudRoot.SetActive(false);
        }

        private void BuildCard()
        {
            _cardRoot = NewUiObject("ChapterCard", _canvas.transform);
            Stretch(_cardRoot.GetComponent<RectTransform>());
            var bg = _cardRoot.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.06f, 0.1f, 1f); // fully opaque — nothing bleeds through

            var title = NewUiObject("Title", _cardRoot.transform);
            _cardTitle = title.AddComponent<Text>();
            _cardTitle.font = LegacyFont();
            _cardTitle.fontSize = 64;
            _cardTitle.alignment = TextAnchor.MiddleCenter;
            _cardTitle.color = Color.white;
            var trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.1f, 0.58f);
            trt.anchorMax = new Vector2(0.9f, 0.78f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            var body = NewUiObject("Body", _cardRoot.transform);
            _cardBody = body.AddComponent<Text>();
            _cardBody.font = LegacyFont();
            _cardBody.fontSize = 40;
            _cardBody.alignment = TextAnchor.MiddleCenter;
            _cardBody.color = new Color(1f, 1f, 1f, 0.8f);
            var yrt = body.GetComponent<RectTransform>();
            yrt.anchorMin = new Vector2(0.1f, 0.4f);
            yrt.anchorMax = new Vector2(0.9f, 0.56f);
            yrt.offsetMin = yrt.offsetMax = Vector2.zero;

            var button = MakeButton("ContinueButton", _cardRoot.transform, "CONTINUE", () =>
            {
                var a = _cardAction;
                _cardAction = null;
                // keep the (opaque) card up; it is hidden once the next scene is loaded
                a?.Invoke();
            });
            _cardButtonLabel = button.GetComponentInChildren<Text>();
            var crt = button.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.24f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(420, 110);
            crt.anchoredPosition = Vector2.zero;
        }

        private void ShowCard(Action action)
        {
            if (EventSystem.current == null) EnsureEventSystem();
            HideStars();
            _cardAction = action;
            _cardRoot.SetActive(true);
            _cardRoot.transform.SetAsLastSibling();
            _fade.transform.SetAsLastSibling(); // fade always on top of the card
            Time.timeScale = 1f;
        }

        private void HideCard()
        {
            if (_cardRoot != null) _cardRoot.SetActive(false);
        }

        // ------------------------------------------------------------------
        // tiny UGUI factory (also used by MainMenuController / LevelSelectView)
        // ------------------------------------------------------------------

        internal static Font LegacyFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        internal static GameObject NewUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        internal static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        internal static GameObject MakeButton(string name, Transform parent, string label,
                                              UnityEngine.Events.UnityAction onClick)
        {
            var go = NewUiObject(name, parent);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.55f, 0.95f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);

            var textGo = NewUiObject("Text", go.transform);
            var t = textGo.AddComponent<Text>();
            t.font = LegacyFont();
            t.text = label;
            t.fontSize = 40;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            Stretch(textGo.GetComponent<RectTransform>());

            return go;
        }
    }
}
