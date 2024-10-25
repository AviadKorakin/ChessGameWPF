using System;

namespace ChessGameWPF.Models
{
    public class GameSummary
    {
        public int GameId { get; }
        public DateTime GameDate { get; }
        public PieceColor UserColor { get; }
        public PieceColor ComputerColor { get; }

        // Custom display text for ComboBox
        public string DisplayText =>
            $"Game {GameId} - {GameDate:g}";

        public GameSummary(int gameId, DateTime gameDate, bool userColor, bool computerColor)
        {
            GameId = gameId;
            GameDate = gameDate;

            // Convert bools to PieceColor
            UserColor = userColor ? PieceColor.White : PieceColor.Black;
            ComputerColor = computerColor ? PieceColor.White : PieceColor.Black;
        }
    }
}
