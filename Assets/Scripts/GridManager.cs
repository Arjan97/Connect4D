using System.Collections;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// Manages the spawning and handling of the 4󫶘 cell grid for each timeline.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        public int sizeX = 4;
        public int sizeY = 4;
        public int sizeZ = 4;
        public float spacing = 1.1f;
        public float rotationDuration = 0.2f;

        [Header("Layout Settings")]
        [Tooltip("World position of the grid's center.")]
        public Vector3 startPosition = Vector3.zero;
        [Tooltip("Spacing between cells along each axis.")]
        public Vector3 cellSpacing = new Vector3(1.8f, 1.8f, 1.8f);

        [Header("References")]
        public GameObject cellPrefab;

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
        /// Instantiates a sizeX譻izeY譻izeZ grid of cells under a new container.
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
            var rend = cells[x, y, z].GetComponent<MeshRenderer>();
            if (rend != null) rend.enabled = visible;
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

    }
}
