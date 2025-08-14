using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// Handles token falling.
    /// </summary>
    public class TokenDropper : ITokenDropper
    {
        #region Dependencies
        readonly GameTuning _tuning;
        readonly CubeManager _cubeM;
        readonly BlackHoleManager _bhM;
        readonly IGameVfx _vfx;         
        readonly IAudioService _audio;
        readonly IBoardRules _rules;
        #endregion

        public TokenDropper(
            CubeManager cube,
            BlackHoleManager holes,
            IAudioService audio,
            IBoardRules rules,
            GameTuning tuning,
            IGameVfx vfx)
        {
            _cubeM = cube;
            _bhM = holes;
            _audio = audio;
            _rules = rules;
            _tuning = tuning;
            _vfx = vfx;
        }

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

            if (!_rules.InBounds(board, x, z))
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

                while (nextPassIndex < passList.Count &&
                       token.transform.position.y <= passList[nextPassIndex].worldY)
                {
                    int passY = passList[nextPassIndex].y;

                    _audio?.PlayPassThrough();

                    if (_vfx != null && _cubeM.Cells[x, passY, z] != null)
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
                        _bhM.RemoveBlackHole(holeSrc.Value);

                        int targetX = (holeSrc.Value.x == 0 || holeSrc.Value.x == _cubeM.sizeX - 1)
                            ? _cubeM.sizeX - 1 - holeSrc.Value.x : x;
                        int targetZ = (holeSrc.Value.z == 0 || holeSrc.Value.z == _cubeM.sizeZ - 1)
                            ? _cubeM.sizeZ - 1 - holeSrc.Value.z : z;

                        int targetY = _rules.FindDropY(board, targetX, targetZ);
                        if (targetY < 0) yield break;

                        Vector3 topOpp = _cubeM.GetCellWorldPosition(targetX, _cubeM.sizeY - 1, targetZ)
                                         + Vector3.up * (_tuning != null ? _tuning.dropHeight : 1.8f);

                        if (_vfx != null) yield return _vfx.ScaleWarp(token.transform, topOpp);
                        else token.transform.position = topOpp;

                        _audio?.PlayTokenLand();

                        x = targetX; z = targetZ; y = targetY;
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

        #region Private Helpers
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
