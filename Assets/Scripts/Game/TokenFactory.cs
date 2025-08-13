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

        public GameObject PickPrefab(GameModes mode, int currentPlayer)
        {
            if (mode == GameModes.AIvAI) return _aiPrefab;
            if (currentPlayer == 0) return _p1Prefab;
            return mode == GameModes.PvAI ? _aiPrefab : _p2Prefab;
        }

        public void PostSpawnVisual(GameModes mode, int currentPlayer, GameObject token, Material aiTwoMaterial)
        {
            if (mode == GameModes.AIvAI && currentPlayer == 1 && aiTwoMaterial != null)
            {
                var r = token.GetComponent<MeshRenderer>();
                if (r != null) r.material = aiTwoMaterial;
            }
        }
    }
}
