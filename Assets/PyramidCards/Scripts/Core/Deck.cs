using System.Collections.Generic;

namespace PyramidCards
{
    /// <summary>Deck construction, shuffling and the initial deal. Pure logic (an injected
    /// <see cref="System.Random"/> is the only side-channel), kept apart from board state so the
    /// dealing policy can be tuned or swapped without touching <see cref="GameManager"/>.</summary>
    public static class Deck
    {
        public static List<CardData> FullDeck(int suits, int nums)
        {
            var d = new List<CardData>();
            for (int s = 0; s < suits; s++)
                for (int n = 1; n <= nums; n++)
                    d.Add(new CardData(s, n, true));
            return d;
        }

        public static void Shuffle<T>(IList<T> a, System.Random rng)
        {
            for (int i = a.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T t = a[i]; a[i] = a[j]; a[j] = t;
            }
        }

        public struct DealResult
        {
            public CardData[][] grid;
            public List<CardData> pile;   // the remaining cards, ready as the draw pile
        }

        /// <summary>Shuffle the deck, deal the pyramid with no starting combos (retrying up to
        /// <paramref name="maxAttempts"/> times), and hand back the rest as the draw pile.</summary>
        public static DealResult Deal(int[] rowSizes, float faceDown, ScoringRules rules,
            Dictionary<int, int> mods, System.Random rng, int maxAttempts = 400)
        {
            int dealt = 0;
            foreach (int size in rowSizes) dealt += size;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                List<CardData> deck = FullDeck(rules.suits, rules.nums);
                Shuffle(deck, rng);

                int k = 0;
                var g = new CardData[rowSizes.Length][];
                for (int r = 0; r < rowSizes.Length; r++)
                {
                    g[r] = new CardData[rowSizes[r]];
                    for (int c = 0; c < g[r].Length; c++)
                    {
                        CardData b = deck[k++];
                        g[r][c] = new CardData(b.suit, b.num, rng.NextDouble() > faceDown);
                    }
                }

                bool clean = Evaluator.Evaluate(g, mods, rules).combos.Count == 0;
                if (clean || attempt == maxAttempts - 1)
                {
                    return new DealResult
                    {
                        grid = g,
                        pile = deck.GetRange(dealt, deck.Count - dealt)
                    };
                }
            }

            // Unreachable (the loop always returns), but keeps the compiler happy.
            return new DealResult { grid = new CardData[0][], pile = new List<CardData>() };
        }
    }
}
