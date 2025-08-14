using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// Handles black hole spawning, removal, visuals, and destination mapping.
    /// </summary>
    public class BlackHoleManager : MonoBehaviour
    {
        #region Fields
        [Header("Prefabs")]
        [SerializeField] GameObject _blackHolePrefab;
        #endregion

        #region Properties
        public IReadOnlyDictionary<Vector3Int, BlackHoleData> BlackHoles => _map;
        #endregion

        #region Private State
        GameManager _gameM;
        CubeManager _cubeM;
        IAudioService _audioM;
        GameTuning _tuning;

        readonly Dictionary<Vector3Int, BlackHoleData> _map = new Dictionary<Vector3Int, BlackHoleData>();
        #endregion

        #region Types
        public class BlackHoleData
        {
            public Vector3Int Destination;
            public GameObject Instance;
        }
        #endregion

        #region Lifecycle
        void OnDisable()
        {
            StopAllCoroutines();
        }
        #endregion

        #region Init / Readiness
        public void Initialize(GameManager game, CubeManager cube, IAudioService audio, GameTuning tuning)
        {
            _gameM = game;
            _cubeM = cube;
            _audioM = audio;
            _tuning = tuning;
        }

        bool IsReadyForSpawn(BoardModel board = null)
        {
            if (_cubeM == null) return false;
            if (_cubeM.CubeContainer == null || _cubeM.Cells == null) return false;
            if (_blackHolePrefab == null) return false;
            if (board != null && board.cells == null) return false;
            return true;
        }
        #endregion

        #region Public API
        public void InitializeBlackHoles(BoardModel board)
        {
            foreach (var kv in _map.Values) if (kv.Instance != null) Destroy(kv.Instance);
            _map.Clear();

            if (!IsReadyForSpawn(board))
            {
                Debug.LogWarning("BlackHoleManager.InitializeBlackHoles: Not ready (cube/board/prefab missing). Skipping.");
                return;
            }

            var avail = new List<Vector3Int>();
            for (int x = 0; x < _cubeM.sizeX; x++)
                for (int y = 0; y < _cubeM.sizeY; y++)
                    for (int z = 0; z < _cubeM.sizeZ; z++)
                        if (_cubeM.Cells[x, y, z] != null && board.cells[x, y, z] == TokenTypes.None)
                            avail.Add(new Vector3Int(x, y, z));

            int maxInit = _tuning ? _tuning.maxInitialBlackHoles : 6;
            int minInit = _tuning ? _tuning.minInitialBlackHoles : 2;

            maxInit = Mathf.Max(0, maxInit);
            minInit = Mathf.Clamp(minInit, 0, maxInit);

            int maxCount = Mathf.Min(maxInit, avail.Count);
            if (maxCount <= 0) return;

            int spawnCount = Random.Range(minInit, maxCount + 1);
            if (spawnCount <= 0) return;

            for (int i = 0; i < spawnCount; i++)
            {
                int idx = Random.Range(0, avail.Count);
                CreateBlackHoleAt(avail[idx]);
                avail.RemoveAt(idx);
            }
        }

        public void TrySpawnRandomBlackHole()
        {
            if (_gameM == null || !IsReadyForSpawn(_gameM.Board)) return;

            int cap = _tuning ? _tuning.maxConcurrentBlackHoles : 6;
            if (_map.Count >= cap) return;

            int empty = 0;
            for (int x = 0; x < _cubeM.sizeX; x++)
                for (int y = 0; y < _cubeM.sizeY; y++)
                    for (int z = 0; z < _cubeM.sizeZ; z++)
                        if (_cubeM.Cells[x, y, z] != null && _gameM.Board[x, y, z] == TokenTypes.None)
                            empty++;

            int minEmpty = _tuning ? _tuning.minEmptyCellsForSpawn : 10;
            if (empty < minEmpty) return;

            float chance = _tuning ? _tuning.blackHoleSpawnChance : 0.1f;
            if (Random.value >= chance) return;

            var choices = new List<Vector3Int>();
            for (int x = 0; x < _cubeM.sizeX; x++)
                for (int y = 0; y < _cubeM.sizeY; y++)
                    for (int z = 0; z < _cubeM.sizeZ; z++)
                    {
                        var c = new Vector3Int(x, y, z);
                        if (_cubeM.Cells[x, y, z] == null) continue;
                        if (_gameM.Board[x, y, z] != TokenTypes.None) continue;
                        if (_map.ContainsKey(c)) continue;
                        choices.Add(c);
                    }

            if (choices.Count == 0) return;

            var pick = choices[Random.Range(0, choices.Count)];
            CreateBlackHoleAt(pick);
        }

        public void RemoveBlackHole(Vector3Int src)
        {
            if (_cubeM == null || _cubeM.CubeContainer == null) return;

            if (_map.TryGetValue(src, out var data))
            {
                if (data.Instance != null) Destroy(data.Instance);
                _map.Remove(src);
            }

            var center = CubeManager.CenterOffset(_cubeM.sizeX, _cubeM.sizeY, _cubeM.sizeZ, _cubeM.CellSpacing);
            var localPos = CubeManager.LocalFromIndices(src.x, src.y, src.z, _cubeM.CellSpacing, center);
            localPos -= center;

            var go = Instantiate(_cubeM.CellPrefab, _cubeM.CubeContainer);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;

            var newCell = go.GetComponent<Cell>();
            newCell.Initialize(src.x, src.y, src.z);
            _cubeM.Cells[src.x, src.y, src.z] = newCell;
        }
        #endregion

        #region Private
        void CreateBlackHoleAt(Vector3Int src)
        {
            if (!IsReadyForSpawn()) return;
            if (_map.ContainsKey(src)) return;

            var candidates = new List<Vector3Int>();
            var cells = _cubeM.Cells;
            foreach (var c in cells)
            {
                if (c == null) continue;
                var coord = new Vector3Int(c.X, c.Y, c.Z);
                if (coord != src) candidates.Add(coord);
            }
            if (candidates.Count == 0) return;

            var dst = candidates[Random.Range(0, candidates.Count)];

            var original = cells[src.x, src.y, src.z];
            Vector3 worldPos = original != null
                ? original.transform.position
                : _cubeM.GetCellWorldPosition(src.x, src.y, src.z);

            if (original != null)
            {
                Destroy(original.gameObject);
                cells[src.x, src.y, src.z] = null;
            }

            var bhGO = Instantiate(_blackHolePrefab, worldPos, Quaternion.identity, _cubeM.CubeContainer);

            var cellComp = bhGO.GetComponent<Cell>() ?? bhGO.AddComponent<Cell>();
            cellComp.Initialize(src.x, src.y, src.z);
            cells[src.x, src.y, src.z] = cellComp;

            StartCoroutine(WarpInBlackHole(bhGO.transform));
            _audioM?.PlayWarp();

            _map[src] = new BlackHoleData { Destination = dst, Instance = bhGO };
        }

        IEnumerator WarpInBlackHole(Transform tf)
        {
            if (!tf) yield break;

            float dur = (_tuning != null && _tuning.blackHoleWarpInDuration > 0f) ? _tuning.blackHoleWarpInDuration : 0.5f;
            Vector3 targetScale = Vector3.one;
            float t = 0f;

            tf.localScale = Vector3.zero;

            while (t < dur)
            {
                if (!tf) yield break;
                tf.localScale = Vector3.LerpUnclamped(Vector3.zero, targetScale, t / dur);
                t += Time.deltaTime;
                yield return null;
            }
            if (tf) tf.localScale = targetScale;
        }
        #endregion
    }
}
