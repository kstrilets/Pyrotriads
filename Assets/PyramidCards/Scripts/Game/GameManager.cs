using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PyramidCards
{
    /// <summary>All game state and rules. Knows nothing about the view: it reads its tuning from
    /// ScriptableObject config and reports everything that happens through C# events, so a UI, an audio
    /// service, a VFX service, analytics — any number of listeners — can react without coupling back in.</summary>
    public class GameManager : MonoBehaviour
    {
        // ===== events (logic ▸ everyone) =====
        public event Action<RenderOpts> RenderRequested;      // redraw board/HUD; opts may be null
        public event Action MoveBlocked;                      // tried to act with no moves left
        public event Action CardFlipped;                      // a successful flip
        public event Action CardsSwapped;                     // a successful swap
        public event Action<CascadeStep> CascadeResolved;     // one match/clear step resolved
        public event Action<bool> NextAvailabilityChanged;    // "Next level" became (un)available
        public event Action<ModalRequest> ModalRequested;
        public event Action ModalDismissed;
        public event Action ShopOpened;
        public event Action ShopChanged;
        public event Action ShopClosed;

        // ===== config (injected by the bootstrap) =====
        GameRulesConfig rules;
        LevelSet levelSet;
        ScoringRules scoring;
        ShopService shop;
        readonly System.Random rng = new System.Random();

        // ===== state (public read surface for the view) =====
        public CardData[][] grid;
        public List<CardData> pile = new List<CardData>();    // the rest of the deck
        public int level, target, moves, score, chain, crystals;
        public float faceDown;
        public bool busy, prompted;

        public Dictionary<int, int> mods = new Dictionary<int, int>();  // number -> score multiplier (persists for the run)
        public List<Combo> lastCleared = new List<Combo>();

        public IReadOnlyList<ShopOffer> Offers => shop != null ? (IReadOnlyList<ShopOffer>)shop.Offers : Array.Empty<ShopOffer>();
        public int PendingLevel => shop != null ? shop.PendingLevel : 0;
        public float ClearSeconds => rules != null ? rules.clearSeconds : 0.42f;
        public float FillSeconds => rules != null ? rules.fillSeconds : 0.34f;

        /// <summary>Must be called once before <see cref="StartLevel"/>.</summary>
        public void Configure(GameRulesConfig rulesConfig, LevelSet levels)
        {
            rules = rulesConfig;
            levelSet = levels;
            scoring = rules.ToScoringRules();
            shop = new ShopService(rng, rules.nums);
        }

        // ===== level lifecycle =====

        public void StartLevel(int i)
        {
            LevelDefinition cfg = levelSet.Get(i);
            level = i; target = cfg.target; moves = cfg.moves; faceDown = cfg.faceDown;
            score = 0; chain = 0; busy = false; prompted = false;
            lastCleared.Clear();

            Deck.DealResult deal = Deck.Deal(rules.rowSizes, faceDown, scoring, mods, rng);
            grid = deal.grid;
            pile = deal.pile;

            RenderRequested?.Invoke(null);
            CheckEnd();
        }

        // ===== player actions: click = flip, drag = swap =====

        bool CanAct()
        {
            if (busy) return false;
            if (moves <= 0) { MoveBlocked?.Invoke(); return false; }
            return true;
        }

        public void Flip(int r, int c)
        {
            if (!CanAct() || grid[r][c] == null) return;
            grid[r][c].up = !grid[r][c].up;
            moves--;
            CardFlipped?.Invoke();
            AfterMove();
        }

        public void Swap(int r1, int c1, int r2, int c2)
        {
            if (!CanAct()) return;
            if (grid[r1][c1] == null || grid[r2][c2] == null) return;
            CardData t = grid[r1][c1];
            grid[r1][c1] = grid[r2][c2];
            grid[r2][c2] = t;
            moves--;
            CardsSwapped?.Invoke();
            AfterMove();
        }

        void AfterMove()
        {
            lastCleared.Clear();
            chain = 0;
            RenderRequested?.Invoke(null);
            StartCoroutine(Resolve());
        }

        // ===== the match -> clear -> refill cascade =====

        IEnumerator Resolve()
        {
            while (true)
            {
                EvalResult ev = Evaluator.Evaluate(grid, mods, scoring);
                if (ev.combos.Count == 0)
                {
                    busy = false;
                    RenderRequested?.Invoke(null);
                    CheckEnd();
                    yield break;
                }

                busy = true;
                chain++;
                int mult = Mathf.Min(chain, rules.chainCap);
                int gained = ev.total * mult;
                score += gained;

                int moveBonus = 0, gems = 0;
                bool hasTriad = false;
                foreach (Combo c in ev.combos)
                {
                    moveBonus += c.moveBonus;
                    if (c.gem) { gems++; hasTriad = true; }
                }
                moves += moveBonus;          // colour triads pay moves, flat (not chained)
                crystals += gems;            // every triad drops a crystal

                var sorted = new List<Combo>(ev.combos);
                sorted.Sort((a, b) => a.type != b.type ? a.type.CompareTo(b.type) : b.points.CompareTo(a.points));
                foreach (Combo c in sorted)
                {
                    lastCleared.Add(new Combo
                    {
                        type = c.type, suit = c.suit, label = c.label, gem = c.gem,
                        moveBonus = c.moveBonus, points = c.points * mult, mult = mult
                    });
                }

                CascadeResolved?.Invoke(new CascadeStep
                {
                    chain = chain, mult = mult, scoreGained = gained,
                    gems = gems, moveBonus = moveBonus, hasTriad = hasTriad
                });

                HashSet<(int, int)> clearing = Evaluator.CellsOf(ev.combos);
                RenderRequested?.Invoke(new RenderOpts { flags = ev.flags, clearing = clearing });
                yield return new WaitForSeconds(rules.clearSeconds);

                foreach (var (r, c) in clearing) grid[r][c] = null;

                var entering = new HashSet<(int, int)>();
                for (int r = 0; r < grid.Length; r++)
                {
                    for (int c = 0; c < grid[r].Length; c++)
                    {
                        if (grid[r][c] == null && pile.Count > 0)
                        {
                            CardData b = pile[pile.Count - 1];
                            pile.RemoveAt(pile.Count - 1);
                            grid[r][c] = new CardData(b.suit, b.num, rng.NextDouble() > faceDown);
                            entering.Add((r, c));
                        }
                    }
                }
                RenderRequested?.Invoke(new RenderOpts { entering = entering });
                yield return new WaitForSeconds(rules.fillSeconds);
            }
        }

        // ===== win / lose =====

        void CheckEnd()
        {
            if (busy) return;
            if (score >= target)
            {
                NextAvailabilityChanged?.Invoke(true);
                if (!prompted)
                {
                    prompted = true;
                    ModalRequested?.Invoke(new ModalRequest
                    {
                        win = true,
                        title = "Target reached",
                        body = "You banked " + score + " against a target of " + target +
                               ". Move on, or keep clearing for a higher score.",
                        primaryLabel = "Next level", onPrimary = Advance,
                        secondaryLabel = "Keep playing", onSecondary = HideModal
                    });
                }
            }
            else
            {
                NextAvailabilityChanged?.Invoke(false);
                if (moves <= 0)
                {
                    ModalRequested?.Invoke(new ModalRequest
                    {
                        win = false,
                        title = "Out of moves",
                        body = "You banked " + score + " of " + target + ". Re-deal and try a different line of play.",
                        primaryLabel = "Try again",
                        onPrimary = () => { HideModal(); StartLevel(level); },
                        secondaryLabel = null, onSecondary = null
                    });
                }
            }
        }

        public void Advance()
        {
            HideModal();
            OpenShop(level + 1);
        }

        public void HideModal()
        {
            ModalDismissed?.Invoke();
        }

        public void Restart()
        {
            if (!busy) StartLevel(level);
        }

        // ===== the Workshop (between-level shop) =====

        public int GetMod(int n)
        {
            return mods.ContainsKey(n) ? mods[n] : 1;
        }

        public void OpenShop(int nextLevel)
        {
            shop.Roll(nextLevel, GetMod);
            ShopOpened?.Invoke();
        }

        public bool TryBuy(int index)
        {
            if (!shop.Commit(index, crystals, out ShopOffer o)) return false;
            crystals -= o.cost;
            mods[o.num] = Mathf.Max(GetMod(o.num), o.mult);
            ShopChanged?.Invoke();
            return true;
        }

        public void BeginPending()
        {
            ShopClosed?.Invoke();
            StartLevel(shop.PendingLevel);
        }
    }
}
