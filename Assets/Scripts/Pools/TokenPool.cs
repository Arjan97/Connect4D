using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>Keyed pool for token prefabs (Player1, Player2, AI). Tracks active instances.</summary>
    public class TokenPool : BasePool
    {
        #region Prefabs
        [Header("Prefabs (owned by pool)")]
        [SerializeField] GameObject playerOnePrefab;
        [SerializeField] GameObject playerTwoPrefab;
        [SerializeField] GameObject aiPrefab;

        [Header("Prewarm")]
        [SerializeField] int p1Prewarm = 8;
        [SerializeField] int p2Prewarm = 8;
        [SerializeField] int aiPrewarm = 8;
        #endregion

        #region State
        readonly Dictionary<GameObject, Queue<GameObject>> pool = new();
        readonly Dictionary<GameObject, GameObject> instanceToPrefab = new();
        readonly HashSet<GameObject> active = new();
        Transform root;
        bool prewarmed;
        #endregion

        #region Unity
        void Awake()
        {
            root = EnsureRoot("Tokens");
            Prewarm();
        }
        #endregion

        #region API
        public GameObject AcquireByMode(GameModes mode, int currentPlayer, Transform parent, Vector3 position)
        {
            var prefab = ResolvePrefab(mode, currentPlayer);
            if (!prefab) return null;

            var rot = prefab.transform.rotation;
            return Acquire(prefab, parent, position, rot);
        }

        public GameObject Acquire(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
        {
            if (!prefab) return null;
            if (!prewarmed) Prewarm();

            if (!pool.TryGetValue(prefab, out var q))
            {
                q = new Queue<GameObject>();
                pool[prefab] = q;
            }

            GameObject go;
            if (q.Count > 0)
            {
                go = q.Dequeue();
                if (!go) go = Instantiate(prefab);
            }
            else
            {
                go = Instantiate(prefab);
            }

            var t = go.transform;
            t.SetParent(parent ? parent : root, false);
            t.position = position;
            t.rotation = rotation;
            go.SetActive(true);

            instanceToPrefab[go] = prefab;
            active.Add(go);
            return go;
        }

        public void Release(GameObject instance)
        {
            if (!instance) return;

            if (!instanceToPrefab.TryGetValue(instance, out var prefab))
            {
                instance.SetActive(false);
                instance.transform.SetParent(root, false);
                return;
            }

            instance.SetActive(false);
            instance.transform.SetParent(root, false);

            if (!pool.TryGetValue(prefab, out var q))
            {
                q = new Queue<GameObject>();
                pool[prefab] = q;
            }
            q.Enqueue(instance);
            active.Remove(instance);
        }

        public void ReleaseAllActive()
        {
            var list = new List<GameObject>(active);
            for (int i = 0; i < list.Count; i++) Release(list[i]);
        }

        public GameObject ResolvePrefab(GameModes mode, int currentPlayer)
        {
            if (mode == GameModes.PvAI && currentPlayer == 1) return aiPrefab;
            return currentPlayer == 0 ? playerOnePrefab : playerTwoPrefab;
        }
        #endregion

        #region Internals
        void Prewarm()
        {
            if (prewarmed) return;
            prewarmed = true;

            TryPrewarmFor(playerOnePrefab, p1Prewarm);
            TryPrewarmFor(playerTwoPrefab, p2Prewarm);
            TryPrewarmFor(aiPrefab, aiPrewarm);
        }

        void TryPrewarmFor(GameObject prefab, int count)
        {
            if (!prefab || count <= 0) return;
            if (!pool.TryGetValue(prefab, out var q))
            {
                q = new Queue<GameObject>();
                pool[prefab] = q;
            }
            for (int i = 0; i < count; i++)
            {
                var go = Instantiate(prefab, root);
                go.SetActive(false);
                q.Enqueue(go);
            }
        }
        #endregion
    }
}
