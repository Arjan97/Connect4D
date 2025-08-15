using UnityEngine;

namespace QuantumConnect
{
    /// <summary>Audio facade used by gameplay/UI.</summary>
    public interface IAudioService
    {
        void PlayPassThroughAt(Vector3 position);
        void PlayTokenLandAt(Vector3 position);
        void PlayWarpAt(Vector3 position);
        void PlayBlackHoleSpawnAt(Vector3 position);

        void PlayWin2D();

        void PlayMenuMusic();
        void PlayGameMusic();
        void StopMusic();

        void SetSfxVolume(float v);
        void SetMusicVolume(float v);
    }
}
