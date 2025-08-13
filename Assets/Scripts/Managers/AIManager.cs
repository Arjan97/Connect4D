using System.Collections;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// AI coordinator: evaluates the board and instructs CubeManager to rotate and drop.
    /// Reads search/delay from GameTuning via CentralManager when available.
    /// </summary>
    public class AIManager : MonoBehaviour
    {
        GameManager _gameM;
        CubeManager _cubeM;
        GameTuning _tuning;

        int _maxDepth;
        float _moveDelay;
        float _rotationChance;

        static readonly Vector3Int[] _faces = { Face.Right, Face.Left, Face.Front, Face.Back };

        void Awake()
        {
            var central = CentralManager.Instance;

            _gameM = central?.Game ?? FindFirstObjectByType<GameManager>();
            _cubeM = central?.Cube ?? FindFirstObjectByType<CubeManager>();
            _tuning = central?.Tuning;

            ApplyTuning();
        }

        void OnEnable()
        {
            if (_gameM == null) _gameM = FindFirstObjectByType<GameManager>();
            if (_cubeM == null) _cubeM = FindFirstObjectByType<CubeManager>();
            if (_tuning == null) _tuning = CentralManager.Instance?.Tuning;
            ApplyTuning();
        }

        void ApplyTuning()
        {
            _maxDepth = _tuning != null ? _tuning.aiMaxDepth : 4;
            _moveDelay = _tuning != null ? _tuning.aiMoveDelay : 0.7f;
            _rotationChance = _tuning != null ? _tuning.aiRotationChance : 0.3f;
        }

        public void MakeMove()
        {
            if (_gameM == null) _gameM = FindFirstObjectByType<GameManager>();
            if (_cubeM == null) _cubeM = FindFirstObjectByType<CubeManager>();

            if (_gameM == null || _cubeM == null) { Debug.LogWarning("AIManager: missing Game/Cube in this scene."); return; }
            if (!_gameM.Turns.IsAiTurn(_gameM.Mode)) return;

            StartCoroutine(MakeMoveRoutine());
        }

        IEnumerator MakeMoveRoutine()
        {
            yield return new WaitForSeconds(_moveDelay);
            if (_gameM == null || _cubeM == null) yield break;
            var board = _gameM.Board;

            // Win
            if (TryFindImmediateMove(TokenType.PlayerTwo, out var winMove, out var winFace))
            {
                yield return ExecuteMove(winFace, winMove.x, winMove.y);
                yield break;
            }

            // Block opponent
            if (TryFindImmediateMove(TokenType.PlayerOne, out var blockMove, out var blockFace))
            {
                yield return ExecuteMove(blockFace, blockMove.x, blockMove.y);
                yield break;
            }

            // Forks
            var currentFace = _cubeM.GetActiveFaceNormal();
            var currentMoves = _cubeM.CollectMovesOnFace(board, currentFace);
            var forks = _gameM.Board.GetForkMoves(currentMoves, TokenType.PlayerTwo);
            if (forks.Count > 0)
            {
                var choice = forks[Random.Range(0, forks.Count)];
                var faceForChoice = _cubeM.DetermineFaceForDrop(board, choice.x, choice.y);
                yield return ExecuteMove(faceForChoice, choice.x, choice.y);
                yield break;
            }

            // Rotate
            if (_rotationChance > 0f && Random.value < _rotationChance)
            {
                var randomFace = PickDifferentFace(currentFace);
                if (randomFace != Vector3Int.zero)
                    yield return RotateOnly(randomFace);
            }

            // Minimax
            var candidates = _cubeM.CollectMovesOnFace(board, currentFace);

            if (candidates.Count == 0)
                candidates = _cubeM.GetSideValidMoves(board);

            if (candidates.Count == 0)
                candidates = _cubeM.GetAllValidMoves(board);

            if (candidates.Count == 0)
            {
                Debug.LogWarning("AI: no valid candidates available. Aborting AI move.");
                yield break;
            }

            Vector2Int bestMove = new Vector2Int(-1, -1);
            int bestScore = int.MinValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                var m = candidates[i];
                int cx = m.x;
                int cz = m.y;

                if (cx < 0 || cx >= _cubeM.sizeX || cz < 0 || cz >= _cubeM.sizeZ)
                    continue;

                _gameM.Board.ApplyMove(cx, cz, TokenType.PlayerTwo);
                int score = Minimax(_maxDepth - 1, false);
                _gameM.Board.UndoMove(cx, cz);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = m;
                }
            }

            if (bestMove.x < 0 || bestMove.y < 0)
            {
                bestMove = candidates[Random.Range(0, candidates.Count)];
            }

            int bx = bestMove.x;
            int bz = bestMove.y;
            if (bx < 0 || bx >= _cubeM.sizeX || bz < 0 || bz >= _cubeM.sizeZ)
            {
                Debug.LogWarning($"AI: bestMove out of range ({bx},{bz}). Aborting AI move.");
                yield break;
            }

            var faceForBest = _cubeM.DetermineFaceForDrop(board, bx, bz);
            yield return ExecuteMove(faceForBest, bx, bz);
        }

        int EvaluateBoard()
        {
            Vector2 centre = new Vector2((_cubeM.sizeX - 1) / 2f, (_cubeM.sizeZ - 1) / 2f);
            int score = 0;
            var board = _gameM.Board;
            var moves = _cubeM.GetAllValidMoves(board);
            for (int i = 0; i < moves.Count; i++)
            {
                var mv = moves[i];
                Vector2 cell = new Vector2(mv.x, mv.y);
                float distSq = (cell - centre).sqrMagnitude;
                score -= Mathf.RoundToInt(distSq);
            }
            return score;
        }

        int Minimax(int depth, bool isMaximizing)
        {
            if (_gameM.Board.CheckAnyWin(TokenType.PlayerTwo))
                return 1000 - (_maxDepth - depth);

            if (_gameM.Board.CheckAnyWin(TokenType.PlayerOne))
                return -1000 + (_maxDepth - depth);

            if (depth == 0)
                return EvaluateBoard();

            var board = _gameM.Board;
            var moves = _cubeM.GetAllValidMoves(board);

            if (moves.Count == 0)
                return 0;

            if (isMaximizing)
            {
                int best = int.MinValue;
                for (int i = 0; i < moves.Count; i++)
                {
                    var m = moves[i];
                    _gameM.Board.ApplyMove(m.x, m.y, TokenType.PlayerTwo);
                    int val = Minimax(depth - 1, false);
                    _gameM.Board.UndoMove(m.x, m.y);
                    if (val > best) best = val;
                }
                return best;
            }
            else
            {
                int best = int.MaxValue;
                for (int i = 0; i < moves.Count; i++)
                {
                    var m = moves[i];
                    _gameM.Board.ApplyMove(m.x, m.y, TokenType.PlayerOne);
                    int val = Minimax(depth - 1, true);
                    _gameM.Board.UndoMove(m.x, m.y);
                    if (val < best) best = val;
                }
                return best;
            }
        }

        bool TryFindImmediateMove(TokenType token, out Vector2Int move, out Vector3Int face)
        {
            var board = _gameM.Board;

            for (int i = 0; i < _faces.Length; i++)
            {
                var f = _faces[i];
                var moves = _cubeM.CollectMovesOnFace(board, f);
                for (int j = 0; j < moves.Count; j++)
                {
                    var m = moves[j];
                    if (_gameM.Board.IsWinningMove(m.x, m.y, token))
                    {
                        move = m; face = f; return true;
                    }
                }
            }
            move = default; face = default; return false;
        }

        IEnumerator ExecuteMove(Vector3Int face, int x, int z)
        {
            yield return _cubeM.RotateToFace(face);
            yield return new WaitForSeconds(_cubeM.RotationPause);

            _gameM.StartCoroutine(_gameM.DropToken(x, z));
            _gameM.SetState(new ResolvingState());
        }

        IEnumerator RotateOnly(Vector3Int face)
        {
            yield return _cubeM.RotateToFace(face);
            yield return new WaitForSeconds(_cubeM.RotationPause);
        }

        Vector3Int PickDifferentFace(Vector3Int current)
        {
            int count = 0;
            for (int i = 0; i < _faces.Length; i++)
                if (_faces[i] != current) count++;

            if (count == 0) return Vector3Int.zero;

            int idx = Random.Range(0, count);
            for (int i = 0; i < _faces.Length; i++)
            {
                var f = _faces[i];
                if (f == current) continue;
                if (idx == 0) return f;
                idx--;
            }
            return Vector3Int.zero;
        }
    }
}
