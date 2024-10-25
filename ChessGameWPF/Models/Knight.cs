using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChessGameWPF.Models
{
    public class Knight : ChessPiece
    {
        public override string UnicodeSymbol => "\u265E";
        public override string Type => "Knight";
        [JsonConstructor]
        public Knight(PieceColor color) : base(color) { }

        public override List<(int, int)> Range((int, int) currentPosition, ChessPiece?[][] board)
        {
            var moves = new List<(int, int)>();
            int row = currentPosition.Item1, col = currentPosition.Item2;

            var possibleMoves = new (int, int)[]
            {
                (row + 2, col + 1), (row + 2, col - 1),
                (row - 2, col + 1), (row - 2, col - 1),
                (row + 1, col + 2), (row + 1, col - 2),
                (row - 1, col + 2), (row - 1, col - 2)
            };

            foreach (var (newRow, newCol) in possibleMoves)
            {
                if (IsWithinBounds(newRow, newCol, board) &&
                    (board[newRow][newCol] == null || board[newRow][newCol]?.Color != this.Color))
                {
                    moves.Add((newRow, newCol));
                }
            }

            return moves;
        }

        private bool IsWithinBounds(int row, int col, ChessPiece?[][] board)
        {
            return row >= 0 && row < board.Length && col >= 0 && col < board[0].Length;
        }
    }
}
