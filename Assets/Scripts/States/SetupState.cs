using System.Collections;

namespace QuantumConnect
{
    public class SetupState : IGameState
    {
        public void Enter(GameManager g)
        {
            g.BeginNewMatch();

            var next = g.Turns.IsAiTurn(g.Mode)
                ? (IGameState)new AITurnState()
                : new PlayerTurnState();

            g.SetState(next);
        }

        public void Exit(GameManager g) { }
        public void OnCellClick(GameManager g, int x, int z) { }
        public IEnumerator Tick(GameManager g) { yield break; }
    }
}
