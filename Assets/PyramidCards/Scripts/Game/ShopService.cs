using System;
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

    /// <summary>The between-level "Workshop": rolls a set of number-multiplier offers and validates purchases.
    /// Holds no score/crystal state itself — <see cref="GameManager"/> stays the single source of truth and
    /// asks the service to roll offers and vet a buy.</summary>
    public class ShopService
    {
        readonly System.Random rng;
        readonly int nums;
        readonly int maxMult;

        public List<ShopOffer> Offers { get; } = new List<ShopOffer>();
        public int PendingLevel { get; private set; }

        public ShopService(System.Random rng, int nums, int maxMult = 3)
        {
            this.rng = rng;
            this.nums = nums;
            this.maxMult = maxMult;
        }

        /// <summary>Roll a fresh set of offers for numbers that aren't already maxed out.</summary>
        public void Roll(int nextLevel, Func<int, int> currentMod, int slots = 3)
        {
            PendingLevel = nextLevel;
            Offers.Clear();

            var pool = new List<int>();
            for (int n = 1; n <= nums; n++)
                if (currentMod(n) < maxMult) pool.Add(n);

            Deck.Shuffle(pool, rng);

            int count = Mathf.Min(slots, pool.Count);
            for (int i = 0; i < count; i++)
            {
                int n = pool[i];
                // an already-owned x2 can only upgrade to x3; a fresh number is a coin-flip between x2 and x3
                int mult = currentMod(n) == 2 ? 3 : (rng.NextDouble() < 0.5 ? 3 : 2);
                Offers.Add(new ShopOffer { num = n, mult = mult, cost = mult == 3 ? 4 : 2 });
            }
        }

        /// <summary>Can this offer be bought with the given crystal balance? Does not mutate anything.</summary>
        public bool CanBuy(int index, int crystals)
        {
            if (index < 0 || index >= Offers.Count) return false;
            ShopOffer o = Offers[index];
            return !o.bought && crystals >= o.cost;
        }

        /// <summary>Marks the offer bought and reports its details. Caller applies crystal/mod changes.</summary>
        public bool Commit(int index, int crystals, out ShopOffer offer)
        {
            offer = null;
            if (!CanBuy(index, crystals)) return false;
            offer = Offers[index];
            offer.bought = true;
            return true;
        }
    }
}
