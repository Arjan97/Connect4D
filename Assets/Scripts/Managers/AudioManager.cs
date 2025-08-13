using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    /// Centralized audio: pooled SFX + background music with crossfade (menu/game).
    public class AudioManager : MonoBehaviour, IAudioPlayer, IMusicPlayer
    {
        [Header("SFX Clips")]
        [SerializeField] AudioClip _passThroughSFX;
        [SerializeField] AudioClip _tokenLandSFX;
        [SerializeField] AudioClip _winSFX;
        [SerializeField] AudioClip _warpSFX;
        [SerializeField] AudioClip _blackHoleSpawnSFX;

        [Header("SFX Settings")]
        [SerializeField] int _initialPoolSize = 10;
        [SerializeField, Range(0f, 1f)] float _sfxVolume = 1f;

        [Header("Music Clips")]
        [SerializeField] AudioClip _menuMusic;
        [SerializeField] AudioClip _gameMusic;

        [Header("Music Settings")]
        [SerializeField, Range(0f, 1f)] float _musicVolume = 0.5f;
        [SerializeField] float _crossfadeDuration = 1.5f;

        List<AudioSource> _pool;
        AudioSource _musicA;
        AudioSource _musicB;
        bool _usingA = true;
        Coroutine _musicFadeRoutine;

        void Awake()
        {
            _pool = new List<AudioSource>(_initialPoolSize);
            for (int i = 0; i < _initialPoolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.volume = _sfxVolume;
                _pool.Add(src);
            }

            _musicA = gameObject.AddComponent<AudioSource>();
            _musicA.playOnAwake = false; _musicA.loop = true; _musicA.volume = 0f;

            _musicB = gameObject.AddComponent<AudioSource>();
            _musicB.playOnAwake = false; _musicB.loop = true; _musicB.volume = 0f;
        }

        void OnDisable()
        {
            if (_musicFadeRoutine != null) { StopCoroutine(_musicFadeRoutine); _musicFadeRoutine = null; }
        }

        void OnDestroy()
        {
            if (_musicFadeRoutine != null) { StopCoroutine(_musicFadeRoutine); _musicFadeRoutine = null; }
        }

        AudioSource GetSource()
        {
            for (int i = 0; i < _pool.Count; i++)
                if (!_pool[i].isPlaying) return _pool[i];

            var extra = gameObject.AddComponent<AudioSource>();
            extra.playOnAwake = false; extra.loop = false; extra.volume = _sfxVolume;
            _pool.Add(extra);
            return extra;
        }

        IEnumerator ReturnWhenDone(AudioSource src)
        {
            yield return new WaitWhile(() => src != null && src.isPlaying);
            if (src != null) src.clip = null;
        }

        void PlayClip(AudioClip clip)
        {
            if (clip == null) return;
            var src = GetSource();
            if (src == null) return;
            src.volume = _sfxVolume;
            src.clip = clip;
            src.Play();
            StartCoroutine(ReturnWhenDone(src));
        }

        public void PlayPassThrough() => PlayClip(_passThroughSFX);
        public void PlayTokenLand() => PlayClip(_tokenLandSFX);
        public void PlayWin() => PlayClip(_winSFX);
        public void PlayWarp() => PlayClip(_warpSFX);
        public void PlayBlackHoleSpawn() => PlayClip(_blackHoleSpawnSFX);
        public void SetSfxVolume(float volume) => _sfxVolume = Mathf.Clamp01(volume);

        public void PlayMenuMusic() => CrossfadeTo(_menuMusic);
        public void PlayGameMusic() => CrossfadeTo(_gameMusic);

        public void StopMusic()
        {
            if (_musicFadeRoutine != null) { StopCoroutine(_musicFadeRoutine); _musicFadeRoutine = null; }
            var active = GetActiveMusic();
            if (active != null) active.Stop();
            var idle = GetIdleMusic();
            if (idle != null) idle.Stop();
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            var active = GetActiveMusic();
            if (active != null && active.isPlaying) active.volume = _musicVolume;
        }

        AudioSource GetActiveMusic() => _usingA ? _musicA : _musicB;
        AudioSource GetIdleMusic() => _usingA ? _musicB : _musicA;

        void CrossfadeTo(AudioClip newClip)
        {
            if (newClip == null) return;

            var from = GetActiveMusic();
            var to = GetIdleMusic();
            if (to == null) return;

            to.clip = newClip;
            to.volume = 0f;
            to.loop = true;
            to.Play();

            if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
            _musicFadeRoutine = StartCoroutine(CrossfadeRoutine(from, to, _crossfadeDuration, _musicVolume));

            _usingA = !_usingA;
        }

        IEnumerator CrossfadeRoutine(AudioSource from, AudioSource to, float duration, float targetVol)
        {
            if (to == null) yield break; 
            if (duration <= 0f)
            {
                if (from != null && from.isPlaying) from.Stop();
                to.volume = targetVol;
                yield break;
            }

            float t = 0f;
            float startFrom = (from != null) ? from.volume : 0f;

            while (t < duration)
            {
                if (this == null || gameObject == null || to == null) yield break;

                float p = t / duration;
                if (from != null) from.volume = Mathf.Lerp(startFrom, 0f, p);
                to.volume = Mathf.Lerp(0f, targetVol, p);
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (from != null)
            {
                from.volume = 0f;
                if (from.isPlaying) from.Stop();
            }
            to.volume = targetVol;
        }
    }
}
