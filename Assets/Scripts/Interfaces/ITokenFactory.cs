using UnityEngine;

namespace QuantumConnect
{
    /// <summary>Creates and styles tokens (pooled).</summary>
    public interface ITokenFactory
    {
        GameObject PickPrefab(GameModes mode, int currentPlayer);
        GameObject SpawnToken(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation);
        void PostSpawnVisual(GameModes mode, int currentPlayer, GameObject token, Material aiTwoMaterial);
    }
}
