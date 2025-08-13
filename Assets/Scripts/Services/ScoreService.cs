namespace QuantumConnect
{
    /// <summary>Tracks player scores.</summary>
    public class ScoreService
    {
        public int P1 { get; private set; }
        public int P2 { get; private set; }

        public void Reset(bool keep)
        {
            if (!keep) { P1 = 0; P2 = 0; }
        }

        public void Add(TokenTypes winner)
        {
            if (winner == TokenTypes.PlayerOne) P1++; else P2++;
        }
    }
}