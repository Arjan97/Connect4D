using UnityEngine;

namespace QuantumConnect
{
    /// <summary>Common cube face normals + shared helpers.</summary>
    public static class Face
    {
        public static readonly Vector3Int Front = new Vector3Int(0, 0, 1);
        public static readonly Vector3Int Back = new Vector3Int(0, 0, -1);
        public static readonly Vector3Int Right = new Vector3Int(1, 0, 0);
        public static readonly Vector3Int Left = new Vector3Int(-1, 0, 0);
        public static readonly Vector3Int Up = new Vector3Int(0, 1, 0);
        public static readonly Vector3Int Down = new Vector3Int(0, -1, 0);

        /// <summary>Clockwise order: Front(0), Right(1), Back(2), Left(3). Returns -1 if non-horizontal.</summary>
        public static int FaceIndex(Vector3Int f)
        {
            if (f == Front) return 0;
            if (f == Right) return 1;
            if (f == Back) return 2;
            if (f == Left) return 3;
            return -1; // Up/Down/invalid for horizontal rotations
        }
    }
}
