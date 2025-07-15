using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// Represents a single cell in the grid and handles click interactions.
    /// </summary>
    public class Cell : MonoBehaviour
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Z { get; private set; }

        /// <summary>
        /// Sets this cell’s logical coordinates in the grid.
        /// </summary>
        public void Initialize(int ix, int iy, int iz)
        {
            X = ix;
            Y = iy;
            Z = iz;
        }

        /// <summary>
        /// Called by InputManager when this cell is clicked.
        /// </summary>
        public void OnCellClicked()
        {
            GameManager.Instance.HandleCellClick(X, Z);
        }

        /// <summary>
        /// Removes any spawned token children so we can rebuild visuals from scratch.
        /// </summary>
        public void Clear()
        {
            // destroy any token cubes or prefabs parented here
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}
