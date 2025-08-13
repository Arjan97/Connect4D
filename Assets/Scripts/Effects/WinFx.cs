using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>Plays win highlight on the cube.</summary>
    public class WinFx : MonoBehaviour, IWinFx
    {
        CubeManager _cubeM;
        IAudioPlayer _audio;
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
            _audio = central?.Audio;
            _tuning = central?.Tuning;
        }

        public Coroutine PlayWinFx(List<Vector3Int> winningLine)
        {
            if (winningLine == null || winningLine.Count == 0) return null;
            return StartCoroutine(PlayRoutine(winningLine));
        }

        void ApplyHighlight(MeshRenderer rend)
        {
            if (rend == null) return;

            if (!_original.ContainsKey(rend))
            {
                var mat = rend.material;
                var hadEmission = mat.IsKeywordEnabled("_EMISSION");
                var emCol = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;

                _original[rend] = new RendBackup
                {
                    baseColor = mat.color,
                    hadEmission = hadEmission,
                    emissionColor = emCol
                };
            }

            var winCol = _tuning != null ? _tuning.winHighlight : Color.green;
            if (winCol.a <= 0f) winCol.a = 0.5f; 
            rend.material.color = winCol;

            var m = rend.material;
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                var target = winCol * 0.25f; 
                target.a = 1f;
                m.SetColor("_EmissionColor", target);
            }
        }

        public void ClearHighlight()
        {
            foreach (var kv in _original)
            {
                var rend = kv.Key;
                if (rend == null) continue;

                rend.material.color = kv.Value.baseColor;

                var mat = rend.material;
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

        IEnumerator EnsureReady()
        {
            while (_cubeM == null)
            {
                var central = CentralManager.Instance;
                _cubeM = central?.Cube ?? FindFirstObjectByType<CubeManager>();
                if (_cubeM != null) break;
                yield return null;
            }

            while (_cubeM.CubeContainer == null || _cubeM.Cells == null)
                yield return null;
        }

        IEnumerator PlayRoutine(List<Vector3Int> line)
        {
            yield return EnsureReady();
            yield return TrySnapTowardsLine(line);

            float blink = _tuning != null ? Mathf.Max(0.05f, _tuning.blinkInterval) : 0.5f;

            for (int i = 0; i < line.Count; i++)
            {
                var c = line[i];
                if (!InBounds(c)) continue;

                _cubeM.SetCellVisible(c.x, c.y, c.z, true);

                var cell = _cubeM.Cells[c.x, c.y, c.z];
                var rend = cell?.GetComponent<MeshRenderer>();
                if (rend != null) ApplyHighlight(rend);

                _audio?.PlayPassThrough();
                yield return new WaitForSeconds(blink);
            }

            for (int pass = 0; pass < 3; pass++)
            {
                for (int i = 0; i < line.Count; i++)
                {
                    var c = line[i];
                    if (!InBounds(c)) continue;
                    _cubeM.SetCellVisible(c.x, c.y, c.z, false);
                }
                yield return new WaitForSeconds(blink);

                for (int i = 0; i < line.Count; i++)
                {
                    var c = line[i];
                    if (!InBounds(c)) continue;
                    _cubeM.SetCellVisible(c.x, c.y, c.z, true);

                    var cell = _cubeM.Cells[c.x, c.y, c.z];
                    if (cell != null)
                    {
                        var rend = cell.GetComponent<MeshRenderer>();
                        if (rend != null) ApplyHighlight(rend);
                    }
                }
                yield return new WaitForSeconds(blink);
            }

            for (int i = 0; i < line.Count; i++)
            {
                var c = line[i];
                if (!InBounds(c)) continue;

                _cubeM.SetCellVisible(c.x, c.y, c.z, true);

                var cell = _cubeM.Cells[c.x, c.y, c.z];
                if (cell != null)
                {
                    var rend = cell.GetComponent<MeshRenderer>();
                    if (rend != null)
                    {
                        ApplyHighlight(rend);
                        rend.enabled = true;
                    }
                }
            }
        }

        IEnumerator TrySnapTowardsLine(List<Vector3Int> line)
        {
            if (_cubeM == null || _cubeM.CubeContainer == null) yield break;

            Vector3 center = Vector3.zero;
            int count = 0;
            for (int i = 0; i < line.Count; i++)
            {
                var c = line[i];
                if (!InBounds(c)) continue;
                center += _cubeM.GetCellWorldPosition(c.x, c.y, c.z);
                count++;
            }
            if (count == 0) yield break;

            center /= count;

            Vector3 toCam = Camera.main != null
                ? (Camera.main.transform.position - _cubeM.CubeContainer.position)
                : Vector3.forward;
            toCam.y = 0f;
            if (toCam.sqrMagnitude < 0.0001f) yield break;

            yield return null;
        }

        bool InBounds(Vector3Int c)
        {
            if (_cubeM == null || _cubeM.Cells == null) return false;
            return c.x >= 0 && c.x < _cubeM.sizeX
                && c.y >= 0 && c.y < _cubeM.sizeY
                && c.z >= 0 && c.z < _cubeM.sizeZ;
        }
    }
}
