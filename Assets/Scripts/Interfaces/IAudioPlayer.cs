namespace QuantumConnect
{
    /// <summary>
    /// Contract for playing sound effects in the game.
    /// </summary>
    public interface IAudioPlayer
    {
        void PlayPassThrough();
        void PlayTokenLand();
        void PlayWin();
        void PlayWarp();
        void PlayBlackHoleSpawn();

        void SetSfxVolume(float volume);
    }
}
