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
        public ButtonAction action = ButtonAction.StartGame;

        [Header("Game Mode (for StartGame)")]
        public GameMode mode = GameMode.PvAI;

        [Header("Scene Names")]
        public string gameSceneName = "QuantumConnect";
        public string menuSceneName = "MainMenu";

        [Header("Reset Options")]
        [Tooltip("When action = ResetMatch, should scores be preserved?")]
        public bool keepScores = true;
    }
}