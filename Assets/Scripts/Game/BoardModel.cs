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
    }
}
