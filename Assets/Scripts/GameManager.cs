using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        [Header("UI Elements")]
        public TextMeshProUGUI turnText;
        public TextMeshProUGUI winText;
        public Image turnImage;
        public Sprite playerOneIcon;
        public Sprite playerTwoIcon;
        TokenType[,,] _board;
        int _currentPlayer;
        bool _isDropping;
        bool _gameOver;
        readonly Color _playerOneColor = Color.red;
        readonly Color _playerTwoColor = Color.yellow;
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
            _gameOver = false;
            if (winText != null) winText.gameObject.SetActive(false);
            UpdateTurnUI();
        }

        /// <summary>
        /// Updates the TMP text and image showing whose turn it is.
        /// </summary>
        void UpdateTurnUI()
        {
            if (turnText != null)
            {
                turnText.text = _currentPlayer == 0 ? "Player One's Turn" : "Player Two's Turn";
                turnText.color = _currentPlayer == 0 ? _playerOneColor : _playerTwoColor;
            }
            if (turnImage != null)
            {
                turnImage.sprite = _currentPlayer == 0 ? playerOneIcon : playerTwoIcon;
            }
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
            if (_isDropping || _gameOver) return;
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

        /// <summary>
        /// Checks all 3D directions for four in a row from (x,y,z).
        /// </summary>
        bool CheckWin(int x, int y, int z, TokenType t)
        {
            var directions = new Vector3Int[] {
            new Vector3Int(1,0,0), new Vector3Int(0,1,0), new Vector3Int(0,0,1),
            new Vector3Int(1,1,0), new Vector3Int(1,-1,0),
            new Vector3Int(1,0,1), new Vector3Int(1,0,-1),
            new Vector3Int(0,1,1), new Vector3Int(0,1,-1),
            new Vector3Int(1,1,1), new Vector3Int(1,1,-1),
            new Vector3Int(1,-1,1), new Vector3Int(1,-1,-1)
        };
            int n = GridManager.Instance.sizeX;

            foreach (var dir in directions)
            {
                int count = 1 + CountDirection(x, y, z, dir, t) + CountDirection(x, y, z, -dir, t);
                if (count >= 4) return true;
            }
            return false;
        }

        /// <summary>
        /// Counts consecutive tokens in dir from (x,y,z).
        /// </summary>
        int CountDirection(int x, int y, int z, Vector3Int dir, TokenType t)
        {
            int count = 0;
            int nx = x + dir.x, ny = y + dir.y, nz = z + dir.z;
            int sx = GridManager.Instance.sizeX;
            int sy = GridManager.Instance.sizeY;
            int sz = GridManager.Instance.sizeZ;

            while (nx >= 0 && nx < sx && ny >= 0 && ny < sy && nz >= 0 && nz < sz)
            {
                if (_board[nx, ny, nz] == t) { count++; }
                else break;
                nx += dir.x; ny += dir.y; nz += dir.z;
            }
            return count;
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
            // Reserve board state
            TokenType placed = _currentPlayer == 0 ? TokenType.PlayerOne : TokenType.PlayerTwo;
            _board[x, y, z] = placed;

            var gm = GridManager.Instance;
            Vector3 targetPos = gm.GetCellWorldPosition(x, y, z);
            // Determine the world Y of the top cell in this column
            Vector3 topCellPos = gm.GetCellWorldPosition(x, gm.sizeY - 1, z);
            float spawnY = topCellPos.y + dropHeight;
            Vector3 spawnPos = new Vector3(targetPos.x, spawnY, targetPos.z);

            GameObject prefab = (_currentPlayer == 0) ? playerOneTokenPrefab : playerTwoTokenPrefab;
            GameObject token = Instantiate(prefab, spawnPos, Quaternion.identity, gm.TimelineContainer);

            yield return StartCoroutine(DropVisual(token.transform, targetPos));
            // Check win
            if (CheckWin(x, y, z, placed))
            {
                _gameOver = true;
                if (winText != null)
                {
                    winText.text = placed == TokenType.PlayerOne ? "Player One Won!" : "Player Two Won!";
                    winText.gameObject.SetActive(true);
                }
                yield break;
            }
            GridManager.Instance.SetCellVisible(x, y, z, false);
            // Switch player
            _currentPlayer = 1 - _currentPlayer;
            UpdateTurnUI();
            _isDropping = false;
        }
    }
}
