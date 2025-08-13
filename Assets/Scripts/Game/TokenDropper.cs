using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// Handles token falling, black-hole warp, exact per-layer pass blink, and board write.
    /// </summary>
    public class TokenDropper : ITokenDropper
    {
        readonly GameTuning _tuning;
        readonly CubeManager _cubeM;
        readonly BlackHoleManager _bhM;
        readonly IAudioPlayer _audio;

        public TokenDropper(CubeManager cube, BlackHoleManager holes, IAudioPlayer audio, GameTuning tuning)
        {
            _cubeM = cube;
            _bhM = holes;
            _audio = audio;
            _tuning = tuning;
        }

        public IEnumerator Drop(
            BoardModel board,
            int currentPlayer,
            GameMode mode,
            Vector3Int startXZ,
            ITokenFactory factory,
            Material aiTwoMaterial,
            Action<TokenType, int, int, int> onPlaced)
        {
            int x = startXZ.x, z = startXZ.z;

            if (x < 0 || x >= board.sizeX || z < 0 || z >= board.sizeZ)
            {
                Debug.LogWarning($"TokenDropper.Drop: invalid start indices x:{x} z:{z} (board {board.sizeX}x{board.sizeZ}). Aborting drop.");
                yield break;
            }

            int y = FindDropY(board, x, z);
            if (y < 0) yield break;

            var placed = currentPlayer == 0 ? TokenType.PlayerOne : TokenType.PlayerTwo;

            Vector3 topWorld = _cubeM.GetCellWorldPosition(x, _cubeM.sizeY - 1, z);
            var prefab = factory.PickPrefab(mode, currentPlayer);
            var token = UnityEngine.Object.Instantiate(
                prefab,
                topWorld + Vector3.up * _tuning.dropHeight,
                prefab.transform.rotation,
                _cubeM.CubeContainer
            );
            factory.PostSpawnVisual(mode, currentPlayer, token, aiTwoMaterial);

            _audio?.PlayTokenLand();

            List<(int y, float worldY)> passList = BuildPassList(x, z, y);
            int nextPassIndex = 0;

            Vector3Int? holeSrc = FindHighestBlackHoleOnColumn(x, z);
            bool warped = false;

            while (true)
            {
                if (token == null) yield break;

                token.transform.position += Vector3.down * _tuning.dropSpeed * Time.deltaTime;

                while (nextPassIndex < passList.Count &&
                       token.transform.position.y <= passList[nextPassIndex].worldY)
                {
                    int passY = passList[nextPassIndex].y;
                    if (_cubeM.Cells[x, passY, z] != null)
                        yield return BlinkOnce(x, passY, z);
                    nextPassIndex++;
                }

                if (!warped && holeSrc.HasValue && _bhM.BlackHoles.TryGetValue(holeSrc.Value, out var data))
                {
                    float holeY = data.Instance.transform.position.y;
                    if (token.transform.position.y <= holeY)
                    {
                        warped = true;
                        _audio?.PlayWarp();

                        yield return ScaleWarpRoutine(token.transform, token.transform.position);

                        _bhM.RemoveBlackHole(holeSrc.Value);
                        x = (holeSrc.Value.x == 0 || holeSrc.Value.x == _cubeM.sizeX - 1)
                            ? _cubeM.sizeX - 1 - holeSrc.Value.x : x;
                        z = (holeSrc.Value.z == 0 || holeSrc.Value.z == _cubeM.sizeZ - 1)
                            ? _cubeM.sizeZ - 1 - holeSrc.Value.z : z;

                        y = FindDropY(board, x, z);
                        if (y < 0) yield break;

                        Vector3 topOpp = _cubeM.GetCellWorldPosition(x, _cubeM.sizeY - 1, z);
                        token.transform.position = topOpp + Vector3.up * _tuning.dropHeight;
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

            board.cells[x, y, z] = placed;
            var oldCell = _cubeM.Cells[x, y, z];
            if (oldCell != null)
            {
                var rend = oldCell.GetComponent<MeshRenderer>();
                if (rend != null) rend.enabled = false;
            }
            _cubeM.SetCellVisible(x, y, z, false);

            onPlaced?.Invoke(placed, x, y, z);
        }

        int FindDropY(BoardModel b, int x, int z)
        {
            for (int yy = 0; yy < b.sizeY; yy++)
                if (b.cells[x, yy, z] == TokenType.None) return yy;
            return -1;
        }

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
            Vector3Int? best = null;
            foreach (var kv in _bhM.BlackHoles)
            {
                var s = kv.Key;
                if (s.x == x && s.z == z && (!best.HasValue || s.y > best.Value.y))
                    best = s;
            }
            return best;
        }
        IEnumerator BlinkOnce(int x, int y, int z)
        {
            float layerTime = _cubeM.CellSpacing.y / _tuning.dropSpeed;
            float hold = Mathf.Clamp(layerTime * 0.25f, 0.02f, 0.08f);

            _cubeM.SetCellVisible(x, y, z, false);
            _audio?.PlayPassThrough();

            yield return new WaitForSeconds(hold);

            _cubeM.SetCellVisible(x, y, z, true);
        }

        IEnumerator ScaleWarpRoutine(Transform token, Vector3 targetWorldPos, float duration = 0.2f)
        {
            Vector3 startScale = token.localScale;
            float half = duration * 0.5f, t = 0f;

            while (t < half)
            {
                token.localScale = Vector3.Lerp(startScale, Vector3.zero, t / half);
                t += Time.deltaTime; yield return null;
            }
            token.localScale = Vector3.zero;

            token.position = targetWorldPos;

            t = 0f;
            while (t < half)
            {
                token.localScale = Vector3.Lerp(Vector3.zero, startScale, t / half);
                t += Time.deltaTime; yield return null;
            }
            token.localScale = startScale;
        }
    }
}
