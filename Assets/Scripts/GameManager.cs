using System.Collections;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// Possible contents of a grid cell.
    /// </summary>
    public enum TokenType { None, PlayerOne, PlayerTwo }

    /// <summary>
    /// Central game controller: handles board state, rotations, and token placement.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Token Prefabs")]
        public GameObject playerOneTokenPrefab;
        public GameObject playerTwoTokenPrefab;

        [Header("Drop Settings")]
        [Tooltip("World-units above top of grid to spawn tokens.")]
        public float dropHeight = 1.8f;
        [Tooltip("Units/sec at which tokens fall.")]
        public float dropSpeed = 8f;

        TokenType[,,] _board;
        int _currentPlayer;
        bool _isDropping;

        void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }

        void Start()
        {
            var gm = GridManager.Instance;
            _board = new TokenType[gm.sizeX, gm.sizeY, gm.sizeZ];
            _currentPlayer = 0;
            _isDropping = false;
        }

        /// <summary>
        /// Rotate the board 90° around the Y-axis (sideways).
        /// </summary>
        public void RotateLeft()
        {
            if (_isDropping) return;
            StartCoroutine(GridManager.Instance.AnimateContainerRotation(Vector3Int.up, 90f));
        }

        /// <summary>
        /// Rotate the board -90° around the Y-axis (sideways).
        /// </summary>
        public void RotateRight()
        {
            if (_isDropping) return;
            StartCoroutine(GridManager.Instance.AnimateContainerRotation(Vector3Int.up, -90f));
        }

        /// <summary>
        /// Handles a cell click by dropping a token into that column.
        /// </summary>
        public void HandleCellClick(int x, int z)
        {
            if (_isDropping) return;
            int y = FindDropY(x, z);
            if (y < 0) return; 
            _isDropping = true;
            StartCoroutine(DropTokenRoutine(x, y, z));
        }

        int FindDropY(int x, int z)
        {
            var gm = GridManager.Instance;
            for (int y = 0; y < gm.sizeY; y++)
                if (_board[x, y, z] == TokenType.None)
                    return y;
            return -1;
        }

        IEnumerator DropVisual(Transform token, Vector3 targetPos)
        {
            while (token.position.y > targetPos.y + 0.01f)
            {
                token.position += Vector3.down * dropSpeed * Time.deltaTime;
                yield return null;
            }
            token.position = targetPos;
        }

        IEnumerator DropTokenRoutine(int x, int y, int z)
        {
            // Reserve board state immediately
            _board[x, y, z] = (_currentPlayer == 0) ? TokenType.PlayerOne : TokenType.PlayerTwo;

            var gm = GridManager.Instance;
            Vector3 targetPos = gm.GetCellWorldPosition(x, y, z);
            // Determine the world Y of the top cell in this column
            Vector3 topCellPos = gm.GetCellWorldPosition(x, gm.sizeY - 1, z);
            float spawnY = topCellPos.y + dropHeight;
            Vector3 spawnPos = new Vector3(targetPos.x, spawnY, targetPos.z);

            GameObject prefab = (_currentPlayer == 0) ? playerOneTokenPrefab : playerTwoTokenPrefab;
            GameObject token = Instantiate(prefab, spawnPos, Quaternion.identity, gm.TimelineContainer);

            yield return StartCoroutine(DropVisual(token.transform, targetPos));

            // Switch player
            _currentPlayer = 1 - _currentPlayer;
            _isDropping = false;
        }
    }
}
