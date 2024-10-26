namespace ChessGameWPF.Models
{
    public class CombinedMoveRequest
    {
        public int GameId { get; set; } // Game ID for tracking
        public int StepOrder { get; set; } // Order of the step
        public Chessboard Board { get; set; } = null!;
    }
}
