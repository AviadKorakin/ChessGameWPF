namespace ChessGameWPF.Models
{
    public class MoveResponse
    {
        public bool Success { get; set; }
        public (int Row, int Col)? From { get; set; }
        public (int Row, int Col)? To { get; set; }
        public string? Promotion { get; set; }
        public string? Error { get; set; }

        public static MoveResponse ValidMove((int Row, int Col) from, (int Row, int Col) to) =>
            new MoveResponse
            {
                Success = true,
                From = from,
                To = to
            };

        public static MoveResponse PawnPromotion((int Row, int Col) from, (int Row, int Col) to, string promotion) =>
            new MoveResponse
            {
                Success = true,
                From = from,
                To = to,
                Promotion = promotion
            };

        public static MoveResponse ErrorResponse(string errorMessage) =>
            new MoveResponse
            {
                Success = false,
                Error = errorMessage
            };
    }
}
