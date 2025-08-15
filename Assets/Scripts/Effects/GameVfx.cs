using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// Centralized VFX.
    /// </summary>
    public class GameVfx : MonoBehaviour, IGameVfx
    {
        #region Deps 
        CubeManager _cube;
        IAudioService _audio;
        IBoardRules _rules;
        GameTuning _tuning;
        BoardModel _board;
        #endregion

        struct RendBackup
        {
            public Color baseColor;
            public bool hadEmission;
            public Color emissionColor;
        }

        readonly Dictionary<MeshRenderer, RendBackup> _original = new();

        #region Public API
        public void Initialize(GameServices s, BoardModel board)
        {
            _cube = s.Cube;
            _audio = s.Audio;
            _rules = s.Rules;
            _tuning = s.Tuning;
            _board = board;
        }
        public Coroutine PlayWinLine(List<Vector3Int> winningLine)
        {
            if (winningLine == null || winningLine.Count == 0) return null;
            return StartCoroutine(PlayWinRoutine(winningLine));
        }

        public void ClearWinHighlight()
        {
            foreach (var kv in _original)
            {
                var rend = kv.Key;
                if (!rend) continue;

                var mat = rend.material;
                mat.color = kv.Value.baseColor;

                if (mat.HasProperty("_EmissionColor"))
                {
                    if (kv.Value.hadEmission)
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", kv.Value.emissionColor);
                    }
                    else mat.DisableKeyword("_EMISSION");
                }
            }
            _original.Clear();
        }

        public IEnumerator BlinkCell(int x, int y, int z, float dropSpeed)
        {
            if (!_cube) yield break;
            var cell = _cube.Cells[x, y, z];
            if (cell == null) yield break;

            if (_tuning != null && !_tuning.fallBlinkEnabled) yield break;

            float spacingY = _cube.CellSpacing.y;
            float layerTime = spacingY <= 0f ? 0.05f : spacingY / Mathf.Max(dropSpeed, 0.0001f);

            float scale = _tuning != null ? _tuning.fallBlinkHoldScale : 0.25f;
            float minHold = _tuning != null ? _tuning.fallBlinkMinHold : 0.02f;
            float maxHold = _tuning != null ? _tuning.fallBlinkMaxHold : 0.08f;

            float hold = Mathf.Clamp(layerTime * Mathf.Max(0f, scale), minHold, maxHold);

            var rends = cell.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < rends.Length; i++) if (rends[i]) rends[i].enabled = false;

            yield return new WaitForSeconds(hold);

            for (int i = 0; i < rends.Length; i++) if (rends[i]) rends[i].enabled = true;
        }

        public IEnumerator ScaleWarp(Transform token, Vector3 targetWorldPos, float duration = 0.2f)
        {
            if (!token) yield break;

            Vector3 startScale = token.localScale;
            float half = Mathf.Max(duration * 0.5f, 0.0001f);
            float t = 0f;

            // Shrink
            while (t < half)
            {
                token.localScale = Vector3.Lerp(startScale, Vector3.zero, t / half);
                t += Time.deltaTime;
                yield return null;
            }
            token.localScale = Vector3.zero;

            // Teleport
            token.position = targetWorldPos;

            // Expand
            t = 0f;
            while (t < half)
            {
                token.localScale = Vector3.Lerp(Vector3.zero, startScale, t / half);
                t += Time.deltaTime;
                yield return null;
            }
            token.localScale = startScale;
        }

        public Coroutine RotateLeft(CubeManager cube) => StartCoroutine(RotateBy(cube, Vector3.up, 90f));

        public Coroutine RotateRight(CubeManager cube) => StartCoroutine(RotateBy(cube, Vector3.up, -90f));

        public Coroutine RotateToFace(CubeManager cube, Vector3Int targetFace, Camera cam = null)
        {
            return StartCoroutine(RotateToFaceRoutine(cube, targetFace, cam));
        }

        #endregion
        #region Private Helpers
        IEnumerator PlayWinRoutine(List<Vector3Int> winningLine)
        {
            if (_cube == null || _rules == null || _board == null) yield break;

            float blink = _tuning != null ? Mathf.Max(0.05f, _tuning.blinkInterval) : 0.5f;

            for (int i = 0; i < winningLine.Count; i++)
            {
                var c = winningLine[i];
                if (!_rules.InBounds(_board, c.x, c.y, c.z)) continue;

                _cube.SetCellVisible(c.x, c.y, c.z, true);

                var cell = _cube.Cells[c.x, c.y, c.z];
                var rend = cell ? cell.GetComponent<MeshRenderer>() : null;
                if (rend != null) ApplyHighlight(rend);

                _audio?.PlayPassThroughAt(cell.transform.position);

                yield return new WaitForSeconds(blink);
            }
        }

        void ApplyHighlight(MeshRenderer rend)
        {
            if (!_original.ContainsKey(rend))
            {
                var mat0 = rend.material;
                bool hadEmission = mat0.IsKeywordEnabled("_EMISSION");
                Color emCol = mat0.HasProperty("_EmissionColor") ? mat0.GetColor("_EmissionColor") : Color.black;

                _original[rend] = new RendBackup
                {
                    baseColor = mat0.color,
                    hadEmission = hadEmission,
                    emissionColor = emCol
                };
            }

            var mat = rend.material;
            var winCol = _tuning != null ? _tuning.winHighlight : Color.green;
            if (winCol.a <= 0f) winCol.a = 0.5f;

            mat.color = winCol;

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                var em = winCol * 0.25f;
                em.a = 1f;
                mat.SetColor("_EmissionColor", em);
            }
        }
        IEnumerator RotateBy(CubeManager cube, Vector3 axis, float angle)
        {
            if (cube == null || cube.CubeContainer == null) yield break;
            float duration = (_tuning != null && _tuning.rotationDuration > 0f) ? _tuning.rotationDuration : 0.2f;
            yield return AnimateContainerRotation(cube.CubeContainer, axis, angle, duration);
        }

        IEnumerator RotateToFaceRoutine(CubeManager cube, Vector3Int targetFace, Camera cam)
        {
            if (cube == null || cube.CubeContainer == null) yield break;

            int current = FaceUtils.Index(FaceUtils.ActiveFaceNormal(cube.CubeContainer, cam));
            int target = FaceUtils.Index(targetFace);
            if (current < 0 || target < 0 || current == target) yield break;

            int diff = (target - current + 4) % 4;
            float pause = (_tuning != null) ? _tuning.rotationPause : cube.RotationPause;
            float duration = (_tuning != null && _tuning.rotationDuration > 0f) ? _tuning.rotationDuration : 0.2f;

            if (diff == 1)
            {
                yield return AnimateContainerRotation(cube.CubeContainer, Vector3.up, -90f, duration);
                yield return new WaitForSeconds(pause);
            }
            else if (diff == 2)
            {
                yield return AnimateContainerRotation(cube.CubeContainer, Vector3.up, -90f, duration);
                yield return new WaitForSeconds(pause);
                yield return AnimateContainerRotation(cube.CubeContainer, Vector3.up, -90f, duration);
                yield return new WaitForSeconds(pause);
            }
            else if (diff == 3)
            {
                yield return AnimateContainerRotation(cube.CubeContainer, Vector3.up, 90f, duration);
                yield return new WaitForSeconds(pause);
            }
        }
        IEnumerator AnimateContainerRotation(Transform container, Vector3 axis, float angle, float duration)
        {
            if (!container) yield break;

            Vector3 pivot = container.position;
            float elapsed = 0f;
            duration = Mathf.Max(duration, 0.0001f);

            while (elapsed < duration)
            {
                float step = (angle / duration) * Time.deltaTime;
                container.RotateAround(pivot, axis, step);
                elapsed += Time.deltaTime;
                yield return null;
            }

            container.RotateAround(pivot, axis, angle - (angle / duration) * elapsed);
        }
        #endregion
    }
}