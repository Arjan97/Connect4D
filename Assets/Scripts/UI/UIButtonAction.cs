using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QuantumConnect
{
    /// <summary>
    /// Reusable UI button driver: pick a ButtonActionConfig in the Inspector.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonAction : MonoBehaviour
    {
        #region Serialized
        [Header("Action Config")]
        [SerializeField] ButtonActionConfig _config;

        [Header("Binding")]
        [SerializeField, Tooltip("Automatically wire Execute() to Button.onClick on Awake.")]
        bool _autoBindOnAwake = true;

        [SerializeField, Tooltip("Remove existing onClick listeners when auto-binding.")]
        bool _clearExistingOnBind = true;
        #endregion

        #region Cached
        Button _button;
        CentralManager _central;   
        #endregion

        #region Init (optional for tests/DI)
        public void Initialize(CentralManager central) => _central = central;
        #endregion

        #region Unity
        void Awake()
        {
            _button = GetComponent<Button>();
            _central = CentralManager.Instance;

            if (_autoBindOnAwake && _button != null)
            {
                if (_clearExistingOnBind) _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(Execute);
            }
        }
        #endregion

        #region API
        /// <summary>Invoke the configured action.</summary>
        public void Execute()
        {
            if (_config == null)
            {
                Debug.LogWarning("UIButtonAction: missing ButtonActionConfig.");
                return;
            }

            var central = CentralManager.Instance ?? _central;
            if (central == null)
            {
                Debug.LogWarning("UIButtonAction: CentralManager not available.");
                return;
            }

            switch (_config.action)
            {
                case ButtonActions.StartGame:
                    StartGame(central);
                    break;

                case ButtonActions.GoToMenu:
                    GoToMenu(central);
                    break;

                case ButtonActions.ResetMatch:
                    ResetMatch(central, _config.keepScores);
                    break;

                case ButtonActions.PlayMenuMusic:
                    central.Audio?.PlayMenuMusic();
                    break;

                case ButtonActions.PlayGameMusic:
                    central.Audio?.PlayGameMusic();
                    break;

                case ButtonActions.StopMusic:
                    central.Audio?.StopMusic();
                    break;

                case ButtonActions.RotateLeft:
                    central.GameVfx?.RotateLeft(central.Cube);
                    break;

                case ButtonActions.RotateRight:
                    central.GameVfx?.RotateRight(central.Cube);
                    break;

                case ButtonActions.QuitApp:
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
        }
        #endregion

        #region Helpers
        void StartGame(CentralManager central)
        {
            var session = central.Session;
            if (session != null) session.SelectedMode = _config.mode;

            central.Audio?.PlayGameMusic();

            if (!string.IsNullOrEmpty(_config.gameSceneName))
                SceneManager.LoadScene(_config.gameSceneName);
            else
                Debug.LogWarning("UIButtonAction: gameSceneName is empty.");
        }

        void GoToMenu(CentralManager central)
        {
            central.Audio?.PlayMenuMusic();

            if (!string.IsNullOrEmpty(_config.menuSceneName))
                SceneManager.LoadScene(_config.menuSceneName);
            else
                Debug.LogWarning("UIButtonAction: menuSceneName is empty.");
        }

        void ResetMatch(CentralManager central, bool keepScores)
        {
            var game = central.Game;
            if (game == null)
            {
                Debug.LogWarning("UIButtonAction.ResetMatch: GameManager not found in this scene.");
                return;
            }
            game.ResetGame(keepScores);
        }
        #endregion
    }
}
