using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// Logical grid cell: stores coordinates and forwards click intent to the game flow.
    /// </summary>
    public class Cell : MonoBehaviour
    {
        #region Properties
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Z { get; private set; }
        #endregion

        #region Private State
        ICellInteractor _interactorM;
        GameManager _gameM; 
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            var central = CentralManager.Instance;
            _gameM = central != null ? central.Game : null;
            _interactorM = _gameM as ICellInteractor;
        }
        #endregion

        #region Public API
        public void Initialize(int x, int y, int z)
        {
            X = x; Y = y; Z = z;
        }

        public void HandleClick()
        {
            if (_interactorM != null) { _interactorM.ClickCell(X, Z); return; }
        }
        #endregion
    }
}
