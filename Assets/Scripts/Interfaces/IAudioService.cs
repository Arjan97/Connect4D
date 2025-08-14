public interface IAudioService
{
    void PlayPassThrough(); void PlayTokenLand(); void PlayWin(); void PlayWarp(); void PlayBlackHoleSpawn();
    void PlayMenuMusic(); void PlayGameMusic(); void StopMusic();
    void SetSfxVolume(float v); void SetMusicVolume(float v);
}
