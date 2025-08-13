using UnityEngine;

namespace QuantumConnect
{
    /// <summary>Default token asset policy.</summary>
    public class TokenFactory : ITokenFactory
    {
        readonly GameObject _p1Prefab, _p2Prefab, _aiPrefab;

        public TokenFactory(GameObject p1, GameObject p2, GameObject ai)
        {
            _p1Prefab = p1; _p2Prefab = p2; _aiPrefab = ai;
        }

        public GameObject PickPrefab(GameMode mode, int currentPlayer)
        {
            if (mode == GameMode.AIvAI) return _aiPrefab;
            if (currentPlayer == 0) return _p1Prefab;
            return mode == GameMode.PvAI ? _aiPrefab : _p2Prefab;
        }

        public void PostSpawnVisual(GameMode mode, int currentPlayer, GameObject token, Material aiTwoMaterial)
        {
            if (mode == GameMode.AIvAI && currentPlayer == 1 && aiTwoMaterial != null)
            {
                var r = token.GetComponent<MeshRenderer>();
                if (r != null) r.material = aiTwoMaterial;
            }
        }
    }
}
