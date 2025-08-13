using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// Manages the 3D cube grid: spawn cells, rotations, face checks, valid move queries, and drops.
    /// Reads layout/rotation from GameTuning via CentralManager.
    /// </summary>
    public class CubeManager : MonoBehaviour
    {
        #region Backing (will be overridden by GameTuning if present)
        [Header("Grid Size (fallback when no GameTuning)")]
        [SerializeField] public int sizeX = 4;
        [SerializeField] public int sizeY = 4;
        [SerializeField] public int sizeZ = 4;

        [Header("Layout (fallback)")]
        [SerializeField] Vector3 _startPosition = Vector3.zero;
        [SerializeField] Vector3 _cellSpacing = new Vector3(1.8f, 1.8f, 1.8f);

        [Header("Rotation (fallback)")]
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

        #region Private
        GameTuning _tuning;
        #endregion

        #region Unity
        void Awake()
        {
            var central = CentralManager.Instance;
            _tuning = central != null ? central.Tuning : null;

            if (_tuning != null)
            {
                sizeX = _tuning.sizeX;
                sizeY = _tuning.sizeY;
                sizeZ = _tuning.sizeZ;
                _startPosition = _tuning.startPosition;
                _cellSpacing = _tuning.cellSpacing;
                _rotationDuration = _tuning.rotationDuration;
                _rotationPause = _tuning.rotationPause;
            }
        }

        void Start()
        {
            SpawnGrid();
        }
        #endregion

        #region Public – Cube Ops
        public void ResetGrid()
        {
            if (CubeContainer != null)
                Destroy(CubeContainer.gameObject);
            SpawnGrid();
        }

        public Vector3 GetCellWorldPosition(int x, int y, int z)
        {
            var c = Cells[x, y, z];
            if (c != null) return c.transform.position;

            Vector3 centerOffset = new(
                (sizeX - 1) * _cellSpacing.x * 0.5f,
                (sizeY - 1) * _cellSpacing.y * 0.5f,
                (sizeZ - 1) * _cellSpacing.z * 0.5f
            );

            Vector3 local = new Vector3(x * _cellSpacing.x, y * _cellSpacing.y, z * _cellSpacing.z) - centerOffset;
            return CubeContainer.TransformPoint(local);
        }

        public void SetCellVisible(int x, int y, int z, bool visible)
        {
            var cell = Cells[x, y, z];
            if (cell == null) return;
            var rend = cell.GetComponent<MeshRenderer>();
            if (rend != null) rend.enabled = visible;
        }

        public Vector3Int GetActiveFaceNormal()
        {
            var cam = Camera.main;
            if (cam == null || CubeContainer == null) return Face.Front;

            Vector3 toCam = cam.transform.position - CubeContainer.position;
            Vector3 local = Quaternion.Inverse(CubeContainer.rotation) * toCam;
            local.y = 0f;
            if (local.sqrMagnitude > 0f) local.Normalize();

            if (Mathf.Abs(local.z) >= Mathf.Abs(local.x))
                return local.z >= 0f ? Face.Front : Face.Back;
            return local.x >= 0f ? Face.Right : Face.Left;
        }

        public bool IsOnFace(Vector3Int coord, Vector3Int face)
        {
            if (face.x < 0 && coord.x != 0) return false;
            if (face.x > 0 && coord.x != sizeX - 1) return false;
            if (face.z < 0 && coord.z != 0) return false;
            if (face.z > 0 && coord.z != sizeZ - 1) return false;
            return true;
        }

        public bool IsEdge(int x, int z)
        {
            return x == 0 || x == sizeX - 1 || z == 0 || z == sizeZ - 1;
        }

        // NEW SIGS: pass the board in
        public List<Vector2Int> GetAllValidMoves(BoardModel board)
        {
            var list = new List<Vector2Int>();
            for (int x = 0; x < sizeX; x++)
                for (int z = 0; z < sizeZ; z++)
                    if (board.GetDropY(x, z) >= 0)
                        list.Add(new Vector2Int(x, z));
            return list;
        }

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
                    if (!IsOnFace(coord, face)) continue;

                    if (Cells[x, y, z] == null) continue;
                    if (board.cells[x, y, z] != TokenType.None) continue;

                    result.Add(new Vector2Int(x, z));
                }
            }
            return result;
        }

        public List<Vector2Int> GetSideValidMoves(BoardModel board)
        {
            var all = GetAllValidMoves(board);
            var sides = all.FindAll(m => IsEdge(m.x, m.y));
            return sides.Count > 0 ? sides : all;
        }

        public Vector3Int DetermineFaceForDrop(BoardModel board, int x, int z)
        {
            int y = board.GetDropY(x, z);
            if (x == 0) return Face.Left;
            if (x == sizeX - 1) return Face.Right;
            if (z == 0) return Face.Back;
            if (z == sizeZ - 1) return Face.Front;
            if (y == 0) return Face.Down;
            return Face.Up;
        }

        public IEnumerator RotateToFace(Vector3Int face)
        {
            int current = FaceIndex(GetActiveFaceNormal());
            int target = FaceIndex(face);
            if (current < 0 || target < 0 || current == target) yield break;

            int diff = (target - current + 4) % 4;

            if (diff == 1)
            {
                yield return AnimateContainerRotation(Vector3.up, -90f);
                yield return new WaitForSeconds(_rotationPause);
            }
            else if (diff == 2)
            {
                yield return AnimateContainerRotation(Vector3.up, -90f);
                yield return new WaitForSeconds(_rotationPause);
                yield return AnimateContainerRotation(Vector3.up, -90f);
                yield return new WaitForSeconds(_rotationPause);
            }
            else if (diff == 3)
            {
                yield return AnimateContainerRotation(Vector3.up, 90f);
                yield return new WaitForSeconds(_rotationPause);
            }
        }

        public void RotateLeft() { StartCoroutine(AnimateContainerRotation(Vector3.up, 90f)); }
        public void RotateRight() { StartCoroutine(AnimateContainerRotation(Vector3.up, -90f)); }

        #endregion

        #region Private
        void SpawnGrid()
        {
            Cells = new Cell[sizeX, sizeY, sizeZ];

            var container = new GameObject("GridCube");
            container.transform.position = _startPosition;
            container.transform.rotation = Quaternion.identity;
            CubeContainer = container.transform;

            Vector3 centerOffset = new(
                (sizeX - 1) * _cellSpacing.x * 0.5f,
                (sizeY - 1) * _cellSpacing.y * 0.5f,
                (sizeZ - 1) * _cellSpacing.z * 0.5f
            );

            for (int x = 0; x < sizeX; x++)
                for (int y = 0; y < sizeY; y++)
                    for (int z = 0; z < sizeZ; z++)
                    {
                        bool isOuter = x == 0 || x == sizeX - 1 || y == 0 || y == sizeY - 1 || z == 0 || z == sizeZ - 1;
                        if (!isOuter) continue;

                        Vector3 localPos = new(x * _cellSpacing.x, y * _cellSpacing.y, z * _cellSpacing.z);
                        localPos -= centerOffset;

                        var go = Instantiate(_cellPrefab, CubeContainer);
                        go.transform.localPosition = localPos;
                        go.transform.localRotation = Quaternion.identity;

                        var cell = go.GetComponent<Cell>();
                        cell.Initialize(x, y, z);
                        Cells[x, y, z] = cell;
                    }
        }

        public IEnumerator AnimateContainerRotation(Vector3 axis, float angle)
        {
            if (CubeContainer == null) yield break;

            Vector3 pivot = CubeContainer.position;
            float elapsed = 0f;
            float duration = Mathf.Max(_rotationDuration, 0.0001f);

            while (elapsed < duration)
            {
                float step = (angle / duration) * Time.deltaTime;
                CubeContainer.RotateAround(pivot, axis, step);
                elapsed += Time.deltaTime;
                yield return null;
            }

            CubeContainer.RotateAround(pivot, axis, angle - (angle / duration) * elapsed);
        }

        static int FaceIndex(Vector3Int f)
        {
            if (f == Face.Front) return 0;
            if (f == Face.Right) return 1;
            if (f == Face.Back) return 2;
            if (f == Face.Left) return 3;
            return -1;
        }
        #endregion
    }
}
