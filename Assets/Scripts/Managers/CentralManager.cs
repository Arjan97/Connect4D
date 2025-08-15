using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuantumConnect
{
    /// <summary>
    /// Single point of access to game services/components.
    /// Wire references in the Inspector or via FindInScene() on Awake as fallback.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class CentralManager : MonoBehaviour
    {
        public static CentralManager Instance { get; private set; }

        #region Fields
        [Header("Core")]
        [SerializeField] GameManager _gameManager;
        [SerializeField] CubeManager _cubeManager;
        [SerializeField] AudioService _audioService;
        [SerializeField] AIManager _aiManager;
        [SerializeField] InputManager _inputManager;
        [SerializeField] BlackHoleManager _blackHoleManager;
        [SerializeField] UIManager _uiManager;
        [SerializeField] AppStateManager _appManager;
        [SerializeField] SessionManager _sessionManager;
        [SerializeField] BoardRules _boardRules;

        [Header("Pools")]
        [SerializeField] AudioPool _audioPool;
        [SerializeField] TokenPool _tokenPool;

        [Header("Scriptable Object")]
        [Tooltip("Game tuning parameters, create one if none existent")]
        [SerializeField] GameTuning _tuning;

        GameVfx _gameVfx;
        #endregion

        public event Action SceneRefsUpdated;

        #region Properties
        public GameManager Game => _gameManager;
        public CubeManager Cube => _cubeManager;
        public AIManager AI => _aiManager;
        public InputManager Input => _inputManager;
        public BlackHoleManager BlackHole => _blackHoleManager;
        public UIManager UI => _uiManager;
        public AppStateManager App => _appManager;
        public SessionManager Session => _sessionManager;
        public GameTuning Tuning => _tuning;
        public GameVfx GameVfx => _gameVfx;

        public AudioPool AudioPool => _audioPool;
        public TokenPool TokenPool => _tokenPool;

        public IBoardRules Rules => _boardRules ??= new BoardRules();
        public IAudioService Audio => _audioService;

        public GameServices BuildGameServices()
        {
            return new GameServices(
                _cubeManager,
                _blackHoleManager,
                _audioService,
                Rules,
                _tuning,
                _gameVfx
            );
        }
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_boardRules == null) _boardRules = new BoardRules();
            BindSceneLocals();
            WireUpPools();
        }

        void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        void OnSceneLoaded(Scene s, LoadSceneMode m)
        {
            BindSceneLocals();
            WireUpPools();
        }
        #endregion

        #region Private Helpers
        static void FindInSceneIfNull<T>(ref T field, string goName) where T : Component
        {
            if (field == null)
                field = FindFirstObjectByType<T>();
        }

        void BindSceneLocals()
        {
            FindInSceneIfNull(ref _gameManager, nameof(GameManager));
            FindInSceneIfNull(ref _cubeManager, nameof(CubeManager));
            FindInSceneIfNull(ref _audioService, nameof(AudioService));
            FindInSceneIfNull(ref _aiManager, nameof(AIManager));
            FindInSceneIfNull(ref _inputManager, nameof(InputManager));
            FindInSceneIfNull(ref _blackHoleManager, nameof(BlackHoleManager));
            FindInSceneIfNull(ref _uiManager, nameof(UIManager));
            FindInSceneIfNull(ref _sessionManager, nameof(SessionManager));
            FindInSceneIfNull(ref _appManager, nameof(AppStateManager));
            FindInSceneIfNull(ref _gameVfx, nameof(GameVfx));
            FindInSceneIfNull(ref _audioPool, nameof(AudioPool));
            FindInSceneIfNull(ref _tokenPool, nameof(TokenPool));

            SceneRefsUpdated?.Invoke();
        }

        void WireUpPools()
        {
            if (_audioService != null && _audioPool != null)
                _audioService.SetPool(_audioPool);
        }
        #endregion
    }
}
