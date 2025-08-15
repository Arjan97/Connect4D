using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// Manages the 3D cube grid: spawn cells, grid math, visibility, and simple move queries.
    /// </summary>
    public class CubeManager : MonoBehaviour
    {
        #region Backing (overridden by Initialize(GameTuning) if provided)
        [Header("Grid Size (fallback when no GameTuning)")]
        public int sizeX = 4;
        public int sizeY = 4;
        public int sizeZ = 4;

        [Header("Layout (fallback)")]
        [SerializeField] Vector3 _startPosition = Vector3.zero;
        [SerializeField] Vector3 _cellSpacing = new Vector3(1.8f, 1.8f, 1.8f);

        [Header("Rotation (fallback, used by VFX timing only)")]
        [SerializeField] float _rotationDuration = 0.2f;
        [SerializeField] float _rotationPause = 0.2f;

        [Header("Prefabs")]
        [SerializeField] GameObject _cellPrefab;
        #endregion

        #region Properties
        public Transform CubeContainer { get; private set; }
        public Cell[,,] Cells { get; private set; }
        public float RotationPause => _rotationPause;
        public Vector3 CellSpacing => _cellSpacing;
        public GameObject CellPrefab => _cellPrefab;
        #endregion

        #region Unity
        void Start()
        {
            if (CubeContainer == null)
                SpawnGrid();
        }
        #endregion

        #region DI Init
        /// <summary>Apply tuning values and (optionally) rebuild the grid.</summary>
        public void Initialize(GameTuning tuning, bool respawn = true)
        {
            if (tuning != null)
            {
                sizeX = tuning.sizeX;
                sizeY = tuning.sizeY;
                sizeZ = tuning.sizeZ;
                _startPosition = tuning.startPosition;
                _cellSpacing = tuning.cellSpacing;
                _rotationDuration = tuning.rotationDuration;
                _rotationPause = tuning.rotationPause;
            }

            if (respawn)
            {
                if (CubeContainer != null)
                    Destroy(CubeContainer.gameObject);
                SpawnGrid();
            }
        }
        #endregion

        #region Public – Grid Ops
        public void ResetGrid()
        {
            if (CubeContainer != null)
                Destroy(CubeContainer.gameObject);
            SpawnGrid();
        }

        public Vector3 GetCellWorldPosition(int x, int y, int z)
            => GridUtils.WorldFromIndices(CubeContainer, x, y, z, sizeX, sizeY, sizeZ, _cellSpacing);

        public void SetCellVisible(int x, int y, int z, bool visible)
        {
            var cell = Cells[x, y, z];
            if (cell == null) return;

            var rends = cell.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < rends.Length; i++)
                if (rends[i]) rends[i].enabled = visible;
        }

        public bool IsEdge(int x, int z)
            => x == 0 || x == sizeX - 1 || z == 0 || z == sizeZ - 1;

        /// <summary>All (x,z) columns that can accept a drop.</summary>
        public List<Vector2Int> GetAllValidMoves(BoardModel board)
        {
            var list = new List<Vector2Int>();
            for (int x = 0; x < sizeX; x++)
                for (int z = 0; z < sizeZ; z++)
                    if (board.GetDropY(x, z) >= 0)
                        list.Add(new Vector2Int(x, z));
            return list;
        }

        /// <summary>Returns edge columns if any; otherwise returns all valid moves.</summary>
        public List<Vector2Int> GetSideValidMoves(BoardModel board)
        {
            var all = GetAllValidMoves(board);
            var sides = all.FindAll(m => IsEdge(m.x, m.y));
            return sides.Count > 0 ? sides : all;
        }

        /// <summary>
        /// Collect moves visible on a given face (delegates face logic to FaceUtils).
        /// </summary>
        public List<Vector2Int> CollectMovesOnFace(BoardModel board, Vector3Int face)
        {
            var result = new List<Vector2Int>();

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int y = board.GetDropY(x, z);
                    if (y < 0) continue;

                    var coord = new Vector3Int(x, y, z);
                    if (!FaceUtils.IsOnFace(coord, face, sizeX, sizeZ)) continue;

                    if (Cells[x, y, z] == null) continue;
                    if (board.cells[x, y, z] != TokenTypes.None) continue;

                    result.Add(new Vector2Int(x, z));
                }
            }
            return result;
        }
        #endregion

        #region Private – Build
        void SpawnGrid()
        {
            Cells = new Cell[sizeX, sizeY, sizeZ];

            var container = new GameObject("GridCube");
            container.transform.position = _startPosition;
            container.transform.rotation = Quaternion.identity;
            CubeContainer = container.transform;

            var center = GridUtils.CenterOffset(sizeX, sizeY, sizeZ, _cellSpacing);

            for (int x = 0; x < sizeX; x++)
                for (int y = 0; y < sizeY; y++)
                    for (int z = 0; z < sizeZ; z++)
                    {
                        bool isOuter = x == 0 || x == sizeX - 1 || y == 0 || y == sizeY - 1 || z == 0 || z == sizeZ - 1;
                        if (!isOuter) continue;

                        var localPos = GridUtils.LocalFromIndices(x, y, z, _cellSpacing, center);
                        var go = Instantiate(_cellPrefab, CubeContainer);
                        go.transform.localPosition = localPos;
                        go.transform.localRotation = Quaternion.identity;

                        var cell = go.GetComponent<Cell>();
                        cell.Initialize(x, y, z);
                        Cells[x, y, z] = cell;
                    }
        }
        #endregion
    }
}
