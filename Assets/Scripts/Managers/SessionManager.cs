using UnityEngine;

namespace QuantumConnect
{
    public class SessionManager : MonoBehaviour
    {
        [SerializeField] GameModes _selectedMode = GameModes.PvAI;

        public GameModes SelectedMode
        {
            get => _selectedMode;
            set => _selectedMode = value;
        }
    }
}
