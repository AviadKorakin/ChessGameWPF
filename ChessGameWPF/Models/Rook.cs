using ChessGameWPF.Models;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChessGameWPF.Models
{
    public class Rook : ChessPiece
    {
        public override string UnicodeSymbol => "\u265C";
        public override string Type => "Rook";

        public bool CanCastle { get; set; } = false; // Track if castling is available


        [JsonConstructor]
        public Rook(PieceColor color, bool canCastle) : base(color)
        {
            CanCastle = canCastle;
        }
        public Rook(PieceColor color) : base(color) { }

        public override List<(int, int)> Range((int, int) rookPosition, ChessPiece?[][] board)
        {
            var moves = new List<(int, int)>();

            // Add standard rook moves (horizontal and vertical lines)
            AddLineMoves(moves, rookPosition.Item1, rookPosition.Item2, board, 1, 0);  // Down
            AddLineMoves(moves, rookPosition.Item1, rookPosition.Item2, board, -1, 0); // Up
            AddLineMoves(moves, rookPosition.Item1, rookPosition.Item2, board, 0, 1);  // Right
            AddLineMoves(moves, rookPosition.Item1, rookPosition.Item2, board, 0, -1); // Left

            // Add castling move if castling is allowed
            if (CanCastle)
            {
                moves.Add((rookPosition.Item1, rookPosition.Item2 + 3)); // Add castling move (King side)
            }

            return moves;
        }

        private void AddLineMoves(List<(int, int)> moves, int row, int col, ChessPiece?[][] board, int rowInc, int colInc)
        {
            int newRow = row + rowInc;
            int newCol = col + colInc;

            while (IsWithinBounds(newRow, newCol, board))
            {
                if (board[newRow][newCol] == null)
                {
                    moves.Add((newRow, newCol));
                }
                else if (board[newRow][newCol]?.Color != this.Color)
                {
                    moves.Add((newRow, newCol));
                    break;
                }
                else
                {
                    break;
                }

                newRow += rowInc;
                newCol += colInc;
            }
        }

        private bool IsWithinBounds(int row, int col, ChessPiece?[][] board)
        {
            return row >= 0 && row < board.Length && col >= 0 && col < board[0].Length;
        }
    }

}
