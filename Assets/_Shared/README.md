# _Shared — merged-game framework

Turns the separate mini-games into one continuous run:
**Connect Balls (60) → Find The Hole (51) → Rolling Maze (0, add later)** = one global 1..N sequence.

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
| `MainMenuController` | `Scripts/UI/MainMenuController.cs` | the one combined menu: CONTINUE / OPTIONS / QUIT over the level grid |
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
`Find The Hole/Scenes/mainMenu.unity`) are no longer used by the flow and can be deleted.

The two games' menu scripts (`ConnectBalls.Menus`, `GameMenus`) were changed only to **delegate**
next-level / restart / main-menu / unlock to `LevelFlowController` when it is present; with no
controller they behave exactly as before, so each pack still runs standalone.

## Adding Rolling Maze later

1. Import the pack under `Assets/Rolling Maze/`.
2. Open `LevelCatalog.asset`, set the Rolling Maze chapter's `sceneFolder` + `levelCount`
   (or `explicitSceneNames`).
3. `PuzzleGame ▸ Setup ▸ Sync Build Settings From Catalog`.
   Nothing in the flow code changes.

## Still on the plan (not done here)

- Phase 3: rename the 111 level scenes to unique names (currently loaded by path — fine as-is).
- Phase 5: strip each level scene's own pause / level-complete Canvas and move those panels into `SharedUI`.
- Phase 6: `BackgroundController` driving the per-chapter background.
