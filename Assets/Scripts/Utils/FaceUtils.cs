using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// Centralized helpers for face math (camera-facing, indexing, membership).
    /// </summary>
    public static class FaceUtils
    {
        /// <summary>Map a face normal to a clockwise index (Front=0, Right=1, Back=2, Left=3). Returns -1 if unknown.</summary>
        public static int Index(Vector3Int f)
        {
            if (f == Face.Front) return 0;
            if (f == Face.Right) return 1;
            if (f == Face.Back) return 2;
            if (f == Face.Left) return 3;
            return -1;
        }

        /// <summary>
        /// Which face of the cube is the camera in front of? (XZ plane only; ignores Y).
        /// </summary>
        public static Vector3Int ActiveFaceNormal(Transform cubeContainer, Camera cam = null)
        {
            if (!cubeContainer) return Face.Front;
            cam ??= Camera.main;
            if (!cam) return Face.Front;

            Vector3 toCam = cam.transform.position - cubeContainer.position;
            Vector3 local = Quaternion.Inverse(cubeContainer.rotation) * toCam;
            local.y = 0f;
            if (local.sqrMagnitude > 0f) local.Normalize();

            if (Mathf.Abs(local.z) >= Mathf.Abs(local.x))
                return local.z >= 0f ? Face.Front : Face.Back;
            return local.x >= 0f ? Face.Right : Face.Left;
        }

        /// <summary>
        /// Is a (x,y,z) cell considered to be on the given face? (checks X/Z edges only)
        /// </summary>
        public static bool IsOnFace(Vector3Int coord, Vector3Int face, int sizeX, int sizeZ)
        {
            if (face.x < 0 && coord.x != 0) return false;
            if (face.x > 0 && coord.x != sizeX - 1) return false;
            if (face.z < 0 && coord.z != 0) return false;
            if (face.z > 0 && coord.z != sizeZ - 1) return false;
            return true;
        }

        /// <summary>
        /// Given a column (x,z) and its next drop Y, which face would that placement appear on?
        /// Returns one of Face.Left/Right/Back/Front/Down/Up.
        /// </summary>
        public static Vector3Int DetermineFaceForDrop(int x, int dropY, int z, int sizeX, int sizeZ)
        {
            if (x == 0) return Face.Left;
            if (x == sizeX - 1) return Face.Right;
            if (z == 0) return Face.Back;
            if (z == sizeZ - 1) return Face.Front;
            if (dropY == 0) return Face.Down;
            return Face.Up;
        }
    }
}
