using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// Centralized VFX.
    /// </summary>
    public class GameVfx : MonoBehaviour
    {
        CubeManager _cubeM;
        IAudioService _audio;
        GameTuning _tuning;

        struct RendBackup
        {
            public Color baseColor;
            public bool hadEmission;
            public Color emissionColor;
        }

        readonly Dictionary<MeshRenderer, RendBackup> _original = new();

        void Awake()
        {
            var central = CentralManager.Instance;
            _cubeM = central?.Cube ?? FindFirstObjectByType<CubeManager>();
            _audio = central?.Audio ?? FindFirstObjectByType<AudioManager>();
            _tuning = central?.Tuning;
        }

        public Coroutine PlayWinFx(List<Vector3Int> winningLine)
        {
            if (winningLine == null || winningLine.Count == 0) return null;
            return StartCoroutine(PlayWinRoutine(winningLine));
        }

        public void ClearHighlight()
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
                    else
                    {
                        mat.DisableKeyword("_EMISSION");
                    }
                }
            }
            _original.Clear();
        }

        IEnumerator PlayWinRoutine(List<Vector3Int> winningLine)
        {
            foreach (var c in winningLine)
            {
                if (!InBounds(c)) continue;
                var cell = _cubeM.Cells[c.x, c.y, c.z];
                if (!cell) continue;

                var rend = cell.GetComponent<MeshRenderer>();
                if (!rend) continue;

                ApplyHighlight(rend);
            }

            yield return null;
        }

        void ApplyHighlight(MeshRenderer rend)
        {
            if (!_original.ContainsKey(rend))
            {
                var mat = rend.material;
                bool hadEmission = mat.IsKeywordEnabled("_EMISSION");
                Color emCol = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;

                _original[rend] = new RendBackup
                {
                    baseColor = mat.color,
                    hadEmission = hadEmission,
                    emissionColor = emCol
                };
            }

            var matHL = rend.material;
            var winCol = _tuning != null ? _tuning.winHighlight : new Color(0f, 1f, 0f, 0.5f);
            if (winCol.a <= 0f) winCol.a = 0.5f;

            matHL.color = winCol;

            if (matHL.HasProperty("_EmissionColor"))
            {
                matHL.EnableKeyword("_EMISSION");
                matHL.SetColor("_EmissionColor", winCol);
            }
        }

        bool InBounds(Vector3Int c)
        {
            if (_cubeM == null || _cubeM.Cells == null) return false;
            return c.x >= 0 && c.x < _cubeM.sizeX
                && c.y >= 0 && c.y < _cubeM.sizeY
                && c.z >= 0 && c.z < _cubeM.sizeZ;
        }

        public IEnumerator BlinkCell(int x, int y, int z, float dropSpeed)
        {
            if (_cubeM == null) yield break;

            float spacingY = _cubeM.CellSpacing.y;
            float layerTime = spacingY <= 0f ? 0.05f : spacingY / Mathf.Max(dropSpeed, 0.0001f);
            float hold = Mathf.Clamp(layerTime * 0.25f, 0.02f, 0.08f);

            _cubeM.SetCellVisible(x, y, z, false);
            _audio?.PlayPassThrough();

            yield return new WaitForSeconds(hold);

            _cubeM.SetCellVisible(x, y, z, true);
        }

        public IEnumerator ScaleWarp(Transform token, Vector3 targetWorldPos, float duration = 0.2f)
        {
            if (!token) yield break;

            Vector3 startScale = token.localScale;
            float half = Mathf.Max(duration * 0.5f, 0.0001f);
            float t = 0f;

            while (t < half)
            {
                token.localScale = Vector3.Lerp(startScale, Vector3.zero, t / half);
                t += Time.deltaTime;
                yield return null;
            }
            token.localScale = Vector3.zero;

            token.position = targetWorldPos;

            t = 0f;
            while (t < half)
            {
                token.localScale = Vector3.Lerp(Vector3.zero, startScale, t / half);
                t += Time.deltaTime;
                yield return null;
            }
            token.localScale = startScale;
        }
    }
}
