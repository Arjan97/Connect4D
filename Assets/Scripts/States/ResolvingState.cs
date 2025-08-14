using System.Collections;

namespace QuantumConnect
{
    /// <summary>Waits a frame for resolution; then branches by game result/turn owner.</summary>
    public class ResolvingState : IGameState
    {
        public void Enter(GameManager g) { }
        public void Exit(GameManager g) { }
        public void OnCellClick(GameManager g, int x, int z) { }

        public IEnumerator Tick(GameManager g)
        {
            while (g.IsResolving) yield return null;

            if (g.IsOver)
            {
                g.ShowRetryIfNeededUI();
                g.SetState(new EndedState());
                yield break;
            }

            var next = g.Turns.IsAiTurn(g.Mode)
                  ? (IGameState)new AITurnState()
                  : new PlayerTurnState();

            g.SetState(next);
        }
    }
}
