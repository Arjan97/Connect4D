using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    public static class GridUtils
    {
        public static bool IsOuter(int x, int y, int z, int sx, int sy, int sz)
        {
            return x == 0 || x == sx - 1 || y == 0 || y == sy - 1 || z == 0 || z == sz - 1;
        }

        public static IEnumerable<Vector3Int> ShellCoords(int sx, int sy, int sz)
        {
            for (int x = 0; x < sx; x++)
                for (int y = 0; y < sy; y++)
                    for (int z = 0; z < sz; z++)
                        if (x == 0 || x == sx - 1 || y == 0 || y == sy - 1 || z == 0 || z == sz - 1)
                            yield return new Vector3Int(x, y, z);
        }

        public static int ShellCount(int sx, int sy, int sz)
        {
            int ix = Mathf.Max(0, sx - 2);
            int iy = Mathf.Max(0, sy - 2);
            int iz = Mathf.Max(0, sz - 2);
            return sx * sy * sz - ix * iy * iz;
        }

        // Lateral shell only (exclude top/bottom)
        public static IEnumerable<Vector3Int> LateralShellCoords(int sx, int sy, int sz)
        {
            for (int x = 0; x < sx; x++)
                for (int y = 0; y < sy; y++)
                    for (int z = 0; z < sz; z++)
                        if (x == 0 || x == sx - 1 || z == 0 || z == sz - 1)
                            yield return new Vector3Int(x, y, z);
        }

        public static int LateralShellCount(int sx, int sy, int sz)
        {
            int ix = Mathf.Max(0, sx - 2);
            int iz = Mathf.Max(0, sz - 2);
            return sx * sy * sz - ix * sy * iz; // 4x4x4 -> 48
        }

        public static IEnumerable<Vector3Int> OuterCoords(int sx, int sy, int sz)
        {
            for (int x = 0; x < sx; x++)
                for (int y = 0; y < sy; y++)
                    for (int z = 0; z < sz; z++)
                        if (IsOuter(x, y, z, sx, sy, sz))
                            yield return new Vector3Int(x, y, z);
        }
        public static Vector3 CenterOffset(int sizeX, int sizeY, int sizeZ, Vector3 spacing)
        {
            return new Vector3(
                (sizeX - 1) * spacing.x * 0.5f,
                (sizeY - 1) * spacing.y * 0.5f,
                (sizeZ - 1) * spacing.z * 0.5f
            );
        }

        public static Vector3 LocalFromIndices(int x, int y, int z, Vector3 spacing, Vector3 centerOffset)
            => new Vector3(x * spacing.x, y * spacing.y, z * spacing.z) - centerOffset;

        public static Vector3 WorldFromIndices(Transform container, int x, int y, int z,
                                               int sizeX, int sizeY, int sizeZ, Vector3 spacing)
        {
            var center = CenterOffset(sizeX, sizeY, sizeZ, spacing);
            var local = LocalFromIndices(x, y, z, spacing, center);
            return container.TransformPoint(local);
        }
    }
}
