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
        CubeManager CubeM => CentralManager.Instance?.Cube ?? FindFirstObjectByType<CubeManager>();
        IAudioService AudioM => CentralManager.Instance?.Audio ?? FindFirstObjectByType<AudioManager>();
        IBoardRules BoardRules => CentralManager.Instance?.Rules;
        GameTuning Tuning => CentralManager.Instance?.Tuning;
        BoardModel Board => CentralManager.Instance?.Game?.Board;
        #endregion

        struct RendBackup
        {
            public Color baseColor;
            public bool hadEmission;
            public Color emissionColor;
        }

        readonly Dictionary<MeshRenderer, RendBackup> _original = new();

        #region Public API
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
            if (!CubeM) yield break;
            var cell = CubeM.Cells[x, y, z];
            if (cell == null) yield break;

            if (Tuning != null && !Tuning.fallBlinkEnabled) yield break;

            float spacingY = CubeM.CellSpacing.y;
            float layerTime = spacingY <= 0f ? 0.05f : spacingY / Mathf.Max(dropSpeed, 0.0001f);

            float scale = Tuning != null ? Tuning.fallBlinkHoldScale : 0.25f;
            float minHold = Tuning != null ? Tuning.fallBlinkMinHold : 0.02f;
            float maxHold = Tuning != null ? Tuning.fallBlinkMaxHold : 0.08f;

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
        #endregion
        #region Private Helpers
        IEnumerator PlayWinRoutine(List<Vector3Int> winningLine)
        {
            var rules = BoardRules;
            var board = Board;
            if (rules == null || board == null) yield break;

            float blink = Tuning != null ? Mathf.Max(0.05f, Tuning.blinkInterval) : 0.5f;

            for (int i = 0; i < winningLine.Count; i++)
            {
                var c = winningLine[i];
                if (!rules.InBounds(board, c.x, c.y, c.z)) continue;

                CubeM.SetCellVisible(c.x, c.y, c.z, true);

                var cell = CubeM.Cells[c.x, c.y, c.z];
                var rend = cell ? cell.GetComponent<MeshRenderer>() : null;
                if (rend != null) ApplyHighlight(rend);

                AudioM?.PlayPassThrough();

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
            var winCol = Tuning != null ? Tuning.winHighlight : Color.green;
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
        #endregion
    }
}