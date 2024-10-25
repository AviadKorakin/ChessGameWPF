using System;

namespace ChessGameWPF.Models
{
    public class MoveRecord
    {
        public int StepOrder { get; }
        public int FromRow { get; }
        public int FromCol { get; }
        public int ToRow { get; }
        public int ToCol { get; }
        public string? Promotion { get; }
        public bool EnPassant { get; }
        public bool Castling { get; }

        public MoveRecord(int stepOrder, int fromRow, int fromCol, int toRow, int toCol, string? promotion, bool enPassant, bool castling)
        {
            StepOrder = stepOrder;
            FromRow = fromRow;
            FromCol = fromCol;
            ToRow = toRow;
            ToCol = toCol;
            Promotion = promotion;
            EnPassant = enPassant;
            Castling = castling;
        }
    }
}
