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

        void Awake()
        {
            RebindAudio();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            var central = CentralManager.Instance;
            if (central != null)
                central.SceneRefsUpdated += RebindAudio;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            var central = CentralManager.Instance;
            if (central != null)
                central.SceneRefsUpdated -= RebindAudio;
        }

        void Start()
        {
            if (_audio == null) RebindAudio();

            var active = SceneManager.GetActiveScene().name;
            if (active == _menuSceneName) _audio?.PlayMenuMusic();
            else if (active == _gameSceneName) _audio?.PlayGameMusic();

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
                    {
                        SceneManager.LoadScene(_menuSceneName); 
                    }
                    else
                    {
                        _audio?.PlayMenuMusic(); 
                    }
                    break;

                case AppStates.Game:
                    if (SceneManager.GetActiveScene().name != _gameSceneName)
                    {
                        SceneManager.LoadScene(_gameSceneName);
                    }
                    else
                    {
                        _audio?.PlayGameMusic();
                    }
                    break;
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_audio == null) RebindAudio();
            if (_audio == null) return;

            if (scene.name == _menuSceneName) _audio.PlayMenuMusic();
            else if (scene.name == _gameSceneName) _audio.PlayGameMusic();
        }

        void RebindAudio()
        {
            _audio = CentralManager.Instance?.Audio;
        }
    }
}
