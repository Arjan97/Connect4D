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
            GameModes mode,
            Vector3Int startXZ,
            ITokenFactory factory,
            Material aiTwoMaterial,
            Action<TokenTypes, int, int, int> onPlaced
        );
    }
}
