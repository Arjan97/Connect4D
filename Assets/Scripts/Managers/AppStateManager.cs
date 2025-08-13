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
        [SerializeField] AppState _initialState = AppState.Menu;

        AppState _current;

        public AppState Current => _current;

        void Start()
        {
            SetState(_initialState);
        }

        public void SetState(AppState next)
        {
            if (_current == next) return;
            _current = next;

            var central = CentralManager.Instance;
            var music = central?.Music;

            switch (_current)
            {
                case AppState.Menu:
                    if (SceneManager.GetActiveScene().name != _menuSceneName)
                        SceneManager.LoadScene(_menuSceneName);
                    music?.PlayMenuMusic();
                    break;

                case AppState.Game:
                    if (SceneManager.GetActiveScene().name != _gameSceneName)
                        SceneManager.LoadScene(_gameSceneName);
                    music?.PlayGameMusic();
                    break;
            }
        }

        public void GoToMenu() => SetState(AppState.Menu);
        public void StartGame() => SetState(AppState.Game);

        void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
        void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var music = CentralManager.Instance?.Music;
            if (music == null) return;

            if (scene.name == _menuSceneName) music.PlayMenuMusic();
            else if (scene.name == _gameSceneName) music.PlayGameMusic();
        }
    }
}
