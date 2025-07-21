using System.Collections;
using System.Collections.Generic;
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
        public GameObject aiTokenPrefab;

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
        public Sprite aiIcon;
        public Button retryButton;

        [Header("Audio Settings")]
        public AudioClip passThroughSFX;   
        public AudioClip tokenLandSFX;     
        public AudioClip winSFX;
        public AudioClip warpSFX;

        public int GetDropY(int x, int z) => FindDropY(x, z);
        public bool IsAITurn => InputManager.Instance.playAgainstAI && _currentPlayer == 1;
        public TokenType[,,] board => _board;

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
        readonly Color _aiColor = Color.purple;
        void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
            DontDestroyOnLoad(this.gameObject);

            _audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
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
            bool vsAI = InputManager.Instance.playAgainstAI;
            if (turnText != null)
            {
                if (_currentPlayer == 0)
                {
                    turnText.text = "Player One's Turn";
                    turnText.color = _playerOneColor;
                }
                else if (vsAI)
                {
                    turnText.text = "AI's Turn";
                    turnText.color = _aiColor;
                }
                else
                {
                    turnText.text = "Player Two's Turn";
                    turnText.color = _playerTwoColor;
                }
            }
            if (turnImage != null)
            {
                if (_currentPlayer == 0)
                    turnImage.sprite = playerOneIcon;
                else if (vsAI)
                    turnImage.sprite = aiIcon;
                else
                    turnImage.sprite = playerTwoIcon;
            }
        }

        /// <summary>
        /// Updates the score display for both players.
        /// </summary>
        void UpdateScoreUI()
        {
            if (scoreText != null)
            {
                bool vsAI = InputManager.Instance.playAgainstAI;
                string secondLabel = vsAI ? "AI" : "P2";
                scoreText.text = $"P1: {_playerOneScore}   {secondLabel}: {_playerTwoScore}";
            }
        }

        /// <summary>
        /// Rotate the board 90° around the Y-axis (sideways).
        /// </summary>
        public void RotateLeft()
        {
            if (InputManager.Instance.playAgainstAI && _currentPlayer == 1) return;
            if (_isDropping) return;
            StartCoroutine(GridManager.Instance.AnimateContainerRotation(Vector3Int.up, 90f));
        }

        /// <summary>
        /// Rotate the board -90° around the Y-axis (sideways).
        /// </summary>
        public void RotateRight()
        {
            if (InputManager.Instance.playAgainstAI && _currentPlayer == 1) return;
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
            for (int x = 0; x < GridManager.Instance.sizeX; x++)
                for (int y = 0; y < GridManager.Instance.sizeY; y++)
                    for (int z = 0; z < GridManager.Instance.sizeZ; z++)
                        _board[x, y, z] = TokenType.None;

            _currentPlayer = 0;
            _isDropping = false;
            _gameOver = false;

            if (!keepScores)
            {
                _playerOneScore = 0;
                _playerTwoScore = 0;
                UpdateScoreUI();
            }

            if (winText != null) winText.gameObject.SetActive(false);
            if (retryButton != null) retryButton.gameObject.SetActive(false);
            UpdateTurnUI();

            GridManager.Instance.ResetGrid();
        }

        /// <summary>
        /// Simulates placing a token at (x,z) for a player and checks for a win.
        /// </summary>
        public bool IsWinningMove(int x, int z, TokenType t)
        {
            int y = GetDropY(x, z);
            if (y < 0) return false;
            _board[x, y, z] = t;
            bool win = CheckWin(x, y, z, t);
            _board[x, y, z] = TokenType.None;
            return win;
        }

        public List<Vector2Int> GetForkMoves(List<Vector2Int> validMoves, TokenType t)
        {
            var forks = new List<Vector2Int>();
            foreach (var m in validMoves)
            {
                int x = m.x, z = m.y;
                int y = GetDropY(x, z);
                if (y < 0) continue;
                _board[x, y, z] = t;
                int count = 0;
                foreach (var n in validMoves)
                {
                    if (n.x == x && n.y == z) continue;
                    if (IsWinningMove(n.x, n.y, t))
                    {
                        count++;
                        if (count >= 2) break;
                    }
                }
                _board[x, y, z] = TokenType.None;
                if (count >= 2) forks.Add(m);
            }
            return forks;
        }

        public int ApplyMove(int x, int z, TokenType t)
        {
            int y = FindDropY(x, z);
            if (y < 0) return -1;
            _board[x, y, z] = t;
            return y;
        }

        public void UndoMove(int x, int z)
        {
            for (int y = GridManager.Instance.sizeY - 1; y >= 0; y--)
                if (_board[x, y, z] != TokenType.None)
                {
                    _board[x, y, z] = TokenType.None;
                    return;
                }
        }

        /// <summary>
        /// Shrinks the token to zero, teleports it, then grows it back.
        /// </summary>
        IEnumerator ScaleWarpRoutine(Transform token, Vector3 targetWorldPos, float duration = 0.2f)
        {
            Vector3 startScale = token.localScale;
            float half = duration * 0.5f, t = 0f;

            // shrink
            while (t < half)
            {
                token.localScale = Vector3.Lerp(startScale, Vector3.zero, t / half);
                t += Time.deltaTime;
                yield return null;
            }
            token.localScale = Vector3.zero;

            // teleport
            token.position = targetWorldPos;

            // grow
            t = 0f;
            while (t < half)
            {
                token.localScale = Vector3.Lerp(Vector3.zero, startScale, t / half);
                t += Time.deltaTime;
                yield return null;
            }
            token.localScale = startScale;
        }

        public bool CheckAnyWin(TokenType t)
        {
            var gm = GridManager.Instance;
            for (int x = 0; x < gm.sizeX; x++)
                for (int y = 0; y < gm.sizeY; y++)
                    for (int z = 0; z < gm.sizeZ; z++)
                        if (_board[x, y, z] == t && CheckWin(x, y, z, t))
                            return true;
            return false;
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
            var gm = GridManager.Instance;
            TokenType placed = _currentPlayer == 0 ? TokenType.PlayerOne : TokenType.PlayerTwo;

            // 1) Spawn at the top of this column:
            Vector3 topWorld = gm.GetCellWorldPosition(x, gm.sizeY - 1, z);
            Vector3 spawnPos = topWorld + Vector3.up * dropHeight;
            GameObject prefab = placed == TokenType.PlayerOne
                ? playerOneTokenPrefab
                : (InputManager.Instance.playAgainstAI && _currentPlayer == 1
                    ? aiTokenPrefab
                    : playerTwoTokenPrefab);
            GameObject token = Instantiate(prefab, spawnPos, prefab.transform.rotation, gm.TimelineContainer);

            // 2) Find if there's a hole in this column, pick the highest one:
            Vector3Int? holeSrc = null;
            foreach (var kv in gm.BlackHoles)
            {
                var src = kv.Key;
                if (src.x == x && src.z == z)
                    if (!holeSrc.HasValue || src.y > holeSrc.Value.y)
                        holeSrc = src;
            }

            if (holeSrc.HasValue)
            {
                var hs = holeSrc.Value;
                Vector3 holeWorld = gm.GetCellWorldPosition(hs.x, hs.y, hs.z);
                yield return StartCoroutine(DropVisual(token.transform, holeWorld));         

                _audioSource.PlayOneShot(warpSFX);
                yield return StartCoroutine(ScaleWarpRoutine(token.transform, holeWorld));

                gm.RemoveBlackHole(hs);

                int xOpp = (hs.x == 0 || hs.x == gm.sizeX - 1) ? gm.sizeX - 1 - hs.x : x;
                int zOpp = (hs.z == 0 || hs.z == gm.sizeZ - 1) ? gm.sizeZ - 1 - hs.z : z;
                int dropYOpp = GetDropY(xOpp, zOpp);

                if (dropYOpp >= 0)
                {
                    Vector3 topOpp = gm.GetCellWorldPosition(xOpp, gm.sizeY - 1, zOpp);
                    token.transform.position = topOpp + Vector3.up * dropHeight;

                    Vector3 dest = gm.GetCellWorldPosition(xOpp, dropYOpp, zOpp);
                    yield return StartCoroutine(DropVisual(token.transform, dest));          

                    gm.SetCellVisible(xOpp, dropYOpp, zOpp, false);
                    _board[xOpp, dropYOpp, zOpp] = placed;

                    x = xOpp; y = dropYOpp; z = zOpp;
                }
                else
                {
                    token.transform.position = holeWorld;
                    _board[hs.x, hs.y, hs.z] = placed;
                    x = hs.x; y = hs.y; z = hs.z;
                }
            }
            else
            {
                Vector3 target = gm.GetCellWorldPosition(x, y, z);
                yield return StartCoroutine(DropVisual(token.transform, target));
                gm.SetCellVisible(x, y, z, false);
                _board[x, y, z] = placed;
            }

            if (CheckWin(x, y, z, placed))
            {
                _audioSource.PlayOneShot(winSFX);
                _gameOver = true;
                if (winText != null)
                {
                    bool vsAI = InputManager.Instance.playAgainstAI;
                    if (placed == TokenType.PlayerOne)
                    {
                        winText.text = "Player One Won!";
                        winText.color = _playerOneColor;
                    }
                    else
                    {
                        winText.text = vsAI ? "AI Won!" : "Player Two Won!";
                        winText.color = vsAI ? _aiColor : _playerTwoColor;
                    }
                    winText.gameObject.SetActive(true);
                }
                if (placed == TokenType.PlayerOne) _playerOneScore++; else _playerTwoScore++;
                UpdateScoreUI();
                StartCoroutine(HighlightWinLineRoutine());
                yield break;
            }

            _currentPlayer = 1 - _currentPlayer;
            UpdateTurnUI();
            GridManager.Instance.TrySpawnRandomBlackHole();
            if (InputManager.Instance.playAgainstAI && _currentPlayer == 1)
            {
                StartCoroutine(AIDelayAndMoveRoutine());
                yield break; 
            }
            _isDropping = false;
        }
        IEnumerator AIDelayAndMoveRoutine()
        {
            yield return new WaitForSeconds(0.5f);
            AIManager.Instance.MakeMove();
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

                yield return new WaitForSeconds(blinkInterval);

                for (int x = 0; x < gm.sizeX; x++)
                    for (int y = 0; y < gm.sizeY; y++)
                        for (int z = 0; z < gm.sizeZ; z++)
                            if (_board[x, y, z] == TokenType.None)
                                gm.SetCellVisible(x, y, z, true);

                if (retryButton != null)
                    retryButton.gameObject.SetActive(true);

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
