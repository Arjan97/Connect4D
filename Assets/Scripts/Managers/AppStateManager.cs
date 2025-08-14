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

        public AppStates Current => _current;

        void Start()
        {
            SetState(_initialState);
        }

        public void SetState(AppStates next)
        {
            if (_current == next) return;
            _current = next;

            var central = CentralManager.Instance;
            var music = central?.Audio;

            switch (_current)
            {
                case AppStates.Menu:
                    if (SceneManager.GetActiveScene().name != _menuSceneName)
                        SceneManager.LoadScene(_menuSceneName);
                    music?.PlayMenuMusic();
                    break;

                case AppStates.Game:
                    if (SceneManager.GetActiveScene().name != _gameSceneName)
                        SceneManager.LoadScene(_gameSceneName);
                    music?.PlayGameMusic();
                    break;
            }
        }

        public void GoToMenu() => SetState(AppStates.Menu);
        public void StartGame() => SetState(AppStates.Game);

        void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
        void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var music = CentralManager.Instance?.Audio;
            if (music == null) return;

            if (scene.name == _menuSceneName) music.PlayMenuMusic();
            else if (scene.name == _gameSceneName) music.PlayGameMusic();
        }
    }
}
