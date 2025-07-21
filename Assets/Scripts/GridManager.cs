using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    // Holds the destination coord and the instantiated prefab
   public class BlackHoleData
    {
        public Vector3Int Destination;
        public GameObject Instance;
    }

    /// <summary>
    /// Manages the spawning and handling of the 4×4×4 cell grid for each timeline.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        public int sizeX = 4;
        public int sizeY = 4;
        public int sizeZ = 4;
        public float spacing = 1.1f;
        public float rotationDuration = 0.2f;
        public GameObject cellPrefab;

        [Header("Layout Settings")]
        [Tooltip("World position of the grid's center.")]
        public Vector3 startPosition = Vector3.zero;
        [Tooltip("Spacing between cells along each axis.")]
        public Vector3 cellSpacing = new Vector3(1.8f, 1.8f, 1.8f);

        [Header("Black Hole Settings")]
        [Range(0f, 1f)]
        public float blackHoleChance = 0.1f;
        public float blackHoleWarpDuration = 0.5f;
        public GameObject blackHolePrefab;
        public AudioClip blackHoleSpawnSFX;
        Dictionary<Vector3Int, BlackHoleData> _blackHoles = new();
        public IReadOnlyDictionary<Vector3Int, BlackHoleData> BlackHoles => _blackHoles;

        public Cell[,,] cells;
        public Transform TimelineContainer { get; private set; }
        public static GridManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
            DontDestroyOnLoad(this.gameObject);

        }

        /// <summary>
        /// Called by Unity on start. Initializes and spawns the grid.
        /// </summary>
        void Start()
        {
            SpawnGrid();
        }

        /// <summary>
        /// Instantiates a sizeX×sizeY×sizeZ grid of cells under a new container.
        /// </summary>
        void SpawnGrid()
        {
            cells = new Cell[sizeX, sizeY, sizeZ];
            GameObject container = new GameObject("GridCube");
            container.transform.position = startPosition;
            TimelineContainer = container.transform;

            Vector3 centerOffset = new Vector3(
                          (sizeX - 1) / 2f,
                          (sizeY - 1) / 2f,
                          (sizeZ - 1) / 2f
                      ) * spacing;

            for (int x = 0; x < sizeX; x++)
                for (int y = 0; y < sizeY; y++)
                    for (int z = 0; z < sizeZ; z++)
                    {
                        // only build if on any of the six faces:
                        bool isOuter = x == 0 || x == sizeX - 1
                                    || y == 0 || y == sizeY - 1
                                    || z == 0 || z == sizeZ - 1;
                        if (!isOuter) continue;

                        Vector3 localPos = new Vector3(
                            x * cellSpacing.x,
                            y * cellSpacing.y,
                            z * cellSpacing.z
                        ) - centerOffset;

                        GameObject go = Instantiate(cellPrefab, container.transform);
                        go.transform.localPosition = localPos;
                        go.transform.localRotation = Quaternion.identity;

                        Cell cell = go.GetComponent<Cell>();
                        cell.Initialize(x, y, z);
                        cells[x, y, z] = cell;
                    }
            MarkBlackHolesInitial();
        }

        /// <summary>
        /// Clears all cells and respawns the grid cube.
        /// </summary>
        public void ResetGrid()
        {
            if (TimelineContainer != null)
                Destroy(TimelineContainer.gameObject);

            SpawnGrid();
        }

        /// <summary>
        /// Hides (or shows) the cube mesh at the given cell.
        /// </summary>
        public void SetCellVisible(int x, int y, int z, bool visible)
        {
            var cell = cells[x, y, z];
            if (cell == null) return;

            var rend = cell.GetComponent<MeshRenderer>();
            if (rend != null)
                rend.enabled = visible;
        }

        /// <summary>Returns the world-space position of the cell at (x,y,z).</summary>
        public Vector3 GetCellWorldPosition(int x, int y, int z)
            => cells[x, y, z].transform.position;

        /// <summary>
        /// Smoothly rotates the TimelineContainer around its own center.
        /// </summary>
        public IEnumerator AnimateContainerRotation(Vector3 axis, float angle)
        {
            Vector3 pivot = TimelineContainer.position;
            float elapsed = 0f;
            float duration = rotationDuration;
            while (elapsed < duration)
            {
                float step = (angle / duration) * Time.deltaTime;
                TimelineContainer.RotateAround(pivot, axis, step);
                elapsed += Time.deltaTime;
                yield return null;
            }
            TimelineContainer.RotateAround(pivot, axis, angle - (angle / duration) * elapsed);
        }
        void MarkBlackHolesInitial()
        {
            foreach (Cell c in cells)
            {
                if (c == null) continue;
                var src = new Vector3Int(c.X, c.Y, c.Z);
                if (Random.value < blackHoleChance)
                    CreateBlackHoleAt(src);
            }
        }
        void CreateBlackHoleAt(Vector3Int src)
        {
            var candidates = new List<Vector3Int>();
            foreach (Cell c in cells)
                if (c != null)
                {
                    var dest = new Vector3Int(c.X, c.Y, c.Z);
                    if (dest != src) candidates.Add(dest);
                }
            if (candidates.Count == 0) return;
            var dst = candidates[Random.Range(0, candidates.Count)];

            var original = cells[src.x, src.y, src.z];
            if (original != null)
            {
                Destroy(original.gameObject);
                cells[src.x, src.y, src.z] = null;
            }

            Vector3 worldPos = original.transform.position;
            GameObject bhGO = Instantiate(
                blackHolePrefab,
                worldPos,
                Quaternion.identity,
                TimelineContainer
            );
            bhGO.transform.localScale = Vector3.zero;

            bhGO.AddComponent<BoxCollider>();
            var cellComp = bhGO.AddComponent<Cell>();
            cellComp.Initialize(src.x, src.y, src.z);

            StartCoroutine(WarpInBlackHole(bhGO.transform));
            if (blackHoleSpawnSFX != null)
                AudioSource.PlayClipAtPoint(blackHoleSpawnSFX, worldPos);

            _blackHoles[src] = new BlackHoleData
            {
                Destination = dst,
                Instance = bhGO
            };
            cells[src.x, src.y, src.z] = cellComp;
        }

        public void RemoveBlackHole(Vector3Int src)
        {
            if (_blackHoles.TryGetValue(src, out var data))
            {
                Destroy(data.Instance);
                _blackHoles.Remove(src);
            }

            Vector3 centerOffset = new Vector3(
                (sizeX - 1) * cellSpacing.x * 0.5f,
                (sizeY - 1) * cellSpacing.y * 0.5f,
                (sizeZ - 1) * cellSpacing.z * 0.5f
            );
            Vector3 localPos = new Vector3(
                src.x * cellSpacing.x,
                src.y * cellSpacing.y,
                src.z * cellSpacing.z
            ) - centerOffset;

            GameObject go = Instantiate(cellPrefab, TimelineContainer);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;

            var newCell = go.GetComponent<Cell>();
            newCell.Initialize(src.x, src.y, src.z);
            cells[src.x, src.y, src.z] = newCell;
        }

        public void TrySpawnRandomBlackHole()
        {
            if (Random.value >= blackHoleChance) return;

            var choices = new List<Vector3Int>();
            var board = GameManager.Instance.board;
            foreach (Cell c in cells)
            {
                if (c == null) continue;
                var coord = new Vector3Int(c.X, c.Y, c.Z);
                if (board[coord.x, coord.y, coord.z] != TokenType.None) continue;
                if (_blackHoles.ContainsKey(coord)) continue;
                choices.Add(coord);
            }
            if (choices.Count == 0) return;

            CreateBlackHoleAt(choices[Random.Range(0, choices.Count)]);
        }
        IEnumerator WarpInBlackHole(Transform tf)
        {
            Vector3 targetScale = Vector3.one;
            float half = blackHoleWarpDuration * 0.5f;
            float t = 0f;

            // scale up
            while (t < blackHoleWarpDuration)
            {
                tf.localScale = Vector3.Lerp(
                    Vector3.zero,
                    targetScale,
                    t / blackHoleWarpDuration
                );
                t += Time.deltaTime;
                yield return null;
            }
            tf.localScale = targetScale;
        }
    }
}
