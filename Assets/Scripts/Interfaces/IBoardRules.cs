using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>Contract for pure board logic: gravity, win checks, forks.</summary>
    public interface IBoardRules
    {
        int FindDropY(BoardModel board, int x, int z);
        int ApplyMove(BoardModel board, int x, int z, TokenTypes t);
        void UndoMove(BoardModel board, int x, int z);

        bool IsWinningMove(BoardModel board, int x, int z, TokenTypes t);
        bool CheckAnyWin(BoardModel board, TokenTypes t, out List<Vector3Int> winningLine);

        bool HasAnyEmpty(BoardModel board);

        List<Vector2Int> GetForkMoves(BoardModel board, List<Vector2Int> validMoves, TokenTypes t);
    }
}
