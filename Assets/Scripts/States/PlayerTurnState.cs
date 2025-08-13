using System.Collections;

namespace QuantumConnect
{
    /// <summary>Player turn: updates UI and processes a single click → drop.</summary>
    public class PlayerTurnState : IGameState
    {
        public void Enter(GameManager g)
        {
            if (!g.Turns.IsHumanTurn(g.Mode)) { g.SetState(new AITurnState()); return; }

            CentralManager.Instance?.UI?.UpdateTurn(g.Mode, g.Turns.Current);
        }

        public void Exit(GameManager g) { }

        public void OnCellClick(GameManager g, int x, int z)
        {
            if (g.IsOver || g.IsResolving || !g.Turns.IsHumanTurn(g.Mode) || g.Mode == GameModes.AIvAI) return;

            g.StartCoroutine(g.DropToken(x, z));
            g.SetState(new ResolvingState());
        }

        public IEnumerator Tick(GameManager g) { yield break; }
    }
}
