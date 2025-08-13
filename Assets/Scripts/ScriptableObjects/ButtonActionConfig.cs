namespace QuantumConnect
{
    using UnityEngine;
    /// <summary>
    /// Reusable button action definition.
    /// </summary>
    [CreateAssetMenu(menuName = "QuantumConnect/UI/Button Action Config", fileName = "ButtonActionConfig")]
    public class ButtonActionConfig : ScriptableObject
    {
        [Header("Action")]
        public ButtonActions action = ButtonActions.StartGame;

        [Header("Game Mode (for StartGame)")]
        public GameModes mode = GameModes.PvAI;

        [Header("Scene Names")]
        public string gameSceneName = "QuantumConnect";
        public string menuSceneName = "MainMenu";

        [Header("Reset Options")]
        [Tooltip("When action = ResetMatch, should scores be preserved?")]
        public bool keepScores = true;
    }
}