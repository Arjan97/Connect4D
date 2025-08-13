using UnityEngine;

namespace QuantumConnect
{
    /// <summary>Chooses token assets per game mode and player, and applies visuals.</summary>
    public interface ITokenFactory
    {
        GameObject PickPrefab(GameModes mode, int currentPlayer);
        void PostSpawnVisual(GameModes mode, int currentPlayer, GameObject token, Material aiTwoMaterial);
    }
}
