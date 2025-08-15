using System.Collections;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>Centralized audio: SFX via pool + background music crossfade.</summary>
    public class AudioService : MonoBehaviour, IAudioService
    {
        #region Serialized
        [Header("SFX")]
        [SerializeField] AudioClip passThroughSfx;
        [SerializeField] AudioClip tokenLandSfx;
        [SerializeField] AudioClip winSfx;
        [SerializeField] AudioClip warpSfx;
        [SerializeField] AudioClip blackHoleSpawnSfx;

        [Header("Music")]
        [SerializeField] AudioClip menuMusic;
        [SerializeField] AudioClip gameMusic;

        [Header("Music Sources (auto-created if empty)")]
        [SerializeField] AudioSource musicA;
        [SerializeField] AudioSource musicB;

        [Header("Volumes")]
        [Range(0f, 1f)][SerializeField] float sfxVolume = 0.8f;
        [Range(0f, 1f)][SerializeField] float musicVolume = 0.6f;

        [Header("Crossfade")]
        [SerializeField] float crossfadeSeconds = 0.5f;

        [Header("Pass-Through Gating")]
        [Tooltip("Max simultaneous pass-through voices; excess calls are dropped within the interval window.")]
        [SerializeField] int passMaxPolyphony = 6;
        [Tooltip("Minimum time between pass-through plays that count toward polyphony.")]
        [SerializeField] float passMinInterval = 0.02f;
        #endregion

        #region State
        AudioSource activeMusic;
        Coroutine crossfade;
        AudioPool audioPool;

        int passActive;
        float passLast;
        #endregion

        #region Unity
        void Start()
        {
            EnsureMusicBusses();
        }
        #endregion

        #region Init
        public void SetPool(AudioPool pool) { audioPool = pool; }
        #endregion

        #region IAudioService – SFX (public)
        public void PlayPassThroughAt(Vector3 position)
        {
            if (!passThroughSfx || audioPool == null) return;

            float now = Time.unscaledTime;
            if ((now - passLast) < passMinInterval && passActive >= passMaxPolyphony) return;
            passLast = now;
            passActive++;

            audioPool.PlayOneShot3D(passThroughSfx, position, sfxVolume);
            StartCoroutine(DecPassAfter(passThroughSfx.length));
        }

        public void PlayTokenLandAt(Vector3 position)
        {
            if (audioPool == null || !tokenLandSfx) return;
            audioPool.PlayOneShot3D(tokenLandSfx, position, sfxVolume);
        }

        public void PlayWarpAt(Vector3 position)
        {
            if (audioPool == null || !warpSfx) return;
            audioPool.PlayOneShot3D(warpSfx, position, sfxVolume);
        }

        public void PlayBlackHoleSpawnAt(Vector3 position)
        {
            if (audioPool == null || !blackHoleSpawnSfx) return;
            audioPool.PlayOneShot3D(blackHoleSpawnSfx, position, sfxVolume);
        }

        public void PlayWin2D()
        {
            if (audioPool == null || !winSfx) return;
            audioPool.PlayOneShot2D(winSfx, sfxVolume);
        }
        #endregion

        #region IAudioService – Music
        public void PlayMenuMusic() => PlayMusic(menuMusic);
        public void PlayGameMusic() => PlayMusic(gameMusic);

        public void StopMusic()
        {
            if (crossfade != null) StopCoroutine(crossfade);
            if (musicA) { musicA.Stop(); musicA.volume = 0f; }
            if (musicB) { musicB.Stop(); musicB.volume = 0f; }
        }

        public void SetSfxVolume(float v)
        {
            sfxVolume = Mathf.Clamp01(v);
        }

        public void SetMusicVolume(float v)
        {
            musicVolume = Mathf.Clamp01(v);
            if (crossfade == null && activeMusic) activeMusic.volume = musicVolume;
        }
        #endregion

        #region Internals – Music
        void EnsureMusicBusses()
        {
            if (!musicA) musicA = gameObject.AddComponent<AudioSource>();
            if (!musicB) musicB = gameObject.AddComponent<AudioSource>();

            musicA.loop = true; musicA.volume = 0f; musicA.ignoreListenerPause = true;
            musicB.loop = true; musicB.volume = 0f; musicB.ignoreListenerPause = true;

            activeMusic = musicA;
        }

        void PlayMusic(AudioClip clip)
        {
            if (!clip) return;

            var from = activeMusic;
            var to = (activeMusic == musicA ? musicB : musicA);

            to.clip = clip;
            to.loop = true;
            if (!to.isPlaying) to.Play();
            to.volume = 0f;

            if (crossfade != null) StopCoroutine(crossfade);
            crossfade = StartCoroutine(Crossfade(from, to, Mathf.Max(0.001f, crossfadeSeconds), musicVolume));
            activeMusic = to;
        }

        void EnsureMusic(AudioClip target)
        {
            if (!target) return;

            if (activeMusic && activeMusic.clip == target && activeMusic.isPlaying)
            {
                activeMusic.volume = musicVolume;
                return;
            }

            PlayMusic(target);
        }

        IEnumerator Crossfade(AudioSource from, AudioSource to, float duration, float targetVol)
        {
            float t = 0f;
            while (t < duration)
            {
                float p = t / duration;
                if (from) from.volume = Mathf.Lerp(targetVol, 0f, p);
                if (to) to.volume = Mathf.Lerp(0f, targetVol, p);
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (from) { from.volume = 0f; if (from.isPlaying) from.Stop(); }
            if (to) to.volume = targetVol;
            crossfade = null;
        }
        #endregion

        #region Internals – Pass-through counter
        IEnumerator DecPassAfter(float seconds)
        {
            float t = 0f;
            while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
            passActive = Mathf.Max(0, passActive - 1);
        }
        #endregion
    }
}
