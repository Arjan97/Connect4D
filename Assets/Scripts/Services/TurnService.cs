namespace QuantumConnect
{
    public class TurnService
    {
        public int Current { get; private set; }
        public void Reset() => Current = 0;     
        public void Next() => Current = 1 - Current;

        public bool IsAiTurn(GameMode mode) =>
            (mode == GameMode.PvAI && Current == 1) || mode == GameMode.AIvAI;

        public bool IsHumanTurn(GameMode mode) =>
            (mode == GameMode.PvP) || (mode == GameMode.PvAI && Current == 0);
    }
}
