using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public enum PieceColor { White, Black }

namespace ChessGameWPF.Models
{
    public abstract class ChessPiece
    {
        [JsonIgnore]
        public PieceColor Color { get; }
        public abstract string Type { get; } //  JSON serialization/deserialization
        public abstract string UnicodeSymbol { get; }
        protected ChessPiece() { } // Parameterless constructor

        [JsonConstructor]
        protected ChessPiece(PieceColor color)
        {
            Color = color;
        }

        // Abstract method to define the possible range for each piece.
        // Updated to use jagged array (ChessPiece?[][]).
        public abstract List<(int, int)> Range((int, int) currentPosition, ChessPiece?[][] board);

        // Checks if a move to a target position is legal.
        public bool CanMove((int, int) currentPosition, (int, int) destination, ChessPiece?[][] board)
        {
            var validMoves = Range(currentPosition, board);
            return validMoves.Contains(destination);
        }
    }
}
