using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QuantumConnect
{
    public class AIManager : MonoBehaviour
    {
        public static AIManager Instance { get; private set; }
        [Tooltip("Seconds AI waits before making its move")] public float moveDelay = 0.7f;
        [Tooltip("Probability (0-1) that AI will rotate the cube on a non-critical turn")][Range(0f, 1f)] public float rotationChance = 0.3f;
        static readonly Vector3Int[] allFaces = new[] {
        new Vector3Int( 1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int( 0, 0, 1),
        new Vector3Int( 0, 0,-1)
        };

        const int MAX_DEPTH = 4;

        void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        /// <summary>
        /// Entry point: orchestrates rotation and token placement.
        /// </summary>
        public void MakeMove()
        {
            StartCoroutine(MakeMoveRoutine());
        }

        // get every (x,z) with y >= 0
        List<Vector2Int> GetAllValidMoves()
        {
            var list = new List<Vector2Int>();
            var gm = GridManager.Instance;
            for (int x = 0; x < gm.sizeX; x++)
                for (int z = 0; z < gm.sizeZ; z++)
                    if (GameManager.Instance.GetDropY(x, z) >= 0)
                        list.Add(new Vector2Int(x, z));
            return list;
        }

        int EvaluateBoard()
        {
            // simple heuristic: center bias
            var gm = GridManager.Instance;
            Vector2 centre = new Vector2((gm.sizeX - 1) / 2f, (gm.sizeZ - 1) / 2f);
            int score = 0;
            foreach (var mv in GetAllValidMoves())
            {
                float dist = (new Vector2(mv.x, mv.y) - centre).sqrMagnitude;
                score -= (int)dist; 
            }
            return score;
        }

        int Minimax(int depth, bool isMaximizing)
        {
            if (GameManager.Instance.CheckAnyWin(TokenType.PlayerTwo))
                return 1000 - (MAX_DEPTH - depth);
            if (GameManager.Instance.CheckAnyWin(TokenType.PlayerOne))
                return -1000 + (MAX_DEPTH - depth);
            if (depth == 0)
                return EvaluateBoard();

            var moves = GetAllValidMoves();
            if (moves.Count == 0) return 0;  

            if (isMaximizing)
            {
                int best = int.MinValue;
                foreach (var m in moves)
                {
                    GameManager.Instance.ApplyMove(m.x, m.y, TokenType.PlayerTwo);
                    int val = Minimax(depth - 1, false);
                    GameManager.Instance.UndoMove(m.x, m.y);

                    best = Mathf.Max(best, val);
                }
                return best;
            }
            else
            {
                int best = int.MaxValue;
                foreach (var m in moves)
                {
                    GameManager.Instance.ApplyMove(m.x, m.y, TokenType.PlayerOne);
                    int val = Minimax(depth - 1, true);
                    GameManager.Instance.UndoMove(m.x, m.y);

                    best = Mathf.Min(best, val);
                }
                return best;
            }
        }
        public IEnumerator MakeMoveRoutine()
        {
            yield return new WaitForSeconds(moveDelay);
            var mgr = GameManager.Instance;

            //check for ai win - win
            foreach (var face in allFaces)
            {
                foreach (var m in CollectMovesOnFace(face))
                    if (GameManager.Instance.IsWinningMove(m.x, m.y, TokenType.PlayerTwo))
                    {
                        yield return RotateToFace(face);
                        yield return new WaitForSeconds(0.2f);
                        yield return DropAt(m.x, m.y);
                        yield break;
                    }
            }

            //check for player win - block
            foreach (var face in allFaces)
            {
                foreach (var m in CollectMovesOnFace(face))
                    if (GameManager.Instance.IsWinningMove(m.x, m.y, TokenType.PlayerOne))
                    {
                        yield return RotateToFace(face);
                        yield return new WaitForSeconds(0.2f);
                        yield return DropAt(m.x, m.y);
                        yield break;
                    }
            }

            var currentFace = InputManager.Instance.GetActiveFaceNormal();
            var currentMoves = CollectMovesOnFace(currentFace);
            var forks = GameManager.Instance.GetForkMoves(currentMoves, TokenType.PlayerTwo);
            if (forks.Count > 0)
            {
                var choice = forks[Random.Range(0, forks.Count)];
                yield return RotateToFace(DetermineFaceForDrop(choice.x, choice.y));
                yield return new WaitForSeconds(0.2f);
                yield return DropAt(choice.x, choice.y);
                yield break;
            }

            var candidates = GetSideValidMoves();
            Vector2Int bestMove = new Vector2Int(-1, -1);
            int bestScore = int.MinValue;
            foreach (var m in candidates)
            {
                GameManager.Instance.ApplyMove(m.x, m.y, TokenType.PlayerTwo);
                int score = Minimax(MAX_DEPTH - 1, false);
                GameManager.Instance.UndoMove(m.x, m.y);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = m;
                }
            }

            // Rotate to face and drop
            var faceForBest = DetermineFaceForDrop(bestMove.x, bestMove.y);
            yield return RotateToFace(faceForBest);
            yield return new WaitForSeconds(0.2f);
            yield return DropAt(bestMove.x, bestMove.y);
        }
        List<Vector2Int> GetSideValidMoves()
        {
            var all = GetAllValidMoves();
            var sides = all.Where(m => IsSideColumn(m.x, m.y)).ToList();
            return sides.Count > 0 ? sides : all;
        }

        // Collect playable drops on a given face normal
        List<Vector2Int> CollectMovesOnFace(Vector3Int face)
        {
            var gm = GridManager.Instance;
            var board = GameManager.Instance.Board;
            var moves = new List<Vector2Int>();

            for (int x = 0; x < gm.sizeX; x++)
                for (int z = 0; z < gm.sizeZ; z++)
                {
                    int y = GameManager.Instance.GetDropY(x, z);
                    if (y < 0) continue;

                    var coord = new Vector3Int(x, y, z);

                    if (!gm.IsOnFace(coord, face)) continue;

                    if (gm.cells[x, y, z] == null) continue;
                    if (board[x, y, z] != TokenType.None) continue;

                    moves.Add(new Vector2Int(x, z));
                }

            return moves;
        }


        /// <summary>
        /// Rotates the TimelineContainer in discrete 90° increments
        /// so that the given horizontal face (front/right/back/left)
        /// ends up facing the camera.
        /// </summary>
        IEnumerator RotateToFace(Vector3Int face)
        {
            var gm = GridManager.Instance;

            int FaceIndex(Vector3Int f)
            {
                if (f == new Vector3Int(0, 0, 1)) return 0;
                if (f == new Vector3Int(1, 0, 0)) return 1;
                if (f == new Vector3Int(0, 0, -1)) return 2;
                if (f == new Vector3Int(-1, 0, 0)) return 3;
                return -1; // top/bottom or invalid: no horizontal rotation
            }

            int current = FaceIndex(InputManager.Instance.GetActiveFaceNormal());
            int target = FaceIndex(face);
            if (current < 0 || target < 0 || current == target)
                yield break;

            // Compute how many 90° steps clockwise (right) we need:
            int diff = (target - current + 4) % 4;

            // 1 step right, 2 steps (either direction), or 1 step left
            if (diff == 1)
            {
                // RotateRight
                yield return gm.AnimateContainerRotation(Vector3.up, -90f);
                yield return new WaitForSeconds(0.2f);
            }
            else if (diff == 2)
            {
                // 180°
                yield return gm.AnimateContainerRotation(Vector3.up, -90f);
                yield return new WaitForSeconds(0.2f);
                yield return gm.AnimateContainerRotation(Vector3.up, -90f);
                yield return new WaitForSeconds(0.2f);
            }
            else if (diff == 3)
            {
                // RotateLeft
                yield return gm.AnimateContainerRotation(Vector3.up, 90f);
                yield return new WaitForSeconds(0.2f);
            }
        }
        bool IsSideColumn(int x, int z)
        {
            var gm = GridManager.Instance;
            return x == 0
                || x == gm.sizeX - 1
                || z == 0
                || z == gm.sizeZ - 1;
        }
        Vector3Int DetermineFaceForDrop(int x, int z)
        {
            var gm = GridManager.Instance;
            int y = GameManager.Instance.GetDropY(x, z);
            if (x == 0) return new Vector3Int(-1, 0, 0);
            if (x == gm.sizeX - 1) return new Vector3Int(1, 0, 0);
            if (z == 0) return new Vector3Int(0, 0, -1);
            if (z == gm.sizeZ - 1) return new Vector3Int(0, 0, 1);
            if (y == 0) return new Vector3Int(0, -1, 0);
            return new Vector3Int(0, 1, 0);
        }

        IEnumerator DropAt(int x, int z)
        {
            GameManager.Instance.HandleCellClick(x, z);
            yield return null;
        }
    }
}
