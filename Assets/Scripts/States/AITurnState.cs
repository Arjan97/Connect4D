using System.Collections;

namespace QuantumConnect
{
    /// <summary>AI turn: updates UI and requests an AI move. Ignores clicks in non‑PVP.</summary>
    public class AITurnState : IGameState
    {
        public void Enter(GameManager g)
        {
            if (!g.Turns.IsAiTurn(g.Mode)) { g.SetState(new PlayerTurnState()); return; }

            g.UpdateTurnUI();
            g.RequestAIMove();
        }

        public void Exit(GameManager g) { }
        public void OnCellClick(GameManager g, int x, int z) { }
        public IEnumerator Tick(GameManager g) { yield break; }
    }
}
