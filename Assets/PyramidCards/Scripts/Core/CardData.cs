namespace PyramidCards
{
    /// <summary>One card: a suit (colour), a number 1..N, and which face is showing.
    /// Face-down plays as a colour; face-up plays as a number. A null cell in the grid is an empty hole.
    /// Plain data with no Unity dependency so the rules layer stays testable in isolation.</summary>
    public class CardData
    {
        public int suit;   // 0..suits-1
        public int num;    // 1..nums
        public bool up;    // true = number face showing

        public CardData(int suit, int num, bool up)
        {
            this.suit = suit;
            this.num = num;
            this.up = up;
        }

        public CardData Clone()
        {
            return new CardData(suit, num, up);
        }
    }
}
