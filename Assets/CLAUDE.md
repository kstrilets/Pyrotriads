# CLAUDE.md — Pyramid Cards (Unity 6.4 port)

Project instructions for Claude Code. Read this first every session.

## What this is

A match-and-refill card puzzle on a 15-card pyramid (rows of 5-4-3-2-1). Ported from a working
HTML/JS prototype to C# for **Unity 6.4 (6000.4)**. The UI is built entirely in code with uGUI —
no prefabs, no authored scenes. Cards are pure state; views are rebuilt on every render.

**Source of truth:** the original prototype `reference/pyramid-cards.html` (if present) is the
authoritative spec for scoring and behaviour. `Evaluator.cs` mirrors its `evaluate()` line-for-line.
When behaviour is ambiguous, match the HTML, not intuition.

## Core rules (must stay in sync with the prototype)

- Each card has a **suit** (0-3) and a **number** (1-11). **Face-down = plays as its colour;
  face-up = plays as its number.** A `null` grid cell is an empty hole.
- **Deck is exactly 44 cards** (4 suits x 11 numbers, one of each). 15 are dealt to the pyramid with
  no starting combos; the remaining **29 are the draw pile**. Cleared cards are gone for good — when
  the pile empties, cleared cells become holes. Depletion is the core difficulty pressure.
- **Player actions:** click a card = flip (colour <-> number); drag one card onto another = swap.
  Each costs 1 move.
- **Resolve loop:** after a move, all combos score and clear, gaps refill from the pile, and any new
  combos cascade — chain multiplier x1 -> x4 (cap) applies to points only.

### Scoring (all constants in `GameConfig.cs` — never hardcode elsewhere)

| Combo | Reward |
|---|---|
| Colour run (face-down, adjacent, same suit) len 2/3/4/5 | 2 / 5 / 9 / 14 |
| Number run (face-up, adjacent, same number) | sum of face values + 0/3/7/12 (by length) |
| Number triad (apex over its two lower neighbours, all same number, face-up) | value x 6 + 6, **+1 crystal** |
| Colour triad (same triangle, face-down, same suit) | **+2 moves, 0 points, +1 crystal** |
| Chain multiplier per cascade step | x1 -> x4 |

Key subtleties (easy to break — preserve them):
- A **triad is detected before runs**, and a colour triad's 3 cells are **excluded from colour-run
  scoring** (so a colour triad pays only moves + crystal, never points).
- Number **runs and number triads are multiplied by any active gild** (`mods[value]`).
- Triads (both kinds) each drop **1 crystal**. Crystals + mods persist for the whole run (session),
  not per level.

### Levels & economy

- 12 moves/level; targets 155/160/170/175/185, then +10 per level. faceDown 0.50.
- These were calibrated by simulation against the prototype for ~50%->30% per-attempt win rate and
  tight winning margins. If you change combo values or moves, the targets must be re-derived — say so,
  don't silently retune.

### The Workshop (between-level shop)

- Opens on "Next level" (via `GameManager.Advance` -> `OpenShop`). Offers up to 3 numbers to **gild**
  (x2 = 2 crystals, x3 = 4 crystals). Owning x2 upgrades to x3; maxed (x3) numbers aren't offered.
- A gilded, face-up card shows an "xN" tag in its top-right corner (`CardView`).

## File map

```
Assets/PyramidCards/Scripts/
  CardData.cs         card model (suit, num, up)
  GameConfig.cs       ALL tuning constants, level table, colour palette
  Evaluator.cs        pure scoring (engine-free, unit-testable) — mirrors prototype evaluate()
  GameManager.cs      state + rules: deal, flip/swap, Resolve() coroutine, crystals, shop, win/lose
  CardView.cs         card visuals + input (IPointerClick = flip, IBeginDrag/EndDrag = swap)
  GameUI.cs           builds the whole interface at runtime via UIFactory; Render() from state
  PyramidBootstrap.cs the only hand-placed component
```

Data/logic flow: `GameManager` holds state and calls `ui.Render(opts)`. Views never hold game state —
they're destroyed and rebuilt each render. `Evaluator` has **no UnityEngine dependency** and is the
right place to add EditMode tests.

## Run it

1. Empty Unity 6.4 project; copy in `Assets/PyramidCards`.
2. Any scene -> empty GameObject -> add `PyramidBootstrap` -> Play.
3. Input backend is auto-handled: `GameUI.Init` selects `InputSystemUIInputModule` when
   `ENABLE_INPUT_SYSTEM` is defined (New or Both), else `StandaloneInputModule`. Requires the Input
   System package installed when on the New backend.

## Known first-pass state (not bugs — intended simplifications)

- Card flip is instant (no 3D rotate tween).
- Triad "threads" are per-card glows, not drawn connecting triangles.
- Text uses the built-in legacy runtime font.
- **This was never compiled in the porting environment** — on first open, do a build/Play pass and fix
  any stray compile or layout issues before feature work.

## Roadmap (good next tasks)

- TextMeshPro for all text; sprite-based rounded cards.
- Coroutine/DOTween flip animation; LineRenderer/UILine triad threads.
- JSON save (`Application.persistentDataPath`) to persist crystals + mods across runs.
- EditMode tests on `Evaluator` — port the prototype's scoring cases first.
- Responsive layout (currently a fixed 1280x720 reference with ScaleWithScreenSize).

## Conventions / guardrails

- All tunable numbers live in `GameConfig.cs`. Don't scatter magic numbers.
- Keep `Evaluator` engine-free (no `UnityEngine` types) so it stays testable and matches the prototype.
- Namespace everything `PyramidCards`.
- If you change any scoring/moves/deck value, note that the calibrated **level targets need
  re-derivation**, and don't change targets unless asked.
- When unsure about a rule, diff against `reference/pyramid-cards.html`.
