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

        Cell[,,] cells;
        public Transform TimelineContainer { get; private set; }
        public static GridManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }

        /// <summary>
        /// Called by Unity on start. Initializes and spawns the grid.
        /// </summary>
        void Start()
        {
            SpawnGrid();
        }

        /// <summary>
        /// Instantiates a sizeX譻izeY譻izeZ grid of cells under a new "Timeline0" container.
        /// </summary>
        void SpawnGrid()
        {
            cells = new Cell[sizeX, sizeY, sizeZ];
            GameObject container = new GameObject("Timeline0");
            container.transform.position = startPosition;
            TimelineContainer = container.transform;

            for (int x = 0; x < sizeX; x++)
                for (int y = 0; y < sizeY; y++)
                    for (int z = 0; z < sizeZ; z++)
                    {
                        Vector3 centerOffset = new Vector3(
                            (sizeX - 1) / 2f,
                            (sizeY - 1) / 2f,
                            (sizeZ - 1) / 2f
                        ) * spacing;

                        Vector3 pos = new Vector3(x, y, z) * spacing - centerOffset; GameObject go = Instantiate(cellPrefab, pos, Quaternion.identity, container.transform);
                        Cell cell = go.GetComponent<Cell>();
                        cell.Initialize(x, y, z);
                        cells[x, y, z] = cell;
                    }
        }

        /// <summary>Returns the world-space position of the cell at (x,y,z).</summary>
        public Vector3 GetCellWorldPosition(int x, int y, int z)
            => cells[x, y, z].transform.position;
        public IEnumerator AnimateContainerRotation(Vector3Int axis, float angle)
        {
            Quaternion start = TimelineContainer.rotation;
            Quaternion end = start * Quaternion.AngleAxis(angle, axis);
            float elapsed = 0f;
            while (elapsed < rotationDuration) 
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / rotationDuration);
                TimelineContainer.rotation = Quaternion.Slerp(start, end, t);
                yield return null;
            }
            TimelineContainer.rotation = end;
        }
        public void RefreshAllCells(TokenType[,,] boardState)
        {
            int sx = sizeX, sy = sizeY, sz = sizeZ;
            for (int x = 0; x < sx; x++)
                for (int y = 0; y < sy; y++)
                    for (int z = 0; z < sz; z++)
                    {
                        Cell c = cells[x, y, z];
                        c.Clear();

                        var t = boardState[x, y, z];
                        if (t == TokenType.None)
                            continue;

                        // pick the right prefab from GameManager
                        GameObject prefab = (t == TokenType.PlayerOne)
                            ? GameManager.Instance.playerOneTokenPrefab
                            : GameManager.Instance.playerTwoTokenPrefab;

                        // instantiate it exactly at the cell's position, parented to the cell
                        Instantiate(prefab,
                                    c.transform.position,
                                    Quaternion.identity,
                                    c.transform);
                    }
        }
        /// <summary>
        /// Calls Clear() on every cell, removing any spawned tokens.
        /// </summary>
        public void ClearAllCells()
        {
            for (int x = 0; x < sizeX; x++)
                for (int y = 0; y < sizeY; y++)
                    for (int z = 0; z < sizeZ; z++)
                        cells[x, y, z].Clear();
        }
    }
}
