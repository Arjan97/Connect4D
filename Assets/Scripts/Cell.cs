using System;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>
    /// A single logical grid cell. Stores its coordinates and delegates click handling.
    /// </summary>
    public class Cell : MonoBehaviour
    {
        public event Action<int, int, int> Clicked;

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
        /// Called by InputManager when this cell is clicked/tapped.
        /// </summary>
        public void HandleClick()
        { Clicked?.Invoke(X, Y, Z); }
    }
}
