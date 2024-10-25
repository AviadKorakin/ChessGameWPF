using System.Text.Json.Serialization;

namespace ChessGameWPF.Models
{
    public class MoveStepRequest
    {
        public int GameId { get; set; }

        [JsonConverter(typeof(TupleConverter))]
        public (int Row, int Col) From { get; set; }

        [JsonConverter(typeof(TupleConverter))]
        public (int Row, int Col) To { get; set; }

        public int StepOrder { get; set; }
        public string PieceType { get; set; } = string.Empty;
        public string? Promotion { get; set; }

        // New Boolean Properties
        public bool EnPassant { get; set; } = false;
        public bool Castling { get; set; } = false;
    }
}
