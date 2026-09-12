# Yes Chef! — Unity Dev Test (Tentworks Interactive)

A 3-minute single-kitchen cooking game built with Unity 6 + C#. Top-down 3D view, no camera movement.
The kitchen layout and UI are built in the Unity editor — open `Assets/Scenes/GameScene.unity`,
press Play, and press **Start Game**.

## How to run
1. Open the project in Unity 6000.4+ (Input System package required, already in `Packages/manifest.json`).
2. Open `Assets/Scenes/GameScene.unity` and press Play.
3. Click **Start Game** on the controls panel.

## Controls
- **WASD / Arrows** — move the chef.
- **E** — context interact: take from fridge, place on table/stove, serve at a window, trash held item.
- **Q** — cycle fridge selection / pick up chopped vegetables or cooked meat.
- **1 / 2 / 3** (or HUD buttons) — choose fridge ingredient: Vegetable / Cheese / Meat.
- **P / Esc** — pause. HUD buttons: Pause, Quit.

## Rules (per spec)
- 3-minute timer, max 4 active orders, game starts with 4 open orders.
- Orders: 50/50 two or three ingredients, fully random (duplicates allowed).
- Vegetable: chop 2s on the Table (20 pts). Cheese: ready immediately (10 pts). Meat: cook 6s on the Stove, 2 slots (30 pts).
- One item held at a time. Stove/table keep working while you walk away. Trash discards the held item.
- Order score = sum of ingredient values − whole seconds the order was open (floored, can be negative).
- Completed window respawns a new order after 5s. Score popup (`+17` / `-6`) shows at the window and on its HUD card, then fades.
- High score persists between sessions (`PlayerPrefs`) and a **New High Score!** banner shows on the game-over panel.

## Architecture
- `Assets/Scripts/Core/` — `GameConstants` (all tuning in one place), `GameState`/`IngredientType`/`IngredientState` enums.
- `Assets/Scripts/Data/` — immutable `IngredientItem` struct; `OrderData` (requirements, fulfilment, `sum − ⌊seconds⌋` scoring).
- `Assets/Scripts/Player/` — `PlayerController` (Input System, clamped to kitchen), `PlayerInteractor`
  (cached proximity search, E/Q routing, prompt text), `Interactable` base.
- `Assets/Scripts/Stations/` — `Refrigerator`, `ChoppingTable`, `Stove`, `Trash`, and `CustomerWindow` (order lifecycle: spawn → age → serve → score → 5s respawn, with 3D status lamp and floating score popup).
- `Assets/Scripts/Managers/GameManager.cs` — state machine (NotStarted/Playing/Paused/GameOver),
  3-minute timer, score + high-score persistence; UI observes it through events.
- `Assets/Scripts/UI/GameHUD.cs` — drives all screen-space HUD elements (score, timer, order cards,
  modals). UI hierarchy is built in the editor; the script handles logic only.

## Design decisions
- New Input System only (the project's active input handling); keyboard controls, PC only.
- One held item enforced by `PlayerController.TryGive/TryTake`, so stations never duplicate or lose items.
- Q (Alternate) separates "place" from "collect" so walking away from the stove/table is safe and readable.
- Fridge uses a selected-ingredient model (1/2/3 + E) so one station serves all three ingredients with clear prompts.
- Order ageing and respawns tick in `CustomerWindow`; scoring lives in `OrderData.CalculateScore()` so the
  rule is implemented once and previewed live on the HUD cards.
