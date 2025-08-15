using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// Handles token falling (with per-layer blink), optional black-hole warp, and board write.
    /// </summary>
    public class TokenDropper : ITokenDropper
    {
        #region Deps
        readonly GameTuning tuning;
        readonly CubeManager cube;
        readonly BlackHoleManager holes;
        readonly IGameVfx vfx;
        readonly IAudioService audio;
        readonly IBoardRules rules;

        public TokenDropper(CubeManager c, BlackHoleManager h, IAudioService a, IBoardRules r, GameTuning t, IGameVfx fx)
        {
            cube = c;
            holes = h;
            audio = a;
            tuning = t;
            rules = r;
            vfx = fx;
        }
        #endregion

        #region API
        public IEnumerator Drop(
            BoardModel board,
            int currentPlayer,
            GameModes mode,
            Vector3Int startXZ,
            ITokenFactory factory,
            Material aiTwoMaterial,
            Action<TokenTypes, int, int, int> onPlaced)
        {
            int x = startXZ.x;
            int z = startXZ.z;
            if (!(x >= 0 && x < board.sizeX && z >= 0 && z < board.sizeZ))
                yield break;

            int y = rules.FindDropY(board, x, z);
            if (y < 0) yield break;

            var placed = currentPlayer == 0 ? TokenTypes.PlayerOne : TokenTypes.PlayerTwo;

            // Spawn token above the column
            Vector3 topWorld = cube.GetCellWorldPosition(x, cube.sizeY - 1, z);
            var prefab = factory.PickPrefab(mode, currentPlayer);
            var token = factory.SpawnToken(
                prefab,
                cube.CubeContainer,
                topWorld + Vector3.up * (tuning ? tuning.dropHeight : 1.8f),
                prefab != null ? prefab.transform.rotation : Quaternion.identity
            );
            factory.PostSpawnVisual(mode, currentPlayer, token, aiTwoMaterial);

            if (token != null)
                audio?.PlayTokenLandAt(token.transform.position);

            var passList = BuildPassList(x, z, y);
            int nextPassIndex = 0;

            Vector3Int? holeSrc = FindHighestBlackHoleOnColumn(x, z);
            bool warped = false;

            float dropSpeed = tuning ? tuning.dropSpeed : 8f;

            // Fall loop
            while (true)
            {
                if (token == null) yield break;

                token.transform.position += Vector3.down * dropSpeed * Time.deltaTime;

                // Blink per layer as token passes it
                while (nextPassIndex < passList.Count &&
                       token.transform.position.y <= passList[nextPassIndex].worldY)
                {
                    int passY = passList[nextPassIndex].y;
                    if (cube.Cells[x, passY, z] != null)
                        yield return vfx.BlinkCell(x, passY, z, dropSpeed);
                    nextPassIndex++;
                }

                // Warp if crossing a black hole’s y
                if (!warped && holeSrc.HasValue && holes != null && holes.BlackHoles.TryGetValue(holeSrc.Value, out var data))
                {
                    float holeY = data.Instance.transform.position.y;
                    if (token.transform.position.y <= holeY)
                    {
                        warped = true;
                        audio?.PlayWarpAt(token.transform.position);

                        // shrink → teleport → expand
                        yield return vfx.ScaleWarp(token.transform, token.transform.position);

                        holes.RemoveBlackHole(holeSrc.Value);

                        x = (holeSrc.Value.x == 0 || holeSrc.Value.x == cube.sizeX - 1)
                            ? cube.sizeX - 1 - holeSrc.Value.x : x;
                        z = (holeSrc.Value.z == 0 || holeSrc.Value.z == cube.sizeZ - 1)
                            ? cube.sizeZ - 1 - holeSrc.Value.z : z;

                        y = rules.FindDropY(board, x, z);
                        if (y < 0) yield break;

                        Vector3 topOpp = cube.GetCellWorldPosition(x, cube.sizeY - 1, z);
                        token.transform.position = topOpp + Vector3.up * (tuning ? tuning.dropHeight : 1.8f);
                        audio?.PlayTokenLandAt(token.transform.position);

                        passList = BuildPassList(x, z, y);
                        nextPassIndex = 0;

                        continue;
                    }
                }

                Vector3 targetPos = cube.GetCellWorldPosition(x, y, z);
                if (token.transform.position.y <= targetPos.y + 0.01f)
                {
                    token.transform.position = targetPos;
                    break;
                }

                yield return null;
            }

            int appliedY = rules.ApplyMove(board, x, z, placed);
            if (appliedY >= 0) y = appliedY;

            var oldCell = cube.Cells[x, y, z];
            if (oldCell != null)
            {
                var rend = oldCell.GetComponent<MeshRenderer>();
                if (rend != null) rend.enabled = false;
            }
            cube.SetCellVisible(x, y, z, false);

            onPlaced?.Invoke(placed, x, y, z);
        }
        #endregion

        #region Helpers
        List<(int y, float worldY)> BuildPassList(int cx, int cz, int yDest)
        {
            var list = new List<(int y, float worldY)>();
            for (int yy = cube.sizeY - 1; yy > yDest; yy--)
            {
                var world = cube.GetCellWorldPosition(cx, yy, cz);
                list.Add((yy, world.y));
            }
            return list;
        }

        Vector3Int? FindHighestBlackHoleOnColumn(int x, int z)
        {
            if (holes == null) return null;

            Vector3Int? best = null;
            foreach (var kv in holes.BlackHoles)
            {
                var s = kv.Key;
                if (s.x == x && s.z == z && (!best.HasValue || s.y > best.Value.y))
                    best = s;
            }
            return best;
        }
        #endregion
    }
}
