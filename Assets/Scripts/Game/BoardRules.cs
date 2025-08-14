using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>Pure board rules. </summary>
    public class BoardRules : IBoardRules
    {
        const int WinLength = 4;

        static readonly Vector3Int[] _dirs = {
            new(1,0,0),  new(0,1,0),  new(0,0,1),
            new(1,1,0),  new(1,-1,0),
            new(1,0,1),  new(1,0,-1),
            new(0,1,1),  new(0,1,-1),
            new(1,1,1),  new(1,1,-1),
            new(1,-1,1), new(1,-1,-1)
        };

        public int FindDropY(BoardModel b, int x, int z)
        {
            for (int y = 0; y < b.sizeY; y++)
                if (b.cells[x, y, z] == TokenTypes.None) return y;
            return -1;
        }

        public int ApplyMove(BoardModel b, int x, int z, TokenTypes t)
        {
            int y = FindDropY(b, x, z);
            if (y >= 0) b.cells[x, y, z] = t;
            return y;
        }

        public void UndoMove(BoardModel b, int x, int z)
        {
            for (int y = b.sizeY - 1; y >= 0; y--)
                if (b.cells[x, y, z] != TokenTypes.None) { b.cells[x, y, z] = TokenTypes.None; return; }
        }

        public bool IsWinningMove(BoardModel b, int x, int z, TokenTypes t)
        {
            int y = FindDropY(b, x, z);
            if (y < 0) return false;
            b.cells[x, y, z] = t;
            bool win = CheckWin(b, x, y, z, t, out _);
            b.cells[x, y, z] = TokenTypes.None;
            return win;
        }

        public bool CheckAnyWin(BoardModel b, TokenTypes t, out List<Vector3Int> line)
        {
            for (int x = 0; x < b.sizeX; x++)
                for (int y = 0; y < b.sizeY; y++)
                    for (int z = 0; z < b.sizeZ; z++)
                        if (b.cells[x, y, z] == t && CheckWin(b, x, y, z, t, out line))
                            return true;
            line = null; return false;
        }

        public bool HasAnyEmpty(BoardModel b)
        {
            for (int x = 0; x < b.sizeX; x++)
                for (int y = 0; y < b.sizeY; y++)
                    for (int z = 0; z < b.sizeZ; z++)
                        if (b.cells[x, y, z] == TokenTypes.None) return true;
            return false;
        }
        public bool InBounds(BoardModel b, int x, int z)
            => b != null
            && x >= 0
            && x < b.sizeX
            && z >= 0
            && z < b.sizeZ;

        public bool InBounds(BoardModel b, int x, int y, int z)
            => b != null
            && x >= 0 && x < b.sizeX
            && y >= 0 && y < b.sizeY
            && z >= 0 && z < b.sizeZ;

        public List<Vector2Int> GetForkMoves(BoardModel b, List<Vector2Int> valid, TokenTypes t)
        {
            var forks = new List<Vector2Int>();
            for (int i = 0; i < valid.Count; i++)
            {
                var m = valid[i];
                int y = FindDropY(b, m.x, m.y);
                if (y < 0) continue;

                b.cells[m.x, y, m.y] = t;

                int count = 0;
                for (int j = 0; j < valid.Count; j++)
                {
                    var n = valid[j];
                    if (n.x == m.x && n.y == m.y) continue;
                    if (IsWinningMove(b, n.x, n.y, t)) { count++; if (count >= 2) break; }
                }
                b.cells[m.x, y, m.y] = TokenTypes.None;

                if (count >= 2) forks.Add(m);
            }
            return forks;
        }

        bool CheckWin(BoardModel b, int x, int y, int z, TokenTypes t, out List<Vector3Int> line)
        {
            for (int i = 0; i < _dirs.Length; i++)
            {
                var d = _dirs[i];
                int c1 = CountDirection(b, x, y, z, d, t);
                int c2 = CountDirection(b, x, y, z, new Vector3Int(-d.x, -d.y, -d.z), t);
                if (c1 + c2 + 1 >= WinLength)
                {
                    line = new List<Vector3Int>();
                    var start = new Vector3Int(x - d.x * c2, y - d.y * c2, z - d.z * c2);
                    for (int k = 0; k < WinLength; k++) line.Add(start + d * k);
                    return true;
                }
            }
            line = null; return false;
        }

        int CountDirection(BoardModel b, int x, int y, int z, Vector3Int dir, TokenTypes t)
        {
            int count = 0;
            int nx = x + dir.x, ny = y + dir.y, nz = z + dir.z;
            while (nx >= 0 && nx < b.sizeX && ny >= 0 && ny < b.sizeY && nz >= 0 && nz < b.sizeZ)
            {
                if (b.cells[nx, ny, nz] == t) count++; else break;
                nx += dir.x; ny += dir.y; nz += dir.z;
            }
            return count;
        }
    }
}
