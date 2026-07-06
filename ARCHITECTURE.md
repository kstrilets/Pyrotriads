# Pyrotriads — Architecture

A match-and-cascade pyramid card game. The codebase is split into decoupled layers so
designers and tech-artists can tune values and drop in art/audio/VFX **without touching code**,
and so new features slot in cleanly.

## Layers

```
Assets/PyramidCards/Scripts/
├── Core/        Pure C# game logic — no UnityEngine dependency, unit-testable.
│   ├── CardData.cs      One card (suit, number, face).
│   ├── ComboTypes.cs    Combo / CellFlags / EvalResult + ScoringRules (plain data).
│   ├── Evaluator.cs     Stateless board scoring. Takes ScoringRules, returns combos.
│   └── Deck.cs          Deck build / shuffle / combo-free deal.
│
├── Config/      ScriptableObjects — the designer/artist tuning surface.
│   ├── GameRulesConfig.cs   Deck size, scoring tables, triads, cascade timings.
│   ├── LevelSet.cs          Ordered levels + endless-overflow rule.
│   ├── ThemePalette.cs      Every colour in one asset (re-skin without code).
│   ├── CardVisualTheme.cs   Card layout + OPTIONAL sprite slots (empty = flat colour).
│   ├── AudioLibrary.cs      GameSfx → AudioClip map (empty = silent).
│   └── VfxLibrary.cs        GameVfx → prefab map (empty = no effect).
│
├── Game/        State + rules orchestration.
│   ├── GameManager.cs   All state & rules. Holds NO view reference — raises C# events.
│   ├── ShopService.cs   Between-level "Workshop" offer rolling / purchase validation.
│   └── GameEvents.cs    Event payloads: RenderOpts, ModalRequest, CascadeStep.
│
├── Services/    Event listeners (add/remove freely; game runs without them).
│   ├── AudioService.cs  Maps game events → sounds.
│   └── VfxService.cs    Spawns VFX prefabs at screen positions (driven by the view).
│
├── View/        Runtime-built uGUI. Reads config; subscribes to GameManager events.
│   ├── GameView.cs      Orchestrator: canvas, HUD, board, buttons, wires events.
│   ├── CardView.cs      One card view (click = flip, drag = swap) + clear VFX hook.
│   ├── ModalView.cs     Win/lose dialog.
│   ├── ShopView.cs      Workshop screen.
│   └── UIFactory.cs     uGUI building helpers (colour/sprite/font aware).
│
└── Bootstrap/
    └── PyramidBootstrap.cs   Composition root: builds & wires everything. The only
                              component you place by hand.
```

## Decoupling contract

- **Logic → View: events only.** `GameManager` has zero references to any view or service. It
  raises `RenderRequested`, `ModalRequested`, `CascadeResolved`, `ShopOpened`, etc. Any number of
  listeners (UI, audio, VFX, future analytics) can subscribe.
- **View → Logic: read state + call actions.** Views read public state (`gm.score`, `gm.grid`…)
  and call intent methods (`Flip`, `Swap`, `TryBuy`, `Advance`). They never reach into rules.
- **Config is data.** Nothing hard-codes tuning. `GameManager` reads `GameRulesConfig`/`LevelSet`;
  views read `ThemePalette`/`CardVisualTheme`; services read `AudioLibrary`/`VfxLibrary`.

## Running it

New scene ▸ empty GameObject ▸ add **PyramidBootstrap** ▸ Play. With no assets assigned it uses
code defaults (identical to the original prototype). Assign assets to tune.

## For designers / tech-artists

Create tuning assets via **Assets ▸ Create ▸ Pyrotriads ▸ …** and drop them onto the
`PyramidBootstrap` component:

| Want to…                    | Do this                                                            |
|-----------------------------|--------------------------------------------------------------------|
| Change targets / move budgets | Edit a **Level Set** asset.                                       |
| Rebalance scoring / cascade | Edit a **Game Rules** asset.                                        |
| Re-skin colours             | Edit a **Theme Palette** asset.                                     |
| Add card art                | Fill sprite slots on a **Card Visual Theme** asset.                 |
| Add sounds                  | Add entries to an **Audio Library** asset (pick the `GameSfx`).     |
| Add particle/UI effects     | Add entries to a **VFX Library** asset (pick the `GameVfx`, prefab).|

## Extending

- **New sound hook:** add a `GameSfx` value, map it in `AudioLibrary`, react to an event in
  `AudioService`.
- **New effect:** add a `GameVfx` value, map it in `VfxLibrary`; trigger via `VfxService.Play`
  (or the card clear hook in `CardView`).
- **New combo / rule:** extend `Evaluator` (pure, test it directly) and any needed `ScoringRules`
  field on `GameRulesConfig`.
- **New UI panel:** build it like `ModalView`/`ShopView` and subscribe to a `GameManager` event.
