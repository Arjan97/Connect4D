using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuantumConnect
{
    /// <summary>
    /// Decoupled UI controller for turn/score/win/draw prompts and retry.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region Serialized
        [Header("Text/Images")]
        [SerializeField] TextMeshProUGUI _turnText;
        [SerializeField] TextMeshProUGUI _winText;
        [SerializeField] TextMeshProUGUI _scoreText;
        [SerializeField] Image _turnImage;
        [SerializeField] Button _retryButton;

        [Header("Icons")]
        [SerializeField] Sprite _playerOneIcon;
        [SerializeField] Sprite _playerTwoIcon;
        [SerializeField] Sprite _aiOneIcon;
        [SerializeField] Sprite _aiTwoIcon;

        [Header("Colors")]
        [SerializeField] Color _p1Color = Color.red;
        [SerializeField] Color _p2Color = Color.yellow;
        [SerializeField] Color _aiOneColor = Color.magenta;
        [SerializeField] Color _aiTwoColor = Color.blue;
        #endregion

        #region Turn/Score
        public void UpdateTurn(GameModes mode, int currentPlayer)
        {
            if (_turnText != null)
            {
                switch (mode)
                {
                    case GameModes.PvP:
                        _turnText.text = currentPlayer == 0 ? "Player One's Turn" : "Player Two's Turn";
                        _turnText.color = currentPlayer == 0 ? _p1Color : _p2Color;
                        break;
                    case GameModes.PvAI:
                        _turnText.text = currentPlayer == 0 ? "Player One's Turn" : "AI's Turn";
                        _turnText.color = currentPlayer == 0 ? _p1Color : _aiOneColor;
                        break;
                    case GameModes.AIvAI:
                        _turnText.text = currentPlayer == 0 ? "AI One's Turn" : "AI Two's Turn";
                        _turnText.color = currentPlayer == 0 ? _aiOneColor : _aiTwoColor;
                        break;
                }
            }

            if (_turnImage != null)
            {
                switch (mode)
                {
                    case GameModes.PvP:
                        _turnImage.sprite = currentPlayer == 0 ? _playerOneIcon : _playerTwoIcon;
                        break;
                    case GameModes.PvAI:
                        _turnImage.sprite = currentPlayer == 0 ? _playerOneIcon : _aiOneIcon;
                        break;
                    case GameModes.AIvAI:
                        _turnImage.sprite = currentPlayer == 0 ? _aiOneIcon : _aiTwoIcon;
                        break;
                }
            }
        }

        public void UpdateScore(GameModes mode, int p1Score, int p2Score)
        {
            if (_scoreText == null) return;
            switch (mode)
            {
                case GameModes.PvP: _scoreText.text = $"P1: {p1Score}   P2: {p2Score}"; break;
                case GameModes.PvAI: _scoreText.text = $"P1: {p1Score}   AI: {p2Score}"; break;
                case GameModes.AIvAI: _scoreText.text = $"AI1: {p1Score}  AI2: {p2Score}"; break;
            }
        }
        #endregion

        #region Win/Draw/Retry
        public void ShowWin(TokenTypes winner, GameModes mode)
        {
            if (_winText == null) return;

            bool vsAI = mode == GameModes.PvAI;
            if (winner == TokenTypes.PlayerOne)
            {
                _winText.text = "Player One Won!";
                _winText.color = _p1Color;
            }
            else
            {
                _winText.text = vsAI ? "AI Won!" : "Player Two Won!";
                _winText.color = vsAI ? _aiOneColor : _p2Color;
            }
            _winText.gameObject.SetActive(true);
        }

        public void ShowDraw()
        {
            if (_winText == null) return;
            _winText.text = "Draw!";
            _winText.color = Color.white;
            _winText.gameObject.SetActive(true);
        }

        public void HideWinAndRetry()
        {
            if (_winText != null) _winText.gameObject.SetActive(false);
            if (_retryButton != null) _retryButton.gameObject.SetActive(false);
        }

        public void ShowRetryIfNeeded(GameModes mode)
        {
            if (mode == GameModes.AIvAI) return;
            if (_retryButton != null) _retryButton.gameObject.SetActive(true);
        }
        #endregion
    }
}
