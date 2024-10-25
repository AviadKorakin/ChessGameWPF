using System;

namespace ChessGameWPF.Models
{
    public class GameRecord
    {
        public int GameId { get; }
        public string Winner { get; }
        public string WinMethod { get; }
        public PieceColor UserColor { get; }
        public PieceColor ComputerColor { get; }

        public GameRecord(int gameId, string winner, string winMethod, bool userColor, bool computerColor)
        {
            GameId = gameId;
            Winner = winner;
            WinMethod = winMethod;

            // Convert bools to PieceColor
            UserColor = userColor ? PieceColor.White : PieceColor.Black;
            ComputerColor = computerColor ? PieceColor.White : PieceColor.Black;
        }

        // Optional: A display method for easy debugging or logging
        public override string ToString()
        {
            return $"Game {GameId}: Winner - {Winner}, WinMethod - {WinMethod}, UserColor - {UserColor}, ComputerColor - {ComputerColor}";
        }
    }
}
