using System;
using System.Collections;
using PuzzleGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleGame.UI
{
    /// <summary>
    /// Builds one scrollable list of every level in the <see cref="LevelCatalog"/>, grouped by
    /// mini-game (chapter). Locked levels are dimmed and non-interactable; the level the player
    /// is currently on is highlighted green and auto-scrolled into view. Tapping an unlocked
    /// level calls <see cref="LevelFlowController.LoadLevel"/>.
    ///
    /// Pure code, no prefab. Call <see cref="Build"/> with a parent RectTransform to fill.
    /// </summary>
    public static class LevelSelectView
    {
        private const int Columns = 5;
        private static readonly Vector2 Cell = new Vector2(180, 180);
        private const float Spacing = 18f;

        private static readonly Color ColUnlocked = new Color(0.20f, 0.55f, 0.95f, 1f);
        private static readonly Color ColLocked   = new Color(0.22f, 0.24f, 0.30f, 1f);
        private static readonly Color ColCurrent  = new Color(0.20f, 0.78f, 0.35f, 1f); // green

        public static void Build(RectTransform parent, Action onAnyLevelChosen = null)
        {
            foreach (Transform c in parent) UnityEngine.Object.Destroy(c.gameObject);

            var flow = LevelFlowController.Instance;
            var catalog = flow != null ? flow.Catalog : null;
            if (catalog == null || catalog.TotalLevels == 0)
            {
                var warn = SharedUI.NewUiObject("Empty", parent);
                var wt = warn.AddComponent<Text>();
                wt.font = SharedUI.LegacyFont();
                wt.text = "No levels in the catalog.\nRun  PuzzleGame ▸ Setup ▸ Run Full Setup.";
                wt.alignment = TextAnchor.MiddleCenter;
                wt.fontSize = 34;
                wt.color = Color.white;
                SharedUI.Stretch(warn.GetComponent<RectTransform>());
                return;
            }

            int currentIndex = flow.ResumeTargetIndex;

            // ---- ScrollRect scaffold ----
            var scrollGo = SharedUI.NewUiObject("Scroll", parent);
            SharedUI.Stretch(scrollGo.GetComponent<RectTransform>());
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;

            var viewportGo = SharedUI.NewUiObject("Viewport", scrollGo.transform);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            SharedUI.Stretch(viewportRt);
            var vpImg = viewportGo.AddComponent<Image>();
            vpImg.color = new Color(1, 1, 1, 0.02f);
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;

            var contentGo = SharedUI.NewUiObject("Content", viewportGo.transform);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;

            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 24f;
            vlg.padding = new RectOffset(24, 24, 24, 48);

            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRt;
            scroll.content = contentRt;

            // ---- one section per chapter ----
            GameId currentGame = GameId.None;
            RectTransform grid = null;
            RectTransform currentButton = null;

            foreach (var lvl in catalog.Levels)
            {
                if (lvl.Game != currentGame)
                {
                    currentGame = lvl.Game;
                    AddHeader(contentRt, $"{lvl.ChapterName}   (1–{lvl.ChapterLevelCount})");
                    grid = AddGrid(contentRt);
                }

                var btn = AddLevelButton(grid, lvl, flow, onAnyLevelChosen, lvl.GlobalIndex == currentIndex);
                if (lvl.GlobalIndex == currentIndex) currentButton = btn;
            }

            // ---- auto-scroll to the current level ----
            if (currentButton != null)
            {
                var mover = scrollGo.AddComponent<ScrollToTarget>();
                mover.Init(scroll, currentButton);
            }
        }

        private static void AddHeader(RectTransform parent, string text)
        {
            var go = SharedUI.NewUiObject("ChapterHeader", parent);
            var t = go.AddComponent<Text>();
            t.font = SharedUI.LegacyFont();
            t.text = text.ToUpperInvariant();
            t.fontSize = 40;
            t.fontStyle = FontStyle.Bold;
            t.color = new Color(1f, 1f, 1f, 0.9f);
            t.alignment = TextAnchor.MiddleCenter;
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 64;
            le.preferredHeight = 64;
        }

        private static RectTransform AddGrid(RectTransform parent)
        {
            var go = SharedUI.NewUiObject("Grid", parent);
            var grid = go.AddComponent<GridLayoutGroup>();
            grid.cellSize = Cell;
            grid.spacing = new Vector2(Spacing, Spacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            grid.childAlignment = TextAnchor.UpperCenter;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return go.GetComponent<RectTransform>();
        }

        private static RectTransform AddLevelButton(RectTransform grid, LevelCatalog.LevelRef lvl,
                                                    LevelFlowController flow, Action onChosen, bool isCurrent)
        {
            bool unlocked = flow.IsLevelUnlocked(lvl.GlobalIndex);
            int stars = SaveManager.GetStars(lvl.GlobalIndex);

            var go = SharedUI.NewUiObject($"Level_{lvl.GlobalIndex}", grid);
            var img = go.AddComponent<Image>();
            img.color = !unlocked ? ColLocked : (isCurrent ? ColCurrent : ColUnlocked);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = unlocked;
            if (unlocked)
            {
                int global = lvl.GlobalIndex;
                btn.onClick.AddListener(() =>
                {
                    onChosen?.Invoke();
                    LevelFlowController.Instance.LoadLevel(global);
                });
            }

            if (isCurrent)
            {
                var outline = go.AddComponent<Outline>();
                outline.effectColor = Color.white;
                outline.effectDistance = new Vector2(4, -4);
            }

            var numGo = SharedUI.NewUiObject("Num", go.transform);
            var num = numGo.AddComponent<Text>();
            num.font = SharedUI.LegacyFont();
            num.text = unlocked ? lvl.ChapterLevelNumber.ToString() : "-";
            num.fontSize = unlocked ? 54 : 40;
            num.fontStyle = isCurrent ? FontStyle.Bold : FontStyle.Normal;
            num.alignment = TextAnchor.MiddleCenter;
            num.color = unlocked ? Color.white : new Color(1, 1, 1, 0.35f);
            SharedUI.Stretch(numGo.GetComponent<RectTransform>());

            if (unlocked && stars > 0)
            {
                var starGo = SharedUI.NewUiObject("Stars", go.transform);
                var st = starGo.AddComponent<Text>();
                st.font = SharedUI.LegacyFont();
                st.text = new string('*', stars);
                st.fontSize = 30;
                st.alignment = TextAnchor.LowerCenter;
                st.color = new Color(1f, 0.85f, 0.2f, 1f);
                var srt = starGo.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0, 0);
                srt.anchorMax = new Vector2(1, 0);
                srt.pivot = new Vector2(0.5f, 0f);
                srt.sizeDelta = new Vector2(0, 34);
                srt.anchoredPosition = new Vector2(0, 8);
            }

            return go.GetComponent<RectTransform>();
        }
    }

    /// <summary>
    /// One-shot helper: after the layout has settled, scrolls a <see cref="ScrollRect"/> so a
    /// target child sits roughly centred in the viewport, then removes itself.
    /// </summary>
    public class ScrollToTarget : MonoBehaviour
    {
        private ScrollRect _scroll;
        private RectTransform _target;

        public void Init(ScrollRect scroll, RectTransform target)
        {
            _scroll = scroll;
            _target = target;
        }

        private IEnumerator Start()
        {
            // wait two frames so VerticalLayoutGroup / ContentSizeFitter have run
            yield return null;
            yield return new WaitForEndOfFrame();

            if (_scroll == null || _target == null || _scroll.content == null || _scroll.viewport == null)
            {
                Destroy(this);
                yield break;
            }

            Canvas.ForceUpdateCanvases();

            var content = _scroll.content;
            var viewport = _scroll.viewport;

            float scrollable = content.rect.height - viewport.rect.height;
            if (scrollable > 1f)
            {
                var contentCorners = new Vector3[4];
                content.GetWorldCorners(contentCorners);
                float contentTopY = contentCorners[1].y;

                var targetCorners = new Vector3[4];
                _target.GetWorldCorners(targetCorners);
                float targetCenterY = (targetCorners[0].y + targetCorners[1].y) * 0.5f;

                float scaleY = Mathf.Approximately(content.lossyScale.y, 0f) ? 1f : content.lossyScale.y;
                float distFromTop = (contentTopY - targetCenterY) / scaleY;

                float norm = 1f - Mathf.Clamp01((distFromTop - viewport.rect.height * 0.5f) / scrollable);
                _scroll.verticalNormalizedPosition = norm;
            }

            Destroy(this);
        }
    }
}
