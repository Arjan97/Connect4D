using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>Visual effects.</summary>
    public interface IGameVfx
    {
        // Per-layer blink while a token falls past a cell.
        IEnumerator BlinkCell(int x, int y, int z, float dropSpeed);

        // Shrink → teleport → expand effect for black-hole warp.
        IEnumerator ScaleWarp(Transform token, Vector3 targetWorldPos, float duration = 0.2f);

        // Win highlight passthrough.
        Coroutine PlayWinLine(List<Vector3Int> winningLine);
        void ClearWinHighlight();
    }
}
