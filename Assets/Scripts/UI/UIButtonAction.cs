using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
            if (ReferenceEquals(_config, null)) { Debug.LogWarning("UIButtonAction missing _config."); return; }
            if (ReferenceEquals(_central, null)) { Debug.LogWarning("UIButtonAction missing CentralManager."); return; }

            switch (_config.action)
            {
                case ButtonActions.StartGame:
                    StartGame();
                    break;

                case ButtonActions.GoToMenu:
                    GoToMenu();
                    break;

                case ButtonActions.ResetMatch:
                    ResetMatch(_config.keepScores);
                    break;

                case ButtonActions.PlayMenuMusic:
                    _central.Audio?.PlayMenuMusic();
                    break;

                case ButtonActions.PlayGameMusic:
                    _central.Audio?.PlayGameMusic();
                    break;

                case ButtonActions.StopMusic:
                    _central.Audio?.StopMusic();
                    break;
                case ButtonActions.RotateLeft:
                    _central.Cube?.RotateLeft();
                    break;
                case ButtonActions.RotateRight:
                    _central.Cube?.RotateRight();
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
        void StartGame()
        {
            var session = _central.Session;
            if (session != null) session.SelectedMode = _config.mode;

            _central.Audio?.PlayGameMusic();

            if (!string.IsNullOrEmpty(_config.gameSceneName))
                SceneManager.LoadScene(_config.gameSceneName);
            else
                Debug.LogWarning("UIButtonAction: gameSceneName is empty.");
        }

        void GoToMenu()
        {
            _central.Audio?.PlayMenuMusic();

            if (!string.IsNullOrEmpty(_config.menuSceneName))
                SceneManager.LoadScene(_config.menuSceneName);
            else
                Debug.LogWarning("UIButtonAction: menuSceneName is empty.");
        }

        void ResetMatch(bool keepScores)
        {
            var game = _central.Game;
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
