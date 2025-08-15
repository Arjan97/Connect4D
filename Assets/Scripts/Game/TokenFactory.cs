using UnityEngine;

namespace QuantumConnect
{
    /// <summary>Creates and styles tokens using TokenPool.</summary>
    public class TokenFactory : ITokenFactory
    {
        #region Prefabs
        readonly GameObject playerOnePrefab;
        readonly GameObject playerTwoPrefab;
        readonly GameObject aiPrefab;
        readonly TokenPool pool;
        #endregion

        public TokenFactory(GameObject p1, GameObject p2, GameObject ai, TokenPool tokenPool)
        {
            playerOnePrefab = p1;
            playerTwoPrefab = p2;
            aiPrefab = ai;
            pool = tokenPool;
        }

        public GameObject PickPrefab(GameModes mode, int currentPlayer)
        {
            if (mode == GameModes.PvAI && currentPlayer == 1) return aiPrefab;
            return currentPlayer == 0 ? playerOnePrefab : playerTwoPrefab;
        }

        public GameObject SpawnToken(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
        {
            if (!prefab) return null;
            if (pool != null) return pool.Acquire(prefab, parent, position, rotation);
            return Object.Instantiate(prefab, position, rotation, parent);
        }

        public void PostSpawnVisual(GameModes mode, int currentPlayer, GameObject token, Material aiTwoMaterial)
        {
            if (!token) return;
            if (mode == GameModes.PvAI && currentPlayer == 1 && aiTwoMaterial != null)
            {
                var rend = token.GetComponentInChildren<MeshRenderer>();
                if (rend != null) rend.sharedMaterial = aiTwoMaterial;
            }
        }
    }
}
