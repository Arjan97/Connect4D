using System;
using System.Collections;
using UnityEngine;

namespace QuantumConnect
{
    public interface ITokenDropper
    {
        IEnumerator Drop(
            BoardModel board,
            int currentPlayer,
            GameMode mode,
            Vector3Int startXZ,
            ITokenFactory factory,
            Material aiTwoMaterial,
            Action<TokenType, int, int, int> onPlaced
        );
    }
}
