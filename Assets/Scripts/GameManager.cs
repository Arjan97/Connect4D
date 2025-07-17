using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        [Header("Highlight Settings")]
        [Tooltip("Seconds to wait between blink states")]
        public float blinkInterval = 0.5f;

        [Header("Drop Settings")]
        [Tooltip("World-units above top of grid to spawn tokens.")]
        public float dropHeight = 1.8f;
        [Tooltip("Units/sec at which tokens fall.")]
        public float dropSpeed = 8f;

        [Header("UI Elements")]
        public TextMeshProUGUI turnText;
        public TextMeshProUGUI winText;
        public TextMeshProUGUI scoreText;
        public Image turnImage;
        public Sprite playerOneIcon;
        public Sprite playerTwoIcon;
        public Button retryButton;

        [Header("Audio Settings")]
        public AudioClip passThroughSFX;   
        public AudioClip tokenLandSFX;     
        public AudioClip winSFX;          

        AudioSource _audioSource;
        TokenType[,,] _board;
        int _currentPlayer;
        bool _isDropping;
        bool _gameOver;
        List<Vector3Int> _winningLine;

        int _playerOneScore;
        int _playerTwoScore;
        readonly Color _playerOneColor = Color.red;
        readonly Color _playerTwoColor = Color.yellow;
        void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
            DontDestroyOnLoad(this.gameObject);
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();
        }

        void Start()
        {
            var gm = GridManager.Instance;
            _board = new TokenType[gm.sizeX, gm.sizeY, gm.sizeZ];
            _currentPlayer = 0;
            _isDropping = false;
            _gameOver = false;
            _playerOneScore = 0;
            _playerTwoScore = 0;
            if (winText != null) winText.gameObject.SetActive(false);
            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(false);
            }
            UpdateTurnUI();
            UpdateScoreUI();

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
        /// Updates the score display for both players.
        /// </summary>
        void UpdateScoreUI()
        {
            if (scoreText != null)
            {
                scoreText.text = $"P1: {_playerOneScore}   P2: {_playerTwoScore}";
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
        public void ResetGame(bool keepScores = true)
        {
            // Clear board
            for (int x = 0; x < GridManager.Instance.sizeX; x++)
                for (int y = 0; y < GridManager.Instance.sizeY; y++)
                    for (int z = 0; z < GridManager.Instance.sizeZ; z++)
                        _board[x, y, z] = TokenType.None;

            // Reset states
            _currentPlayer = 0;
            _isDropping = false;
            _gameOver = false;

            if (!keepScores)
            {
                _playerOneScore = 0;
                _playerTwoScore = 0;
                UpdateScoreUI();
            }

            // Reset UI
            if (winText != null) winText.gameObject.SetActive(false);
            if (retryButton != null) retryButton.gameObject.SetActive(false);
            UpdateTurnUI();

            // Clear grid visuals
            GridManager.Instance.ResetGrid();
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
            var dirs = new Vector3Int[] {
            new Vector3Int(1,0,0), new Vector3Int(0,1,0), new Vector3Int(0,0,1),
            new Vector3Int(1,1,0), new Vector3Int(1,-1,0),
            new Vector3Int(1,0,1), new Vector3Int(1,0,-1),
            new Vector3Int(0,1,1), new Vector3Int(0,1,-1),
            new Vector3Int(1,1,1), new Vector3Int(1,1,-1),
            new Vector3Int(1,-1,1), new Vector3Int(1,-1,-1)
        };
            int n = GridManager.Instance.sizeX;
            foreach (var dir in dirs)
            {
                int count1 = CountDirection(x, y, z, dir, t);
                int count2 = CountDirection(x, y, z, -dir, t);
                if (count1 + count2 + 1 >= 4)
                {
                    // build winning line coords
                    _winningLine = new List<Vector3Int>();
                    Vector3Int start = new Vector3Int(x - dir.x * count2,
                                                      y - dir.y * count2,
                                                      z - dir.z * count2);
                    for (int i = 0; i < 4; i++)
                        _winningLine.Add(start + dir * i);
                    return true;
                }
            }
            return false;
        }

        IEnumerator DropTokenRoutine(int x, int y, int z)
        {
            TokenType placed = _currentPlayer == 0 ? TokenType.PlayerOne : TokenType.PlayerTwo;
            _board[x, y, z] = placed;

            var gm = GridManager.Instance;
            Vector3 targetPos = gm.GetCellWorldPosition(x, y, z);
            Vector3 topCellPos = gm.GetCellWorldPosition(x, gm.sizeY - 1, z);
            float spawnY = topCellPos.y + dropHeight;
            Vector3 spawnPos = new Vector3(targetPos.x, spawnY, targetPos.z);

            GameObject prefab = placed == TokenType.PlayerOne ? playerOneTokenPrefab : playerTwoTokenPrefab;
            GameObject token = Instantiate(prefab, spawnPos, prefab.transform.rotation, gm.TimelineContainer);

            StartCoroutine(BlockFlashRoutine(x, y, z));

            yield return StartCoroutine(DropVisual(token.transform, targetPos));
            gm.SetCellVisible(x, y, z, false);
            _audioSource.PlayOneShot(tokenLandSFX);

            if (CheckWin(x, y, z, placed))
            {
                _audioSource.PlayOneShot(winSFX);
                _gameOver = true;
                if (winText != null)
                {
                    winText.text = placed == TokenType.PlayerOne ? "Player One Won!" : "Player Two Won!";
                    winText.color = placed == TokenType.PlayerOne ? _playerOneColor : _playerTwoColor;
                    winText.gameObject.SetActive(true);
                }
                if (placed == TokenType.PlayerOne) _playerOneScore++; else _playerTwoScore++;
                UpdateScoreUI();
                if (retryButton != null) retryButton.gameObject.SetActive(true);

                StartCoroutine(HighlightWinLineRoutine());
                yield break;
            }

            _currentPlayer = 1 - _currentPlayer;
            UpdateTurnUI();
            _isDropping = false;
        }

        IEnumerator HighlightWinLineRoutine()
        {
            if (_winningLine == null) yield break;
            var gm = GridManager.Instance;

            float snapAngle = ComputeSnapAngleToFaceWinningLine();
            yield return StartCoroutine(
                GridManager.Instance.AnimateContainerRotation(Vector3.up, snapAngle)
            );

            foreach (var coord in _winningLine)
            {
                if (coord.x < 0 || coord.x >= gm.sizeX ||
                    coord.y < 0 || coord.y >= gm.sizeY ||
                    coord.z < 0 || coord.z >= gm.sizeZ)
                    continue;

                gm.SetCellVisible(coord.x, coord.y, coord.z, true);
                _audioSource.PlayOneShot(passThroughSFX);
                var cell = gm.cells[coord.x, coord.y, coord.z];
                if (cell == null) continue;
                var rend = cell.GetComponent<MeshRenderer>();
                if (rend == null) continue;

                rend.material.color = Color.green;
                yield return new WaitForSeconds(0.5f);
                gm.SetCellVisible(coord.x, coord.y, coord.z, false);
            }
        }

        /// <summary>
        /// Computes the minimal yaw (around world‐up) to turn the line’s centroid toward the camera (world +Z).
        /// </summary>
        float ComputeSnapAngleToFaceWinningLine()
        {
            var gm = GridManager.Instance;
            var container = gm.TimelineContainer;

            Vector3 sum = Vector3.zero;
            foreach (var c in _winningLine)
                sum += gm.GetCellWorldPosition(c.x, c.y, c.z);
            Vector3 centroid = sum / _winningLine.Count;

            Vector3 dir = centroid - container.position;
            dir.y = 0;
            if (dir.sqrMagnitude < 0.0001f) return 0f;
            dir.Normalize();

            Vector3 localDir = Quaternion.Inverse(container.rotation) * dir;

            float targetY;
            if (Mathf.Abs(localDir.z) >= Mathf.Abs(localDir.x))
            {
                targetY = (localDir.z >= 0f) ? 180f : 0f;
            }
            else
            {
                targetY = (localDir.x >= 0f) ? 90f : -90f;
            }

            float currentY = container.eulerAngles.y;
            return Mathf.DeltaAngle(currentY, targetY);
        }

            IEnumerator BlockFlashRoutine(int x, int yDest, int z)
        {
            var gm = GridManager.Instance;
            float wait = gm.cellSpacing.y / dropSpeed;
            for (int y = gm.sizeY - 1; y > yDest; y--)
            {
                gm.SetCellVisible(x, y, z, false);
                _audioSource.PlayOneShot(passThroughSFX);
                yield return new WaitForSeconds(wait);
                gm.SetCellVisible(x, y, z, true);
            }
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
    }
}
