using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// Manages the 3D cube grid: spawn cells, rotations, face checks, valid move queries, and drops.
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

        bool _initialized;

        #region Unity
        void Start()
        {
            if (CubeContainer == null)
                SpawnGrid();
        }
        #endregion

        #region DI Init
        /// <summary>
        /// Apply tuning values and (optionally) rebuild the grid.
        /// </summary>
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

            _initialized = true;

            if (respawn)
            {
                if (CubeContainer != null)
                    Destroy(CubeContainer.gameObject);
                SpawnGrid();
            }
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
            return WorldFromIndices(CubeContainer, x, y, z, sizeX, sizeY, sizeZ, _cellSpacing);
        }

        public void SetCellVisible(int x, int y, int z, bool visible)
        {
            var cell = Cells[x, y, z];
            if (cell == null) return;

            var rends = cell.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i]) rends[i].enabled = visible;
            }
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
        public static Vector3 CenterOffset(int sizeX, int sizeY, int sizeZ, Vector3 spacing)
        {
            return new Vector3(
                (sizeX - 1) * spacing.x * 0.5f,
                (sizeY - 1) * spacing.y * 0.5f,
                (sizeZ - 1) * spacing.z * 0.5f
            );
        }

        public static Vector3 LocalFromIndices(int x, int y, int z, Vector3 spacing, Vector3 centerOffset)
        {
            return new Vector3(x * spacing.x, y * spacing.y, z * spacing.z) - centerOffset;
        }

        public static Vector3 WorldFromIndices(Transform container, int x, int y, int z,
                                               int sizeX, int sizeY, int sizeZ, Vector3 spacing)
        {
            var center = CenterOffset(sizeX, sizeY, sizeZ, spacing);
            var local = LocalFromIndices(x, y, z, spacing, center);
            return container.TransformPoint(local);
        }

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
                    if (board.cells[x, y, z] != TokenTypes.None) continue;

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
            var center = CenterOffset(sizeX, sizeY, sizeZ, _cellSpacing);

            for (int x = 0; x < sizeX; x++)
                for (int y = 0; y < sizeY; y++)
                    for (int z = 0; z < sizeZ; z++)
                    {
                        bool isOuter = x == 0 || x == sizeX - 1 || y == 0 || y == sizeY - 1 || z == 0 || z == sizeZ - 1;
                        if (!isOuter) continue;

                        var localPos = LocalFromIndices(x, y, z, _cellSpacing, center);
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
