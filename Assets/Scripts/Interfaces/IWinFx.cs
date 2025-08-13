using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    public interface IWinFx
    {
        Coroutine PlayWinFx(List<Vector3Int> winningLine);
        void ClearHighlight();

    }
}
