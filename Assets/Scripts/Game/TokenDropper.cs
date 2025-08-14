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
        #region Ctor deps
        readonly GameTuning _tuning;
        readonly CubeManager _cubeM;
        readonly BlackHoleManager _bhM;
        readonly GameVfx _vfx;
        readonly IAudioService _audio;
        readonly IBoardRules _rules;

        public TokenDropper(CubeManager cube, BlackHoleManager holes, IAudioService audio, IBoardRules rules, GameTuning tuning, GameVfx vfx = null)
        {
            _cubeM = cube;
            _bhM = holes;
            _audio = audio;
            _tuning = tuning;
            _rules = rules;
            _vfx = vfx;
        }
        #endregion

        #region Public API
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

            if (!InBounds(board, x, z))
            {
                Debug.LogWarning($"TokenDropper.Drop: invalid start indices x:{x} z:{z} (board {board.sizeX}x{board.sizeZ}). Aborting drop.");
                yield break;
            }

            int y = _rules.FindDropY(board, x, z);
            if (y < 0) yield break;

            var placed = currentPlayer == 0 ? TokenTypes.PlayerOne : TokenTypes.PlayerTwo;

            // Spawn token above the column
            Vector3 topWorld = _cubeM.GetCellWorldPosition(x, _cubeM.sizeY - 1, z);
            var prefab = factory.PickPrefab(mode, currentPlayer);
            var token = UnityEngine.Object.Instantiate(
                prefab,
                topWorld + Vector3.up * (_tuning != null ? _tuning.dropHeight : 1.8f),
                prefab.transform.rotation,
                _cubeM.CubeContainer
            );
            factory.PostSpawnVisual(mode, currentPlayer, token, aiTwoMaterial);

            _audio?.PlayTokenLand();

            var passList = BuildPassList(x, z, y);
            int nextPassIndex = 0;

            Vector3Int? holeSrc = FindHighestBlackHoleOnColumn(x, z);
            bool warped = false;

            float dropSpeed = _tuning != null ? _tuning.dropSpeed : 8f;

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
                    if (_cubeM.Cells[x, passY, z] != null)
                        yield return _vfx.BlinkCell(x, passY, z, dropSpeed);
                    nextPassIndex++;
                }

                // Warp if crossing a black hole’s y
                if (!warped && holeSrc.HasValue && _bhM != null && _bhM.BlackHoles.TryGetValue(holeSrc.Value, out var data))
                {
                    float holeY = data.Instance.transform.position.y;
                    if (token.transform.position.y <= holeY)
                    {
                        warped = true;
                        _audio?.PlayWarp();

                        yield return _vfx.ScaleWarp(token.transform, token.transform.position);

                        _bhM.RemoveBlackHole(holeSrc.Value);

                        x = (holeSrc.Value.x == 0 || holeSrc.Value.x == _cubeM.sizeX - 1)
                            ? _cubeM.sizeX - 1 - holeSrc.Value.x : x;
                        z = (holeSrc.Value.z == 0 || holeSrc.Value.z == _cubeM.sizeZ - 1)
                            ? _cubeM.sizeZ - 1 - holeSrc.Value.z : z;

                        y = _rules.FindDropY(board, x, z);
                        if (y < 0) yield break;

                        Vector3 topOpp = _cubeM.GetCellWorldPosition(x, _cubeM.sizeY - 1, z);
                        token.transform.position = topOpp + Vector3.up * (_tuning != null ? _tuning.dropHeight : 1.8f);
                        _audio?.PlayTokenLand();

                        passList = BuildPassList(x, z, y);
                        nextPassIndex = 0;

                        continue;
                    }
                }

                Vector3 targetPos = _cubeM.GetCellWorldPosition(x, y, z);
                if (token.transform.position.y <= targetPos.y + 0.01f)
                {
                    token.transform.position = targetPos;
                    break;
                }

                yield return null;
            }

            int appliedY = _rules.ApplyMove(board, x, z, placed);
            if (appliedY >= 0) y = appliedY;

            var oldCell = _cubeM.Cells[x, y, z];
            if (oldCell != null)
            {
                var rend = oldCell.GetComponent<MeshRenderer>();
                if (rend != null) rend.enabled = false;
            }
            _cubeM.SetCellVisible(x, y, z, false);

            onPlaced?.Invoke(placed, x, y, z);
        }
        #endregion

        #region Private helpers
        static bool InBounds(BoardModel b, int x, int z)
            => x >= 0 && x < b.sizeX && z >= 0 && z < b.sizeZ;

        List<(int y, float worldY)> BuildPassList(int cx, int cz, int yDest)
        {
            var list = new List<(int y, float worldY)>();
            for (int yy = _cubeM.sizeY - 1; yy > yDest; yy--)
            {
                var world = _cubeM.GetCellWorldPosition(cx, yy, cz);
                list.Add((yy, world.y));
            }
            return list;
        }

        Vector3Int? FindHighestBlackHoleOnColumn(int x, int z)
        {
            if (_bhM == null) return null;

            Vector3Int? best = null;
            foreach (var kv in _bhM.BlackHoles)
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
