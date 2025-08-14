using System.Collections;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// Thin orchestrator for match flow and the state machine. Delegates rules, drops, FX, UI, AI.
    /// </summary>
    public class GameManager : MonoBehaviour, ICellInteractor
    {
        #region Serialized (Inspector)
        [Header("Token Prefabs")]
        [SerializeField] GameObject _playerOnePrefab;
        [SerializeField] GameObject _playerTwoPrefab;
        [SerializeField] GameObject _aiPrefab;
        [SerializeField] Material _aiTwoMaterial;
        #endregion

        #region Dependencies (scene singletons)
        CubeManager _cubeM;
        BlackHoleManager _bhM;
        IAudioService _audioM;
        AIManager _aiM;
        UIManager _uiM;
        SessionManager _sessionM;
        WinFx _winFx;
        #endregion

        #region Services (pure logic)
        BoardModel _board;
        IBoardRules _rules;
        ITokenFactory _tokenFactory;
        ITokenDropper _dropper;
        TurnService _turns;
        ScoreService _scores;
        #endregion

        #region State machine
        IGameState _state;
        Coroutine _stateTick;
        bool _gameOver;
        public bool IsResolving { get; private set; }
        #endregion

        #region Unity
        void Awake()
        {
            var central = CentralManager.Instance;
            _sessionM = central?.Session;
            _audioM = central?.Audio;
        }

        IEnumerator Start()
        {
            yield return null;

            BindDependencies();
            if (!EnsureDependencies()) yield break;

            InitServices();

            _uiM.HideWinAndRetry();
            SetState(new SetupState());
        }

        void OnDisable()
        {
            if (_stateTick != null) StopCoroutine(_stateTick);
        }
        #endregion

        #region Public API
        public void ClickCell(int x, int z) => _state?.OnCellClick(this, x, z);

        public void SetState(IGameState next)
        {
            _state?.Exit(this);
            _state = next;
            _state?.Enter(this);

            if (_stateTick != null) StopCoroutine(_stateTick);
            if (_state != null) _stateTick = StartCoroutine(_state.Tick(this));
        }

        public void BeginNewMatch()
        {
            _gameOver = false;
            _turns.Reset();
            _board.Clear();

            _cubeM.ResetGrid();
            _bhM?.InitializeBlackHoles(_board);

            _uiM.UpdateTurn(Mode, _turns.Current);
            _uiM.UpdateScore(Mode, _scores.P1, _scores.P2);
        }

        public void ResetGame(bool keepScores)
        {
            StopAllCoroutines();

            _scores.Reset(keepScores);
            _uiM.UpdateScore(Mode, _scores.P1, _scores.P2);
            _uiM.HideWinAndRetry();

            _winFx?.ClearHighlight();

            SetState(new SetupState());
        }

        public void UpdateTurnUI() => _uiM?.UpdateTurn(Mode, _turns.Current);

        public void RequestAIMove()
        {
            if (_turns.IsAiTurn(Mode))
                _aiM?.MakeMove();
        }

        public IEnumerator DropToken(int x, int z)
        {
            IsResolving = true;

            yield return _dropper.Drop(
                _board,
                _turns.Current,
                Mode,
                new Vector3Int(x, 0, z),
                _tokenFactory,
                _aiTwoMaterial,
                OnTokenPlaced
            );

            IsResolving = false;
        }

        public bool IsOver => _gameOver;
        public int CurrentPlayer => _turns.Current;
        public BoardModel Board => _board;
        public TurnService Turns => _turns;

        /// <summary>Live game mode (from SessionManager). Defaults to PvAI if session missing.</summary>
        public GameModes Mode => _sessionM != null ? _sessionM.SelectedMode : GameModes.PvAI;
        #endregion

        #region Private – wiring/boot
        void BindDependencies()
        {
            var central = CentralManager.Instance;

            _cubeM = central?.Cube ?? FindFirstObjectByType<CubeManager>();
            _bhM = central?.BlackHole ?? FindFirstObjectByType<BlackHoleManager>();
            _aiM = central?.AI ?? FindFirstObjectByType<AIManager>();
            _uiM = central?.UI ?? FindFirstObjectByType<UIManager>();
            _winFx = central?.WinFx ?? FindFirstObjectByType<WinFx>();
        }

        bool EnsureDependencies()
        {
            if (_cubeM == null) { Debug.LogError("GameManager: CubeManager not found in scene."); return false; }
            if (_uiM == null) { Debug.LogError("GameManager: UIManager not found in scene."); return false; }
            if (_bhM == null) Debug.LogWarning("GameManager: BlackHoleManager not found (black holes disabled).");
            if (_aiM == null) Debug.LogWarning("GameManager: AIManager not found (AI disabled).");
            if (_winFx == null) Debug.LogWarning("GameManager: WinFx not found (using basic fallback).");
            return true;
        }

        void InitServices()
        {
            var central = CentralManager.Instance;

            _board = new BoardModel(_cubeM.sizeX, _cubeM.sizeY, _cubeM.sizeZ);
            _rules = new BoardRules();
            _tokenFactory = new TokenFactory(_playerOnePrefab, _playerTwoPrefab, _aiPrefab);
            _dropper = new TokenDropper(_cubeM, _bhM, _audioM, central?.Tuning);
            _turns = new TurnService();
            _scores = new ScoreService();
        }
        #endregion

        #region Private – event handlers
        void OnTokenPlaced(TokenTypes placed, int x, int y, int z)
        {
            // Win?
            if (_rules.CheckAnyWin(_board, placed, out var winLine))
            {
                _scores.Add(placed);
                _uiM.UpdateScore(Mode, _scores.P1, _scores.P2);

                _winFx?.PlayWinFx(winLine);
                _uiM.ShowWin(placed, Mode);

                _gameOver = true;
                return;
            }

            // Draw?
            if (!_rules.HasAnyEmpty(_board))
            {
                _uiM.ShowDraw();
                _gameOver = true;
                return;
            }

            // Next turn
            _turns.Next();
            _uiM.UpdateTurn(Mode, _turns.Current);

            _bhM?.TrySpawnRandomBlackHole();
        }
        #endregion
    }
}
