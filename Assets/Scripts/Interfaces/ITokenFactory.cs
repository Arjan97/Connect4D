using UnityEngine;

namespace QuantumConnect
{
    /// <summary>Chooses token assets per game mode and player, and applies visuals.</summary>
    public interface ITokenFactory
    {
        GameObject PickPrefab(GameMode mode, int currentPlayer);
        void PostSpawnVisual(GameMode mode, int currentPlayer, GameObject token, Material aiTwoMaterial);
    }
}
