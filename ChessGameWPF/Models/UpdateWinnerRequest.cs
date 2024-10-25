namespace ChessGameWPF.Models
{
    public class UpdateWinnerRequest
    {
        public int GameId { get; set; }
        public string Winner { get; set; } = "NULL";
        public string WinMethod { get; set; } = "NULL";
    }
}