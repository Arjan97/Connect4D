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
    }
}
