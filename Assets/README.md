# Pyramid Cards — Unity port

A match-and-refill card puzzle on a 15-card pyramid (5-4-3-2-1), ported from an HTML prototype.
Face-down cards play as **colours**, face-up cards play as **numbers**. Click to flip, drag to swap;
matches clear, the 44-card deck refills the gaps, cascades chain up to x4. Triads drop **crystals**,
spent between levels in **the Workshop** on number-score multipliers ("gild the 3s: x3").

## Setup (Unity 6.4 / 6000.4)

1. In Unity Hub, create a new **empty project** with Unity 6.4 (any template; "Universal 2D" or
   "Universal 3D" both work — the game is pure uGUI and needs no pipeline features).
2. Copy the `Assets/PyramidCards` folder from this repo into your project's `Assets/`.
3. Open any scene (the default SampleScene is fine), create an **empty GameObject**, and add the
   **PyramidBootstrap** component to it.
4. Press **Play**. The whole interface is built at runtime — no prefabs, scenes, or packages required.

> **Input note:** the runtime EventSystem auto-selects its UI input module — `InputSystemUIInputModule`
> when the Input System package is active (New or Both), or the legacy `StandaloneInputModule` otherwise —
> via the `ENABLE_INPUT_SYSTEM` define. No Player Settings change is required for any of the three modes.

## Controls

- **Click** a card — flip it (colour ⇄ number). Costs 1 move.
- **Drag** a card onto another — swap them. Costs 1 move.
- Matches clear automatically and refill from the deck; cascades multiply score x2/x3/x4.

## Scoring (all knobs in `GameConfig.cs`)

| Combo | Reward |
|---|---|
| Colour run (face-down, adjacent, same suit) 2/3/4/5 | 2 / 5 / 9 / 14 |
| Number run (face-up, adjacent, same number) | sum of values + 0 / 3 / 7 / 12 |
| Number triad (apex over two below, all same number) | value x 6 + 6, + 1 crystal |
| Colour triad (same, face-down, same suit) | +2 moves (no points), + 1 crystal |
| Chain multiplier per cascade step | x1 → x4 (cap) |

The deck is exactly one of every card (4 suits x 11 numbers = 44): 15 dealt, 29 in the draw pile.
Depletion is the pressure — when the pile empties, cleared cells stay as holes.
Levels: 12 moves, targets 155 / 160 / 170 / 175 / 185, +10 per level after.

## Architecture

```
Assets/PyramidCards/Scripts/
  CardData.cs         card model (suit, number, face)
  GameConfig.cs       every tuning constant, level table, palette
  Evaluator.cs        pure scoring: runs, triads, mods — no Unity deps, unit-testable
  GameManager.cs      state + rules: deal, flip/swap, cascade coroutine, shop, win/lose
  CardView.cs         card visuals + pointer input (click = flip, drag = swap)
  GameUI.cs           entire interface built at runtime via UIFactory (uGUI, no prefabs)
  PyramidBootstrap.cs the one component you place by hand
```

`Evaluator` is deliberately engine-free — it mirrors the prototype's `evaluate()` and can be
covered by EditMode tests without touching the scene.

## Push to GitHub

```bash
cd <this folder>
git init
git add .
git commit -m "Pyramid Cards — initial Unity 6.4 port"
git branch -M main
git remote add origin https://github.com/<you>/pyramid-cards.git
git push -u origin main
```

The included `.gitignore` keeps `Library/`, `Temp/`, builds and IDE files out of the repo.
Commit `Assets/`, `Packages/` and `ProjectSettings/` from your Unity project once created.

## Porting notes / roadmap

- **Visual parity gaps** vs the HTML prototype, all intentional first-pass simplifications:
  card flip is instant (no 3D rotate tween), triad "threads" are glows instead of drawn triangles,
  and text uses the built-in legacy font. Natural next steps: TextMeshPro, sprite-based cards with
  rounded corners, DOTween (or coroutine tweens) for flips, a LineRenderer/UILine for triad threads.
- **Persistence:** crystals + mods live for the session only; add JSON save via
  `Application.persistentDataPath` to persist runs.
- **Tests:** `Evaluator` is ready for the Unity Test Framework (EditMode) — port the prototype's
  scoring test cases first.
