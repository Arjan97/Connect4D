using NUnit.Framework;

namespace QuantumConnect.Tests
{
    public class BoardRulesTests
    {
        [Test]
        public void ApplyMove_FillsFromBottom_UndoRestores()
        {
            var rules = new BoardRules();
            var board = new BoardModel(3, 4, 3); // x,y,z

            int x = 1, z = 1;

            // First drop → y=0
            int y0 = rules.ApplyMove(board, x, z, TokenTypes.PlayerOne);
            Assert.AreEqual(0, y0);
            Assert.AreEqual(TokenTypes.PlayerOne, board[x, 0, z]);

            // Second drop → y=1
            int y1 = rules.ApplyMove(board, x, z, TokenTypes.PlayerTwo);
            Assert.AreEqual(1, y1);
            Assert.AreEqual(TokenTypes.PlayerTwo, board[x, 1, z]);

            // Undo second → topmost in column clears
            rules.UndoMove(board, x, z);
            Assert.AreEqual(TokenTypes.None, board[x, 1, z]);
            Assert.AreEqual(TokenTypes.PlayerOne, board[x, 0, z]);

            // Column still accepts another piece at y=1
            int dropY = rules.FindDropY(board, x, z);
            Assert.AreEqual(1, dropY);
        }

        [Test]
        public void HasAnyEmpty_TrueUntilBoardFilled()
        {
            var rules = new BoardRules();
            var board = new BoardModel(2, 2, 2);

            Assert.IsTrue(rules.HasAnyEmpty(board));

            // Fill all 8 cells (2x2x2 shell == all cells)
            for (int x = 0; x < 2; x++)
                for (int z = 0; z < 2; z++)
                    for (int i = 0; i < 2; i++)
                        rules.ApplyMove(board, x, z, TokenTypes.PlayerOne);

            Assert.IsFalse(rules.HasAnyEmpty(board));
        }
    }
}
