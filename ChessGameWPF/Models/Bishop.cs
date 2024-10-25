using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChessGameWPF.Models
{
    public class Bishop : ChessPiece
    {
        public override string UnicodeSymbol => "\u265D"; // White Bishop and Black Bishop
        public override string Type => "Bishop";
        [JsonConstructor]
        public Bishop(PieceColor color) : base(color) { }

        public override List<(int, int)> Range((int, int) currentPosition, ChessPiece?[][] board)
        {
            var moves = new List<(int, int)>();
            int row = currentPosition.Item1, col = currentPosition.Item2;

            AddDiagonalMoves(moves, row, col, board, 1, 1);   // Down-right
            AddDiagonalMoves(moves, row, col, board, 1, -1);  // Down-left
            AddDiagonalMoves(moves, row, col, board, -1, 1);  // Up-right
            AddDiagonalMoves(moves, row, col, board, -1, -1); // Up-left

            return moves;
        }

        private void AddDiagonalMoves(List<(int, int)> moves, int row, int col, ChessPiece?[][] board, int rowInc, int colInc)
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
