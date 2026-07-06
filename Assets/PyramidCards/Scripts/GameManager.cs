using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PyramidCards
{
    public class ShopOffer
    {
        public int num;
        public int mult;
        public int cost;
        public bool bought;
    }

    public class RenderOpts
    {
        public CellFlags[][] flags;
        public HashSet<(int, int)> clearing;
        public HashSet<(int, int)> entering;
    }

    /// <summary>All game state and rules. The UI observes via direct reference.</summary>
    public class GameManager : MonoBehaviour
    {
        public GameUI ui;

        public CardData[][] grid;
        public List<CardData> pile = new List<CardData>();   // the rest of the 44-card deck

        public int level, target, moves, score, chain, crystals;
        public float faceDown;
        public bool busy, prompted;

        public Dictionary<int, int> mods = new Dictionary<int, int>();  // number -> score multiplier (persists for the run)
        public List<Combo> lastCleared = new List<Combo>();
        public List<ShopOffer> offers = new List<ShopOffer>();
        public int pendingLevel;

        readonly System.Random rng = new System.Random();

        // ===== level lifecycle =====

        public void StartLevel(int i)
        {
            LevelConfig cfg = GameConfig.GetLevel(i);
            level = i; target = cfg.target; moves = cfg.moves; faceDown = cfg.faceDown;
            score = 0; chain = 0; busy = false; prompted = false;
            lastCleared.Clear();
            DealLevel();
            ui.Render(null);
            CheckEnd();
        }

        List<CardData> FullDeck()
        {
            var d = new List<CardData>();
            for (int s = 0; s < GameConfig.Suits; s++)
                for (int n = 1; n <= GameConfig.Nums; n++)
                    d.Add(new CardData(s, n, true));
            return d;
        }

        void Shuffle<T>(IList<T> a)
        {
            for (int i = a.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T t = a[i]; a[i] = a[j]; a[j] = t;
            }
        }

        /// <summary>Shuffle the 44, deal 15 with no starting combos; the remaining 29 become the draw pile.</summary>
        void DealLevel()
        {
            for (int attempt = 0; attempt < 400; attempt++)
            {
                List<CardData> deck = FullDeck();
                Shuffle(deck);
                int k = 0;
                var g = new CardData[GameConfig.RowSizes.Length][];
                for (int r = 0; r < GameConfig.RowSizes.Length; r++)
                {
                    g[r] = new CardData[GameConfig.RowSizes[r]];
                    for (int c = 0; c < g[r].Length; c++)
                    {
                        CardData b = deck[k++];
                        g[r][c] = new CardData(b.suit, b.num, rng.NextDouble() > faceDown);
                    }
                }
                if (Evaluator.Evaluate(g, mods).combos.Count == 0 || attempt == 399)
                {
                    grid = g;
                    pile = deck.GetRange(15, deck.Count - 15);
                    return;
                }
            }
        }

        // ===== player actions: click = flip, drag = swap =====

        bool CanAct()
        {
            if (busy) return false;
            if (moves <= 0) { ui.FlashMoves(); return false; }
            return true;
        }

        public void Flip(int r, int c)
        {
            if (!CanAct() || grid[r][c] == null) return;
            grid[r][c].up = !grid[r][c].up;
            moves--;
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
            AfterMove();
        }

        void AfterMove()
        {
            lastCleared.Clear();
            chain = 0;
            ui.Render(null);
            StartCoroutine(Resolve());
        }

        // ===== the match -> clear -> refill cascade =====

        IEnumerator Resolve()
        {
            while (true)
            {
                EvalResult ev = Evaluator.Evaluate(grid, mods);
                if (ev.combos.Count == 0)
                {
                    busy = false;
                    ui.Render(null);
                    CheckEnd();
                    yield break;
                }

                busy = true;
                chain++;
                int mult = Mathf.Min(chain, GameConfig.ChainCap);
                score += ev.total * mult;

                int moveBonus = 0, gems = 0;
                foreach (Combo c in ev.combos)
                {
                    moveBonus += c.moveBonus;
                    if (c.gem) gems++;
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

                HashSet<(int, int)> clearing = Evaluator.CellsOf(ev.combos);
                ui.Render(new RenderOpts { flags = ev.flags, clearing = clearing });
                yield return new WaitForSeconds(GameConfig.ClearSeconds);

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
                ui.Render(new RenderOpts { entering = entering });
                yield return new WaitForSeconds(GameConfig.FillSeconds);
            }
        }

        // ===== win / lose =====

        void CheckEnd()
        {
            if (busy) return;
            if (score >= target)
            {
                ui.SetNextVisible(true);
                if (!prompted)
                {
                    prompted = true;
                    ui.ShowModal(true, "Target reached",
                        "You banked " + score + " against a target of " + target +
                        ". Move on, or keep clearing for a higher score.",
                        "Next level", Advance,
                        "Keep playing", ui.HideModal);
                }
            }
            else
            {
                ui.SetNextVisible(false);
                if (moves <= 0)
                {
                    ui.ShowModal(false, "Out of moves",
                        "You banked " + score + " of " + target + ". Re-deal and try a different line of play.",
                        "Try again", () => { ui.HideModal(); StartLevel(level); },
                        null, null);
                }
            }
        }

        public void Advance()
        {
            ui.HideModal();
            OpenShop(level + 1);
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
            pendingLevel = nextLevel;
            offers.Clear();
            var pool = new List<int>();
            for (int n = 1; n <= GameConfig.Nums; n++)
                if (GetMod(n) < 3) pool.Add(n);
            Shuffle(pool);
            for (int i = 0; i < Mathf.Min(3, pool.Count); i++)
            {
                int n = pool[i];
                int mult = GetMod(n) == 2 ? 3 : (rng.NextDouble() < 0.5 ? 3 : 2); // owned x2 upgrades to x3
                offers.Add(new ShopOffer { num = n, mult = mult, cost = mult == 3 ? 4 : 2 });
            }
            ui.ShowShop();
        }

        public bool TryBuy(int index)
        {
            ShopOffer o = offers[index];
            if (o.bought || crystals < o.cost) return false;
            crystals -= o.cost;
            mods[o.num] = Mathf.Max(GetMod(o.num), o.mult);
            o.bought = true;
            return true;
        }

        public void BeginPending()
        {
            ui.HideShop();
            StartLevel(pendingLevel);
        }
    }
}
