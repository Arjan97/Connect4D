namespace QuantumConnect
{
    /// <summary>
    /// Abstraction for performing a logical click/drop on the game board.
    /// </summary>
    public interface ICellInteractor
    {
        void ClickCell(int x, int z);
    }
}
