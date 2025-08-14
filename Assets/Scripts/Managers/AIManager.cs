using System.Collections;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// AI coordinator: evaluates the board and instructs CubeManager to rotate and drop.
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

            if (_gameM == null || _cubeM == null)
            {
                Debug.LogWarning("AIManager: missing GameManager or CubeManager in this scene.");
                return;
            }
            if (!_gameM.Turns.IsAiTurn(_gameM.Mode)) return;

            StartCoroutine(MakeMoveRoutine());
        }

        IEnumerator MakeMoveRoutine()
        {
            yield return new WaitForSeconds(_moveDelay);
            if (_gameM == null || _cubeM == null) yield break;

            var board = _gameM.Board;
            var rules = _gameM.Rules;

            // Try to win immediately.
            if (TryFindImmediateMove(TokenTypes.PlayerTwo, out var winMove, out var winFace))
            {
                yield return ExecuteMove(winFace, winMove.x, winMove.y);
                yield break;
            }

            // Block opponent’s immediate win.
            if (TryFindImmediateMove(TokenTypes.PlayerOne, out var blockMove, out var blockFace))
            {
                yield return ExecuteMove(blockFace, blockMove.x, blockMove.y);
                yield break;
            }

            // Create a fork if possible (face-aware).
            var currentFace = _cubeM.GetActiveFaceNormal();
            var currentMoves = _cubeM.CollectMovesOnFace(board, currentFace);
            var forks = rules.GetForkMoves(board, currentMoves, TokenTypes.PlayerTwo);
            if (forks.Count > 0)
            {
                var choice = forks[Random.Range(0, forks.Count)];
                var faceForChoice = _cubeM.DetermineFaceForDrop(board, choice.x, choice.y);
                yield return ExecuteMove(faceForChoice, choice.x, choice.y);
                yield break;
            }

            if (_rotationChance > 0f && Random.value < _rotationChance)
            {
                var randomFace = PickDifferentFace(currentFace);
                if (randomFace != Vector3Int.zero)
                    yield return RotateOnly(randomFace);
            }

            // Evaluate candidates (prefer current face -> edges -> anywhere).
            var candidates = _cubeM.CollectMovesOnFace(board, currentFace);
            if (candidates.Count == 0) candidates = _cubeM.GetSideValidMoves(board);
            if (candidates.Count == 0) candidates = _cubeM.GetAllValidMoves(board);

            if (candidates.Count == 0)
            {
                Debug.LogWarning("AI: no valid candidates available. Aborting AI move.");
                yield break;
            }

            // Minimax pick.
            Vector2Int bestMove = new(-1, -1);
            int bestScore = int.MinValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                var m = candidates[i];
                if (m.x < 0 || m.x >= _cubeM.sizeX || m.y < 0 || m.y >= _cubeM.sizeZ)
                    continue;

                rules.ApplyMove(board, m.x, m.y, TokenTypes.PlayerTwo);
                int score = Minimax(_maxDepth - 1, false);
                rules.UndoMove(board, m.x, m.y);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = m;
                }
            }

            if (bestMove.x < 0 || bestMove.y < 0)
                bestMove = candidates[Random.Range(0, candidates.Count)];

            var faceForBest = _cubeM.DetermineFaceForDrop(board, bestMove.x, bestMove.y);
            yield return ExecuteMove(faceForBest, bestMove.x, bestMove.y);
        }

        int EvaluateBoard()
        {
            // Slight center preference.
            Vector2 centre = new Vector2((_cubeM.sizeX - 1) / 2f, (_cubeM.sizeZ - 1) / 2f);
            int score = 0;

            var moves = _cubeM.GetAllValidMoves(_gameM.Board);
            for (int i = 0; i < moves.Count; i++)
            {
                var mv = moves[i];
                Vector2 p = new Vector2(mv.x, mv.y);
                score -= Mathf.RoundToInt((p - centre).sqrMagnitude);
            }
            return score;
        }

        int Minimax(int depth, bool isMaximizing)
        {
            var board = _gameM.Board;
            var rules = _gameM.Rules;

            if (rules.CheckAnyWin(board, TokenTypes.PlayerTwo, out _)) return 1000 - (_maxDepth - depth);
            if (rules.CheckAnyWin(board, TokenTypes.PlayerOne, out _)) return -1000 + (_maxDepth - depth);
            if (depth == 0) return EvaluateBoard();

            var moves = _cubeM.GetAllValidMoves(board);
            if (moves.Count == 0) return 0;

            if (isMaximizing)
            {
                int best = int.MinValue;
                for (int i = 0; i < moves.Count; i++)
                {
                    var m = moves[i];
                    rules.ApplyMove(board,m.x, m.y, TokenTypes.PlayerTwo);
                    int val = Minimax(depth - 1, false);
                    rules.UndoMove(board,m.x, m.y);
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
                    rules.ApplyMove(board, m.x, m.y, TokenTypes.PlayerOne);
                    int val = Minimax(depth - 1, true);
                    rules.UndoMove(board, m.x, m.y);
                    if (val < best) best = val;
                }
                return best;
            }
        }

        bool TryFindImmediateMove(TokenTypes token, out Vector2Int move, out Vector3Int face)
        {
            var board = _gameM.Board;
            var rules = _gameM.Rules;

            for (int i = 0; i < _faces.Length; i++)
            {
                var f = _faces[i];
                var moves = _cubeM.CollectMovesOnFace(board, f);
                for (int j = 0; j < moves.Count; j++)
                {
                    var m = moves[j];
                    if (rules.IsWinningMove(board, m.x, m.y, token))
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
