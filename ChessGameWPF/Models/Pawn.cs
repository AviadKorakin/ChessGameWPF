using ChessGameWPF.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json.Serialization;

namespace ChessGameWPF.Models
{
    public class Pawn : ChessPiece
    {
        public override string UnicodeSymbol => "\u265F";
        public override string Type => "Pawn";

        public int MoveCounter { get; private set; } = 0; // Track the number of moves.
        public bool EnPassantLeft { get; set; } = false; // Can capture left en passant
        public bool EnPassantRight { get; set; } = false; // Can capture right en passant

        // Track movement direction for each side
        public int WhiteDirection { get; private set; }
        public int BlackDirection { get; private set; }


        [JsonConstructor]
        public Pawn(PieceColor color, int moveCounter, bool enPassantLeft, bool enPassantRight, int whiteDirection,int blackDirection) : base(color)
        {
            MoveCounter = moveCounter;
            EnPassantLeft = enPassantLeft;
            EnPassantRight = enPassantRight;
            WhiteDirection = whiteDirection;
            BlackDirection = blackDirection;
        }

        // Existing constructor
        public Pawn(PieceColor color, int whiteDirection, int blackDirection) : base(color) {
           WhiteDirection = whiteDirection;
            BlackDirection = blackDirection;
        }

        public override List<(int, int)> Range((int, int) currentPosition, ChessPiece?[][] board)
        {
            var moves = new List<(int, int)>();
            int row = currentPosition.Item1, col = currentPosition.Item2;

            // Determine direction based on color: White moves up (+1), Black moves down (-1)
            int direction = Color == PieceColor.White ? WhiteDirection : BlackDirection;

            // 1. Forward move: If the cell directly in front is empty
            if (IsWithinBounds(row + direction, col, board) && board[row + direction][col] == null)
            {
                moves.Add((row + direction, col));

                // 2. Allow two steps forward if this is the first move
                if (MoveCounter == 0 && IsWithinBounds(row + 2 * direction, col, board) && board[row + 2 * direction][col] == null)
                {
                    moves.Add((row + 2 * direction, col));
                }
            }

            // 3. Diagonal capture: Check if enemy pieces are diagonally adjacent
            bool leftCapture = AddCaptureMove(moves, row + direction, col - 1, board);
            bool rightCapture = AddCaptureMove(moves, row + direction, col + 1, board);

            // 4. Check for en passant moves
            if (!leftCapture && EnPassantLeft)
            {
                moves.Add((row + direction, col - 1)); // Add left en passant move
            }
            if (!rightCapture && EnPassantRight)
            {
                moves.Add((row + direction, col + 1)); // Add right en passant move
            }

            // 5. Horizontal movement: Only allowed if the target cell is empty
            if (col > 0 && board[row][col - 1] == null)  // Move left
            {
                moves.Add((row, col - 1));
            }
            if (col < board[0].Length - 1 && board[row][col + 1] == null)  // Move right
            {
                moves.Add((row, col + 1));
            }

            return moves;
        }

        private bool AddCaptureMove(List<(int, int)> moves, int row, int col, ChessPiece?[][] board)
        {
            if (IsWithinBounds(row, col, board) && IsEnemyPiece(row, col, board))
            {
                moves.Add((row, col));
                return true; // Enemy piece found
            }
            return false; // No enemy piece found
        }

        // Increment the move counter
        public void MarkAsMoved()
        {
            MoveCounter++;
        }

        // Reset en passant properties
        public void ResetEnPassant()
        {
            EnPassantLeft = false;
            EnPassantRight = false;
        }

        private bool IsWithinBounds(int row, int col, ChessPiece?[][] board)
        {
            return row >= 0 && row < board.Length && col >= 0 && col < board[0].Length;
        }

        private bool IsEnemyPiece(int row, int col, ChessPiece?[][] board)
        {
            return board[row][col] != null && board[row][col]?.Color != this.Color;
        }
    }
}
