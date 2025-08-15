using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuantumConnect
{
    /// <summary>
    /// Owns the high-level app state (Menu/Game), handles scene loads and music switching.
    /// </summary>
    public class AppStateManager : MonoBehaviour
    {
        [Header("Scenes")]
        [SerializeField] string _menuSceneName = "MainMenu";
        [SerializeField] string _gameSceneName = "QuantumConnect";

        [Header("Boot")]
        [SerializeField] AppStates _initialState = AppStates.Menu;

        AppStates _current;
        IAudioService _audio;

        void Start()
        {
            SetState(_initialState);
        }
        public void Initialize(IAudioService audio)
        {
            _audio = audio;
        }

        public void SetState(AppStates next)
        {
            if (_current == next) return;
            _current = next;

            switch (_current)
            {
                case AppStates.Menu:
                    if (SceneManager.GetActiveScene().name != _menuSceneName)
                        SceneManager.LoadScene(_menuSceneName);
                    _audio?.PlayMenuMusic();
                    break;

                case AppStates.Game:
                    if (SceneManager.GetActiveScene().name != _gameSceneName)
                        SceneManager.LoadScene(_gameSceneName);
                    _audio?.PlayGameMusic();
                    break;
            }
        }

        void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
        void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_audio == null) return;

            if (scene.name == _menuSceneName) _audio.PlayMenuMusic();
            else if (scene.name == _gameSceneName) _audio.PlayGameMusic();
        }
    }
}
