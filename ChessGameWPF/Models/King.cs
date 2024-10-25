using ChessGameWPF.Models;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChessGameWPF.Models
{
    public class King : ChessPiece
    {
        public override string UnicodeSymbol => "\u265A";
        public override string Type => "King";

        public bool CanCastle { get; set; } = false; // Track if castling is available

        [JsonConstructor]
        public King(PieceColor color, bool canCastle) : base(color)
        {
            CanCastle = canCastle;
        }
        public King(PieceColor color) : base(color) { }

        public override List<(int, int)> Range((int, int) kingPosition, ChessPiece?[][] board)
        {
            var moves = new List<(int, int)>();

            // Standard King moves
            var possibleMoves = new (int, int)[]
            {
            (kingPosition.Item1 + 1, kingPosition.Item2), (kingPosition.Item1 - 1, kingPosition.Item2),
            (kingPosition.Item1, kingPosition.Item2 + 1), (kingPosition.Item1, kingPosition.Item2 - 1),
            (kingPosition.Item1 + 1, kingPosition.Item2 + 1), (kingPosition.Item1 - 1, kingPosition.Item2 - 1),
            (kingPosition.Item1 + 1, kingPosition.Item2 - 1), (kingPosition.Item1 - 1, kingPosition.Item2 + 1)
            };

            // Add valid moves to the range
            foreach (var (newRow, newCol) in possibleMoves)
            {
                if (IsWithinBounds(newRow, newCol, board) &&
                    (board[newRow][newCol] == null || board[newRow][newCol]?.Color != this.Color))
                {
                    moves.Add((newRow, newCol));
                }
            }

            // Add castling move to the range if castling is allowed
            if (CanCastle)
            {
                moves.Add((kingPosition.Item1, kingPosition.Item2 - 3)); // Add castling move (King side)
            }

            return moves;
        }

        private bool IsWithinBounds(int row, int col, ChessPiece?[][] board)
        {
            return row >= 0 && row < board.Length && col >= 0 && col < board[0].Length;
        }
    }

}
