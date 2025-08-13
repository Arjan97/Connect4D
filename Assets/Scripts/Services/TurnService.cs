namespace QuantumConnect
{
    public class TurnService
    {
        public int Current { get; private set; }
        public void Reset() => Current = 0;     
        public void Next() => Current = 1 - Current;

        public bool IsAiTurn(GameModes mode) =>
            (mode == GameModes.PvAI && Current == 1) || mode == GameModes.AIvAI;

        public bool IsHumanTurn(GameModes mode) =>
            (mode == GameModes.PvP) || (mode == GameModes.PvAI && Current == 0);
    }
}
