using System;
using System.Collections.Generic;

namespace PyramidCards
{
    /// <summary>Stateless board evaluation. null cells are holes and never match.
    /// Takes its tuning as a plain <see cref="ScoringRules"/> object, so it has no Unity dependency
    /// and can be unit-tested directly.</summary>
    public static class Evaluator
    {
        public static EvalResult Evaluate(CardData[][] grid, Dictionary<int, int> mods, ScoringRules rules)
        {
            var res = new EvalResult();
            res.flags = new CellFlags[grid.Length][];
            for (int r = 0; r < grid.Length; r++)
            {
                res.flags[r] = new CellFlags[grid[r].Length];
                for (int c = 0; c < grid[r].Length; c++) res.flags[r][c] = new CellFlags();
            }

            Func<int, int> mod = v => (mods != null && mods.ContainsKey(v)) ? mods[v] : 1;

            // ---- triads first: apex (r,c) over base (r+1,c) and (r+1,c+1) ----
            // cells of a colour triad are excluded from colour-run scoring (moves only, no points)
            var colorTriadCells = new HashSet<(int, int)>();
            for (int r = 0; r < grid.Length - 1; r++)
            {
                for (int c = 0; c < grid[r].Length; c++)
                {
                    CardData apex = grid[r][c];
                    CardData L = grid[r + 1][c];
                    CardData R = grid[r + 1][c + 1];
                    if (apex == null || L == null || R == null) continue;

                    if (apex.up && L.up && R.up && apex.num == L.num && apex.num == R.num)
                    {
                        var combo = new Combo
                        {
                            type = ComboType.Triad,
                            gem = true,
                            points = (apex.num * rules.triadNumMult + rules.triadNumBonus) * mod(apex.num),
                            label = "Triad of " + apex.num
                        };
                        combo.cells.Add((r, c)); combo.cells.Add((r + 1, c)); combo.cells.Add((r + 1, c + 1));
                        foreach (var (rr, cc) in combo.cells) res.flags[rr][cc].triad = true;
                        res.total += combo.points;
                        res.combos.Add(combo);
                    }
                    else if (!apex.up && !L.up && !R.up && apex.suit == L.suit && apex.suit == R.suit)
                    {
                        var combo = new Combo
                        {
                            type = ComboType.TriadColor,
                            gem = true,
                            points = 0,
                            moveBonus = rules.triadColorMoves,
                            suit = apex.suit,
                            label = SuitName(rules, apex.suit) + " triad"
                        };
                        combo.cells.Add((r, c)); combo.cells.Add((r + 1, c)); combo.cells.Add((r + 1, c + 1));
                        foreach (var (rr, cc) in combo.cells)
                        {
                            res.flags[rr][cc].triadColor = true;
                            colorTriadCells.Add((rr, cc));
                        }
                        res.combos.Add(combo);
                    }
                }
            }

            // ---- runs within each row ----
            for (int r = 0; r < grid.Length; r++)
            {
                CardData[] row = grid[r];
                int rr = r;

                // colour runs: face-down, same suit, adjacent, not claimed by a colour triad
                foreach (var run in ScanRuns(row.Length, c =>
                    (row[c] != null && !row[c].up && !colorTriadCells.Contains((rr, c))) ? "s" + row[c].suit : null))
                {
                    int cap = Math.Min(run.Count, rules.colorPts.Length - 1);
                    var combo = new Combo
                    {
                        type = ComboType.Color,
                        suit = row[run[0]].suit,
                        points = rules.colorPts[cap],
                        label = run.Count + " " + SuitName(rules, row[run[0]].suit) + " in a row"
                    };
                    foreach (int c in run) { combo.cells.Add((r, c)); res.flags[r][c].color = true; }
                    res.total += combo.points;
                    res.combos.Add(combo);
                }

                // number runs: face-up, same number, adjacent — scored by face value
                foreach (var run in ScanRuns(row.Length, c =>
                    (row[c] != null && row[c].up) ? "n" + row[c].num : null))
                {
                    int v = row[run[0]].num;
                    int cap = Math.Min(run.Count, rules.numberLenBonus.Length - 1);
                    var combo = new Combo
                    {
                        type = ComboType.Number,
                        points = (v * run.Count + rules.numberLenBonus[cap]) * mod(v),
                        label = run.Count + "x the number " + v
                    };
                    foreach (int c in run) { combo.cells.Add((r, c)); res.flags[r][c].number = true; }
                    res.total += combo.points;
                    res.combos.Add(combo);
                }
            }

            return res;
        }

        static string SuitName(ScoringRules rules, int suit)
        {
            if (rules.suitNames != null && suit >= 0 && suit < rules.suitNames.Length) return rules.suitNames[suit];
            return "Suit " + suit;
        }

        /// <summary>Groups adjacent indices sharing the same non-null key; keeps runs of length >= 2.</summary>
        static List<List<int>> ScanRuns(int length, Func<int, string> key)
        {
            var runs = new List<List<int>>();
            int i = 0;
            while (i < length)
            {
                string k = key(i);
                if (k == null) { i++; continue; }
                int j = i + 1;
                while (j < length && key(j) == k) j++;
                if (j - i >= 2)
                {
                    var run = new List<int>();
                    for (int t = i; t < j; t++) run.Add(t);
                    runs.Add(run);
                }
                i = j;
            }
            return runs;
        }

        public static HashSet<(int, int)> CellsOf(List<Combo> combos)
        {
            var set = new HashSet<(int, int)>();
            foreach (var combo in combos)
                foreach (var cell in combo.cells)
                    set.Add(cell);
            return set;
        }
    }
}
