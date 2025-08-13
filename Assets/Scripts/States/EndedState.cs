using System.Collections;

namespace QuantumConnect
{
    /// <summary>End game state.</summary>
    public class EndedState : IGameState
    {
        public void Enter(GameManager g) { }
        public void Exit(GameManager g) { }
        public void OnCellClick(GameManager g, int x, int z) { }
        public IEnumerator Tick(GameManager g) { yield break; }
    }
}
