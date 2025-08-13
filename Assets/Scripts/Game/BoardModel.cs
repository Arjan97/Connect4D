using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// Mutable 3D token grid + basic rule helpers used by AI/Cube/BlackHole.
    /// </summary>
    public class BoardModel
    {
        public readonly int sizeX;
        public readonly int sizeY;
        public readonly int sizeZ;

        public TokenTypes[,,] cells;

        static readonly Vector3Int[] _dirs =
        {
            new(1,0,0),  new(0,1,0),  new(0,0,1),
            new(1,1,0),  new(1,-1,0),
            new(1,0,1),  new(1,0,-1),
            new(0,1,1),  new(0,1,-1),
            new(1,1,1),  new(1,1,-1),
            new(1,-1,1), new(1,-1,-1)
        };

        public BoardModel(int x, int y, int z)
        {
            sizeX = x; sizeY = y; sizeZ = z;
            cells = new TokenTypes[x, y, z];
            Clear();
        }

        /// <summary>Indexer so callers can use board[x,y,z].</summary>
        public TokenTypes this[int x, int y, int z]
        {
            get => cells[x, y, z];
            set => cells[x, y, z] = value;
        }

        public void Clear()
        {
            for (int x = 0; x < sizeX; x++)
                for (int y = 0; y < sizeY; y++)
                    for (int z = 0; z < sizeZ; z++)
                        cells[x, y, z] = TokenTypes.None;
        }

        public int GetDropY(int x, int z)
        {
            for (int y = 0; y < sizeY; y++)
                if (cells[x, y, z] == TokenTypes.None) return y;
            return -1;
        }

        public int ApplyMove(int x, int z, TokenTypes t)
        {
            int y = GetDropY(x, z);
            if (y < 0) return -1;
            cells[x, y, z] = t;
            return y;
        }

        public void UndoMove(int x, int z)
        {
            for (int y = sizeY - 1; y >= 0; y--)
                if (cells[x, y, z] != TokenTypes.None)
                {
                    cells[x, y, z] = TokenTypes.None;
                    return;
                }
        }

        public bool IsWinningMove(int x, int z, TokenTypes t)
        {
            int y = GetDropY(x, z);
            if (y < 0) return false;
            cells[x, y, z] = t;
            bool win = CheckWin(x, y, z, t, out _);
            cells[x, y, z] = TokenTypes.None;
            return win;
        }

        public bool CheckAnyWin(TokenTypes t) => CheckAnyWin(t, out _);

        public bool CheckAnyWin(TokenTypes t, out List<Vector3Int> winLine)
        {
            for (int x = 0; x < sizeX; x++)
                for (int y = 0; y < sizeY; y++)
                    for (int z = 0; z < sizeZ; z++)
                        if (cells[x, y, z] == t && CheckWin(x, y, z, t, out winLine))
                            return true;

            winLine = null;
            return false;
        }

        public bool CheckWin(int x, int y, int z, TokenTypes t, out List<Vector3Int> line, int length = 4)
        {
            for (int i = 0; i < _dirs.Length; i++)
            {
                var dir = _dirs[i];
                int c1 = CountDirection(x, y, z, dir, t);
                int c2 = CountDirection(x, y, z, new Vector3Int(-dir.x, -dir.y, -dir.z), t);
                if (c1 + c2 + 1 >= length)
                {
                    line = new List<Vector3Int>(length);
                    var start = new Vector3Int(x - dir.x * c2, y - dir.y * c2, z - dir.z * c2);
                    for (int k = 0; k < length; k++) line.Add(start + dir * k);
                    return true;
                }
            }
            line = null;
            return false;
        }

        int CountDirection(int x, int y, int z, Vector3Int dir, TokenTypes t)
        {
            int count = 0;
            int nx = x + dir.x, ny = y + dir.y, nz = z + dir.z;
            while (nx >= 0 && nx < sizeX && ny >= 0 && ny < sizeY && nz >= 0 && nz < sizeZ)
            {
                if (cells[nx, ny, nz] == t) count++;
                else break;
                nx += dir.x; ny += dir.y; nz += dir.z;
            }
            return count;
        }

        public bool HasAnyEmpty()
        {
            for (int x = 0; x < sizeX; x++)
                for (int y = 0; y < sizeY; y++)
                    for (int z = 0; z < sizeZ; z++)
                        if (cells[x, y, z] == TokenTypes.None) return true;
            return false;
        }

        public List<Vector2Int> GetForkMoves(List<Vector2Int> validMoves, TokenTypes t)
        {
            var forks = new List<Vector2Int>();
            for (int i = 0; i < validMoves.Count; i++)
            {
                var m = validMoves[i];
                int y = GetDropY(m.x, m.y);
                if (y < 0) continue;

                cells[m.x, y, m.y] = t;

                int count = 0;
                for (int j = 0; j < validMoves.Count; j++)
                {
                    var n = validMoves[j];
                    if (n.x == m.x && n.y == m.y) continue;
                    if (IsWinningMove(n.x, n.y, t))
                    {
                        count++;
                        if (count >= 2) break;
                    }
                }

                cells[m.x, y, m.y] = TokenTypes.None;
                if (count >= 2) forks.Add(m);
            }
            return forks;
        }
    }
}
