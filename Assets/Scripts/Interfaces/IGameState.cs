using System.Collections;

namespace QuantumConnect
{
    /// <summary>State contract for the Game state machine.</summary>
    public interface IGameState
    {
        void Enter(GameManager g);
        void Exit(GameManager g);
        void OnCellClick(GameManager g, int x, int z);
        IEnumerator Tick(GameManager g);
    }
}
