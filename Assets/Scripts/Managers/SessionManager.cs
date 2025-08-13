using UnityEngine;

namespace QuantumConnect
{
    public class SessionManager : MonoBehaviour
    {
        [SerializeField] GameMode _selectedMode = GameMode.PvAI;

        public GameMode SelectedMode
        {
            get => _selectedMode;
            set => _selectedMode = value;
        }
    }
}
