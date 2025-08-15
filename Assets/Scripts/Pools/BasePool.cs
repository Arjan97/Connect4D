using UnityEngine;

namespace QuantumConnect
{
    /// <summary>Common base for all pools.</summary>
    public abstract class BasePool : MonoBehaviour
    {
        #region State
        Transform poolRoot;
        #endregion

        #region API
        protected Transform EnsureRoot(string label)
        {
            if (poolRoot != null) return poolRoot;

            var go = new GameObject($"[Pool] {label}");
            go.transform.SetParent(transform, false);
            poolRoot = go.transform;

            return poolRoot;
        }
        #endregion
    }
}
