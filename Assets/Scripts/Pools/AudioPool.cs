using System;
using System.Collections;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>Pool of AudioSources for overlapping SFX (2D and 3D).</summary>
    public class AudioPool : ComponentPool<AudioSource>
    {
        #region Overrides
        protected override void PrepareOnCreate(AudioSource inst)
        {
            inst.playOnAwake = false;
            inst.loop = false;
            inst.spatialBlend = 0f;
            inst.volume = 1f;
        }
        #endregion

        #region API
        public void PlayOneShot2D(AudioClip clip, float volume) { PlayOneShot2D(clip, volume, null); }

        public void PlayOneShot2D(AudioClip clip, float volume, Action onComplete)
        {
            if (!clip) return;
            var src = Get();
            if (!src) return;

            src.transform.localPosition = Vector3.zero;
            src.spatialBlend = 0f;
            src.volume = volume;
            src.clip = clip;
            src.Play();

            StartCoroutine(ReturnAfter(src, clip.length, onComplete));
        }

        public void PlayOneShot3D(AudioClip clip, Vector3 position, float volume) { PlayOneShot3D(clip, position, volume, null); }

        public void PlayOneShot3D(AudioClip clip, Vector3 position, float volume, Action onComplete)
        {
            if (!clip) return;
            var src = Get();
            if (!src) return;

            src.transform.position = position;
            src.spatialBlend = 1f;
            src.volume = volume;
            src.clip = clip;
            src.Play();

            StartCoroutine(ReturnAfter(src, clip.length, onComplete));
        }
        #endregion

        #region Internals
        IEnumerator ReturnAfter(AudioSource src, float seconds, Action onComplete)
        {
            float t = 0f;
            while (t < seconds && src) { t += Time.unscaledDeltaTime; yield return null; }
            if (src) Return(src);
            onComplete?.Invoke();
        }
        #endregion
    }
}
