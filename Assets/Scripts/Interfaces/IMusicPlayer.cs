namespace QuantumConnect
{
    /// <summary>
    /// Contract for controlling background music in the game.
    /// </summary>
    public interface IMusicPlayer
    {
        void PlayMenuMusic();
        void PlayGameMusic();
        void StopMusic();

        void SetMusicVolume(float volume);
    }
}
