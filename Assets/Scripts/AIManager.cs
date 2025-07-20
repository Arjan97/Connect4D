using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    public class AIManager : MonoBehaviour
    {
        public static AIManager Instance { get; private set; }
        [Tooltip("Seconds AI waits before making its move")] public float moveDelay = 0.7f;

        void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        /// <summary>
        /// Entry point for AI: considers every outer column (where a visible cell exists), tries to win, blocks.
        /// </summary>
        public void MakeMove()
        {
            var gm = GridManager.Instance;
            var validMoves = new List<Vector2Int>();
            int sx = gm.sizeX, sz = gm.sizeZ;

            for (int x = 0; x < sx; x++)
                for (int z = 0; z < sz; z++)
                {
                    int y = GameManager.Instance.GetDropY(x, z);
                    if (y >= 0 && gm.cells[x, y, z] != null)
                        validMoves.Add(new Vector2Int(x, z));
                }

            if (validMoves.Count == 0) return;

            foreach (var m in validMoves)
                if (GameManager.Instance.IsWinningMove(m.x, m.y, TokenType.PlayerTwo))
                {
                    StartCoroutine(DelayedMove(m.x, m.y));
                    return;
                }

            foreach (var m in validMoves)
                if (GameManager.Instance.IsWinningMove(m.x, m.y, TokenType.PlayerOne))
                {
                    StartCoroutine(DelayedMove(m.x, m.y));
                    return;
                }

            Vector2 center = new Vector2((sx - 1) / 2f, (sz - 1) / 2f);
            validMoves.Sort((a, b) =>
            {
                float da = (new Vector2(a.x, a.y) - center).sqrMagnitude;
                float db = (new Vector2(b.x, b.y) - center).sqrMagnitude;
                return da.CompareTo(db);
            });
            var choice = validMoves[Random.Range(0, Mathf.Min(3, validMoves.Count))];
            StartCoroutine(DelayedMove(choice.x, choice.y));
        }

        IEnumerator DelayedMove(int x, int z)
        {
            yield return new WaitForSeconds(moveDelay);
            GameManager.Instance.HandleCellClick(x, z);
        }
    }
}