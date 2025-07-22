using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX Clips")]
    public AudioClip passThroughSFX;
    public AudioClip tokenLandSFX;
    public AudioClip winSFX;
    public AudioClip warpSFX;
    public AudioClip blackHoleSpawnSFX;

    [Tooltip("How many AudioSources to pre-warm into the pool")]
    public int initialPoolSize = 10;

    List<AudioSource> _pool;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _pool = new List<AudioSource>(initialPoolSize);
        for (int i = 0; i < initialPoolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            _pool.Add(src);
        }
    }

    AudioSource GetSource()
    {
        foreach (var src in _pool)
            if (!src.isPlaying)
                return src;

        var extra = gameObject.AddComponent<AudioSource>();
        extra.playOnAwake = false;
        _pool.Add(extra);
        return extra;
    }

    IEnumerator ReturnWhenDone(AudioSource src)
    {
        yield return new WaitWhile(() => src.isPlaying);
        src.clip = null;
    }

    void PlayClip(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        var src = GetSource();
        src.clip = clip;
        src.volume = volume;
        src.Play();
        StartCoroutine(ReturnWhenDone(src));
    }

    public void PlayPassThrough() => PlayClip(passThroughSFX);
    public void PlayTokenLand() => PlayClip(tokenLandSFX);
    public void PlayWin() => PlayClip(winSFX);
    public void PlayWarp() => PlayClip(warpSFX);
    public void PlayBlackHoleSpawn() => PlayClip(blackHoleSpawnSFX);
}
