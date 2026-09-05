# _Shared — merged-game framework

Turns the separate mini-games into one continuous run:
**Connect Balls (55) → Find The Hole (46) → Rolling Maze (46)** = one global 1..147 sequence,
played **interleaved 5 levels at a time** (CB 1-5, FTH 1-5, RM 1-5, CB 6-10, …) rather than one
huge block per game — see `LevelCatalog.interleaveBlockSize`. Each pack's first 5 levels (the
too-easy/tutorial ones) are cut by starting each chapter at `firstNumber: 6`.

The level-select grid numbers buttons with the **global** running number (1, 2, 3, 4, 5, 6, 7…
counting across every game) rather than each game's own local count — no more three separate
"1, 2, 3, 4, 5" sections. Each button still loads the correct underlying scene for that global
index; the section header above each run shows which game/local-range it maps to.

## One-time setup (in the Unity Editor)

`PuzzleGame ▸ Setup ▸ Run Full Setup`

That:
1. creates `Assets/_Shared/Resources/LevelCatalog.asset` (auto-counts the level scenes on disk),
2. creates `Assets/_Shared/Scenes/MainMenu.unity`,
3. rewrites **Build Settings** = MainMenu + every level scene, in catalog order.

Then open `Assets/_Shared/Scenes/MainMenu.unity` and press Play.

## How it fits together

| Piece | File | Role |
|---|---|---|
| `GameId` | `Scripts/Core/GameId.cs` | enum of mini-games |
| `LevelCatalog` | `Scripts/Core/LevelCatalog.cs` | ordered chapters → flat global level list; loads scenes **by path** (no renaming) |
| `LevelFlowController` | `Scripts/Core/LevelFlowController.cs` | current global level, next/prev/restart/menu, **chapter hand-off** |
| `GameRoot` | `Scripts/Core/GameRoot.cs` | self-spawns before first scene, `DontDestroyOnLoad`, holds the controller + shared UI |
| `SaveManager` | `Scripts/Core/SaveManager.cs` | single global progress pointer (`levelUnlock`), stars, save-version wipe |
| `SharedUI` | `Scripts/UI/SharedUI.cs` | persistent overlay: level label, Menu button, **Skip button**, chapter intro card, all-complete card |
| `MainMenuController` | `Scripts/UI/MainMenuController.cs` | two pages: **Start** (PLAY / OPTIONS / QUIT) and **Level Select** (BACK / CONTINUE / OPTIONS over the level grid) |
| `LevelSelectView` | `Scripts/UI/LevelSelectView.cs` | scrollable grid of **every** level, grouped by mini-game; locked levels dimmed |
| `GameConfig` | `Scripts/Core/GameConfig.cs` | testing flags (see below) — asset at `Resources/GameConfig.asset` |
| setup tool | `Scripts/Editor/PuzzleGameSetup.cs` | the menu above |

## Testing shortcuts — `Assets/_Shared/Resources/GameConfig.asset`

| Field | Effect |
|---|---|
| `unlockAllLevels` | every level in the menu is selectable/playable, saved progress ignored |
| `playableLevelsPerChapter` | `0` = whole chapter. `N` = only the first N levels of **each** mini-game are playable; finishing level N jumps straight to the next mini-game (fast hand-off testing) |
| `showChapterIntro` | toggle the "<chapter> complete — next up …" card |
| `skipAfterFailedAttempts` | SKIP appears on the HUD once the player has failed the current level this many times (default 5). `0` = always, `-1` = never. Only ever visible inside a level. |
| `fadeDuration` | seconds for the black fade that covers every level / mini-game transition (default 0.35) |

The old per-game main-menu scenes (`Connect Balls/Scenes/MainMenu.unity`,
`Find The Hole/Scenes/mainMenu.unity`, `Rolling Maze/Scenes/mainMenu.unity`) are no longer used
by the flow and can be deleted.

## Level order — `LevelCatalog.asset` → `Interleave Block Size`

- `0` = old behaviour: finish a mini-game's whole chapter before moving to the next.
- `N > 0` (default **5**): round-robin — N levels of Connect Balls, then N of Find The Hole,
  then N of Rolling Maze, back to Connect Balls, repeating until every chapter is exhausted (a
  chapter simply drops out of the rotation once it runs out, so 60 / 51 / 51 still finishes
  cleanly with no gaps).
- `ChapterLevelNumber` / `ChapterLevelCount` on each `LevelRef` are always the level's position
  within its *own* game (e.g. Find The Hole level 37 of 51), regardless of interleaving — so
  unlock/star/attempt tracking (all keyed by global index) and the level-select grid's per-run
  headers ("FIND THE HOLE (6–10 of 51)") stay correct either way.
- When interleaved, a chapter switch happens every few levels, so `LevelFlowController` skips the
  blocking "chapter complete, tap to continue" card and instead flashes a quick, non-blocking
  `SharedUI.FlashChapterBanner` during the fade — sequential mode (`0`) still gets the full card.
- Removing overly-easy early levels: use a chapter's `Explicit Scene Names` list instead of
  `Level Count` — list only the scene names you want, in order, and the rest are skipped. No code
  change needed.

The two games' menu scripts (`ConnectBalls.Menus`, `GameMenus`) were changed only to **delegate**
next-level / restart / main-menu / unlock to `LevelFlowController` when it is present; with no
controller they behave exactly as before, so each pack still runs standalone.

## Still on the plan (not done here)

- Phase 3: rename the level scenes to unique names (currently loaded by path — fine as-is).
- Phase 5: strip each level scene's own pause / level-complete Canvas and move those panels into `SharedUI`.
- Phase 6: `BackgroundController` driving the per-chapter background.
