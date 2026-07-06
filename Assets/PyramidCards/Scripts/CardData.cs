namespace PyramidCards
{
    /// <summary>One card: a suit (colour), a number 1..11, and which face is showing.
    /// Face-down plays as a colour; face-up plays as a number. A null cell in the grid is an empty hole.</summary>
    public class CardData
    {
        public int suit;   // 0..3
        public int num;    // 1..11
        public bool up;    // true = number face showing

        public CardData(int suit, int num, bool up)
        {
            this.suit = suit;
            this.num = num;
            this.up = up;
        }
    }
}
