using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Numerics;
using System.Text.Json.Serialization;
using ChessGameWPF.Exceptions;
using ChessGameWPF.Models;
using Newtonsoft.Json.Linq;


namespace ChessGameWPF.Models
{
    public class Chessboard
    {
        // Readonly chess size
        public int Rows { get; private set; } = 8;
        public int Cols { get; private set; } = 4;

        public int FiftyMoveCounter { get; set; } = 0;

        // Instance variables to store starting positions for kings, rooks, etc.
        private (int Row, int Col) WhiteKingStart;
        private (int Row, int Col) BlackKingStart;

        private (int Row, int Col) WhiteRookStart;
        private (int Row, int Col) BlackRookStart;


        // Track movement direction for each side
        public int WhiteDirection { get; private set; }
        public int BlackDirection { get; private set; }

        // Track whether castling is available
        public bool WhiteCanCastle { get; private set; } = true;
        public bool BlackCanCastle { get; private set; } = true;

        // Track if castling has been disabled
        private bool WhiteCastlingDisabled = false;
        private bool BlackCastlingDisabled = false;



        [JsonConverter(typeof(TupleConverter))]
        public (int Row, int Col)? LeftEnPassantLocation { get; set; } = null;  // Location of the left pawn eligible for en passant
        [JsonConverter(typeof(TupleConverter))]
        public (int Row, int Col)? RightEnPassantLocation { get; set; } = null;// Location of the right pawn eligible for en passant



        // Use a jagged array instead of 2D array
        [JsonConverter(typeof(ChessPieceArrayConverter))]
        public ChessPiece?[][] Board { get; set; }
        public PieceColor CurrentTurn { get; set; } = PieceColor.Black;

        // Winner attributes
        public string? WinMethod { get; set; } = null;
        public PieceColor? Winner { get; set; } = null;

        // Track the current positions of the kings
        [JsonConverter(typeof(TupleConverter))]
        public (int Row, int Col) WhiteKingPosition { get; set; }
        [JsonConverter(typeof(TupleConverter))]
        public (int Row, int Col) BlackKingPosition { get; set; }

        public Chessboard()
        {
            Board = new ChessPiece?[Rows][];
            for (int i = 0; i < Rows; i++)
            {
                Board[i] = new ChessPiece?[Cols];
            }
        }
        public Chessboard(bool isWhiteAtBottom = true)
        {
            // Initialize the jagged array
            Board = new ChessPiece?[Rows][];
            for (int i = 0; i < Rows; i++)
            {
                Board[i] = new ChessPiece?[Cols];
            }
            SetDirections(isWhiteAtBottom);
            // Initialize the board based on the player's perspective
            if (isWhiteAtBottom)
            {
                InitializeBoard(); // Standard board setup (White at the bottom)
            }
            else
            {
                InitializeBoardFlipped(); // Flipped board setup (White at the top)
            }
            CurrentTurn = PieceColor.White;
        }


        private void SetDirections(bool isWhiteAtBottom)
        {
            // If white is at the bottom, white moves up (-1) and black moves down (+1)
            if (isWhiteAtBottom)
            {
                WhiteDirection = -1;
                BlackDirection = +1;
            }
            else
            {
                // If white is at the top, white moves down (+1) and black moves up (-1)
                WhiteDirection = +1;
                BlackDirection = -1;
            }
        }
        public void InitializeKingTie()
        {

            // Place white pieces.
            Board[0][3] = new King(PieceColor.White);   // White King at d8

            // Place black pieces.
            Board[5][3] = new King(PieceColor.Black);   // Black King at d3

            // Set kings' positions.
            WhiteKingPosition = (0, 3);  // White King at d8
            BlackKingPosition = (5, 3);  // Black King at d4

            // Set the current turn to White.
            CurrentTurn = PieceColor.White;
        }
      

        public void InitializeExampleScenario1()
        {

            // Place white pieces.
            Board[0][0] = new Rook(PieceColor.White);   // White Rook at a8
            Board[0][1] = new Knight(PieceColor.White); // White Knight at b8
            Board[0][2] = new Bishop(PieceColor.White); // White Bishop at c8
            Board[0][3] = new King(PieceColor.White);   // White King at d8
            Board[1][3] = new Pawn(PieceColor.White, WhiteDirection, BlackDirection);   // White King at d8
            Board[4][0] = new Pawn(PieceColor.White,WhiteDirection,BlackDirection);   // White Pawn at a5

            // Place black pieces.
            Board[2][1] = new Bishop(PieceColor.Black); // Black Bishop at b6
            Board[5][3] = new King(PieceColor.Black);   // Black King at d3
            Board[2][2] = new Knight(PieceColor.Black);   // Black Pawn at c5
            Board[3][1] = new Pawn(PieceColor.Black, WhiteDirection, BlackDirection);   // Black Pawn at a2
            Board[7][0] = new Rook(PieceColor.Black);   // Black Rook at a1
            Board[7][1] = new Knight(PieceColor.Black); // Black Knight at b1
            Board[6][0] = new Pawn(PieceColor.Black, WhiteDirection, BlackDirection);   // Black Pawn at b5

            // Set kings' positions.
            WhiteKingPosition = (0, 3);  // White King at d8
            BlackKingPosition = (5, 3);  // Black King at d4

            // Set the current turn to White.
            CurrentTurn = PieceColor.White;
        }

        public void InitializeExampleScenario2()
        {

            // Place white pieces.

            Board[0][3] = new King(PieceColor.White);   // White King at d

            // Place black pieces.
            Board[2][1] = new Bishop(PieceColor.Black); // Black Bishop at b6
            Board[5][3] = new King(PieceColor.Black);   // Black King at d3
            Board[2][2] = new Knight(PieceColor.Black);   // Black Pawn at c5
            Board[3][1] = new Pawn(PieceColor.Black, WhiteDirection, BlackDirection);   // Black Pawn at a2
            Board[7][0] = new Rook(PieceColor.Black);   // Black Rook at a1
            Board[7][1] = new Knight(PieceColor.Black); // Black Knight at b1
            Board[6][0] = new Pawn(PieceColor.Black, WhiteDirection, BlackDirection);   // Black Pawn at b5

            // Set kings' positions.
            WhiteKingPosition = (0, 3);  // White King at d8
            BlackKingPosition = (5, 3);  // Black King at d4

            // Set the current turn to White.
            CurrentTurn = PieceColor.White;
        }

        public void InitializeKingsAndRooksScenario()
        {

            // Set up black pieces
            Board[1][0] = new Rook(PieceColor.Black); // Rook on a8 (0, 0)
            Board[2][0] = new Rook(PieceColor.Black); // Rook on b8 (0, 1)
            Board[3][0] = new Rook(PieceColor.Black); // Rook on c8 (0, 2)
            Board[4][0] = new Rook(PieceColor.Black); // Rook on d8 (0, 3)

            // Set up white pieces
            Board[5][0] = new Rook(PieceColor.Black); // Rook on a1 (7, 0)
            Board[6][0] = new Rook(PieceColor.Black); // Rook on b1 (7, 1)
            Board[7][0] = new Rook(PieceColor.Black); // Rook on c1 (7, 2)
            Board[0][0] = new Rook(PieceColor.Black); // Rook on d1 (7, 3)
            Board[1][3] = new King(PieceColor.White);  // White King on e1 (7, 4)
            Board[2][3] = new King(PieceColor.Black);  // White King on e2 (6, 4)
            WhiteKingPosition = (1, 3);
            BlackKingPosition = (2, 3);

            // Set the current turn
            CurrentTurn = PieceColor.White; // Set the current turn
        }




        // Initialize the board with default pieces
        private void InitializeBoardFlipped()
        {
            // Set starting positions for standard setup-castling
            WhiteKingStart = (0, 3);
            BlackKingStart = (7, 3);
            WhiteRookStart = (0, 0);
            BlackRookStart = (7, 0);

            // Place kings
            Board[WhiteKingStart.Row][WhiteKingStart.Col] = new King(PieceColor.White);
            WhiteKingPosition = WhiteKingStart;

            Board[BlackKingStart.Row][BlackKingStart.Col] = new King(PieceColor.Black);
            BlackKingPosition = BlackKingStart;

            // Place other pieces for White
            Board[0][0] = new Rook(PieceColor.White);
            Board[0][1] = new Knight(PieceColor.White);
            Board[0][2] = new Bishop(PieceColor.White);
            for (int col = 0; col < 4; col++)
                Board[1][col] = new Pawn(PieceColor.White,WhiteDirection,BlackDirection);

            // Place other pieces for Black
            Board[7][0] = new Rook(PieceColor.Black);
            Board[7][1] = new Knight(PieceColor.Black);
            Board[7][2] = new Bishop(PieceColor.Black);
            for (int col = 0; col < 4; col++)
                Board[6][col] = new Pawn(PieceColor.Black, WhiteDirection, BlackDirection);
        }
        private void InitializeBoard()
        {
            WhiteKingStart = (7, 3);
            BlackKingStart = (0, 3);
            WhiteRookStart = (7, 0);
            BlackRookStart = (0, 0);

            // Place kings
            Board[WhiteKingStart.Row][WhiteKingStart.Col] = new King(PieceColor.White);
            WhiteKingPosition = WhiteKingStart;

            Board[BlackKingStart.Row][BlackKingStart.Col] = new King(PieceColor.Black);
            BlackKingPosition = BlackKingStart;

            // Place other pieces for White
            Board[7][0] = new Rook(PieceColor.White);
            Board[7][1] = new Knight(PieceColor.White);
            Board[7][2] = new Bishop(PieceColor.White);
            for (int col = 0; col < 4; col++)
                Board[6][col] = new Pawn(PieceColor.White, WhiteDirection, BlackDirection);

            // Place other pieces for Black
            Board[0][0] = new Rook(PieceColor.Black);
            Board[0][1] = new Knight(PieceColor.Black);
            Board[0][2] = new Bishop(PieceColor.Black);
            for (int col = 0; col < 4; col++)
                Board[1][col] = new Pawn(PieceColor.Black, WhiteDirection, BlackDirection);
        }
        public void CheckCastling()
        {
            // Check white pieces
            if (!WhiteCastlingDisabled && IsInStartPosition(WhiteKingStart, PieceColor.White) && IsInStartPosition(WhiteRookStart, PieceColor.White))
            {
                if (AreSquaresEmptyBetween(WhiteKingStart, WhiteRookStart))
                {
                    EnableCastling(WhiteKingStart);
                    EnableCastling(WhiteRookStart);
                }
            }
            else
            {
                DisableCastling(PieceColor.White);
            }

            // Check black pieces
            if (!BlackCastlingDisabled && IsInStartPosition(BlackKingStart, PieceColor.Black) && IsInStartPosition(BlackRookStart, PieceColor.Black))
            {
                if (AreSquaresEmptyBetween(BlackKingStart, BlackRookStart))
                {
                    EnableCastling(BlackKingStart);
                    EnableCastling(BlackRookStart);
                }
            }
            else
            {
                DisableCastling(PieceColor.Black);
            }
        }
        // Helper method to check if the squares between the King and Rook are empty
        private bool AreSquaresEmptyBetween((int Row, int Col) kingPos, (int Row, int Col) rookPos)
        {
            int startCol = Math.Min(kingPos.Col, rookPos.Col) + 1;
            int endCol = Math.Max(kingPos.Col, rookPos.Col);

            // Check each square between the King and the Rook
            for (int col = startCol; col < endCol; col++)
            {
                if (Board[kingPos.Row][col] != null)
                {
                    return false; // A piece is blocking the castling path
                }
            }
            return true; // All squares are empty
        }
        // Check if a piece is in its starting position
        // Check if a piece is in its starting position and has the correct color
        private bool IsInStartPosition((int Row, int Col) position, PieceColor expectedColor)
        {
            var piece = Board[position.Row][position.Col];
            return piece != null && (piece is King or Rook) && piece.Color == expectedColor;
        }

        // Enable castling for the piece at the given position
        private void EnableCastling((int Row, int Col) position)
        {
            if (Board[position.Row][position.Col] is King king)
                king.CanCastle = true;
            if (Board[position.Row][position.Col] is Rook rook)
                rook.CanCastle = true;
        }

        // Disable castling for a given color, but only once
        private void DisableCastling(PieceColor color)
        {
            if (color == PieceColor.White && !WhiteCastlingDisabled)
            {
                WhiteCanCastle = false;
                WhiteCastlingDisabled = true; // Mark as disabled
                DisableKing(WhiteKingPosition);
                DisableRook(PieceColor.White);
            }
            else if (color == PieceColor.Black && !BlackCastlingDisabled)
            {
                BlackCanCastle = false;
                BlackCastlingDisabled = true; // Mark as disabled
                DisableKing(BlackKingPosition);
                DisableRook(PieceColor.Black);
            }
        }

        // Disable castling for the piece at the given position
        private void DisableKing((int Row, int Col) position)
        {
            if (Board[position.Row][position.Col] is King king)
                king.CanCastle = false;
        }
        // Move a piece from one location to another

        private void DisableRook(PieceColor color)
        {
            Parallel.For(0, Board.Length, (i, state) =>
            {
                for (int j = 0; j < Board[i].Length; j++)
                {
                    var piece = Board[i][j];
                    if (piece is Rook rook && rook.Color == color)
                    {
                        rook.CanCastle = false;  // Disable castling for the rook

                        state.Break();  // Signal to stop further iterations at higher indexes
                        return;  // Exit the inner loop
                    }
                }
            });
        }

        public bool MovePiece((int Row, int Col) from, (int Row, int Col) to)
        {
            // Check if the game is over
            if (WinMethod != null)
            {
                // If Winner is null, indicate a tie
                if (Winner == null)
                {
                    throw new TieException("The game is a stalemate, resulting in a tie.");
                }

                throw new GameOverException($"Game is over. {Winner} has already won!");
            }

            var piece = Board[from.Row][from.Col];
            if (piece == null)
                throw new InvalidOperationException("No piece at the starting location.");
            if (piece.Color != CurrentTurn)
                throw new InvalidOperationException("It's not this player's turn.");
            if (!piece.CanMove(from, to, Board))
                return false;


            // **Handle Castling**: If moving between a King and its Rook, assume castling.
            if (IsCastlingMove(from, to, piece.Color))
            {
                var updatedPositions = UpdateBoardForCastling(from, to, piece.Color); // Update the board and get updated positions
                DisableCastling(piece.Color); // Disable future castling for this color
                SwitchTurn(); // Switch turns

                // Throw a CastlingException with the updated positions
                throw new CastlingException("Castling performed successfully.", updatedPositions);
            }


            // Check for tie conditions
            if (IsStalemate(CurrentTurn)) 
            {
                Winner = null; // No winner in a stalemate
                WinMethod = "The game is a stalemate, resulting in a tie.";
                throw new TieException(WinMethod); // Throw TieException with a custom message
            }

            if (NoStepsLeft(CurrentTurn))
            {
                if (IsCheck(CurrentTurn))
                {
                    Winner = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
                    WinMethod = $"Checkmate! A decisive victory for {Winner}.";

                    throw new CheckmateException(WinMethod); // Throw CheckmateException if it's checkmate.
                }
                // Move the piece

                Board[to.Row][to.Col] = piece;
                Board[from.Row][from.Col] = null;

                // Switch turns
                SwitchTurn();
                return true;
            }

            // Store the previous value of the Fifty-Move counter
            int previousFiftyMoveCounter = FiftyMoveCounter;
            // Backup the target square
            var backupTo = Board[to.Row][to.Col];

            // Move the piece

            Board[to.Row][to.Col] = piece;
            Board[from.Row][from.Col] = null;

            // Update Fifty-Move counter
            if (piece is Pawn || backupTo != null) // If a pawn was moved or a piece was captured
            {
                FiftyMoveCounter = 0; // Reset counter on pawn move or capture
            }
            else
            {
                FiftyMoveCounter++; // Increment counter for regular moves
            }

            // Update king position if needed
            if (piece is King)
            {
                if (piece.Color == PieceColor.White)
                    WhiteKingPosition = to;
                else
                    BlackKingPosition = to;
            }

            // Check for check condition
            if (IsCheck(CurrentTurn))
            {
                UndoMove(from, to, piece, backupTo, previousFiftyMoveCounter);
                throw new IlegalMoveException("This move puts the king in check.");
            }

            // Handle pawn promotion
            if (piece is Pawn pawn)
            {
                pawn.MarkAsMoved();
                if (to.Row == 0 || to.Row == 7)
                {
                    ResetEnPassantFlags();
                    throw new PawnException("Pawn promotion is required.");
                }
                var res = PerformEnPassant(from, to);

                if (res == null)
                {
                    ResetEnPassantFlags();
                    CheckEnPassantEligibility(from, to, pawn);
                }
                else
                {
                    string message = $"En passant captured from {from} to {to}.";
                    SwitchTurn();
                    throw new EnpassantException(message, res.Value);
                }
            }
            CheckCastling();
            // Switch turns
            SwitchTurn();
            return true;
        }
        // Helper method to check if the move is castling
        private bool IsCastlingMove((int Row, int Col) from, (int Row, int Col) to, PieceColor color)
        {
            var fromPiece = Board[from.Row][from.Col];
            var toPiece = Board[to.Row][to.Col];

            return (fromPiece is King && toPiece is Rook && fromPiece.Color == color && toPiece.Color == color) ||
                   (fromPiece is Rook && toPiece is King && fromPiece.Color == color && toPiece.Color == color);
        }
        // Update the Board for Castling
        private List<(int Row, int Col)> UpdateBoardForCastling((int Row, int Col) from, (int Row, int Col) to, PieceColor color)
        {
            // Retrieve the pieces from the board
            var fromPiece = Board[from.Row][from.Col];
            var toPiece = Board[to.Row][to.Col];

            // Handle both scenarios: King selected first or Rook selected first
            if (fromPiece is King king && toPiece is Rook rook && king.Color == rook.Color)
            {
                return PerformCastling(king, rook, from, to, color);
            }
            else if (fromPiece is Rook rookAlt && toPiece is King kingAlt && kingAlt.Color == rookAlt.Color)
            {
                return PerformCastling(kingAlt, rookAlt, to, from, color);
            }
            else
            {
                throw new InvalidOperationException("Invalid castling move. King or Rook not found or mismatched.");
            }
        }

        // Helper method to perform the castling logic and return updated positions

        // Helper method to perform the castling logic and return updated positions
        private List<(int Row, int Col)> PerformCastling(King king, Rook rook, (int Row, int Col) kingPos, (int Row, int Col) rookPos, PieceColor color)
        {
            // Determine new positions based on the color
            // On the 8x4 board, both King and Rook are on the same side (left side)
            int newKingCol = 1; // King moves two columns to the left
            int newRookCol = 2; // Rook moves just right of the King

            // Move the pieces to their new positions
            Board[kingPos.Row][newKingCol] = king;
            Board[rookPos.Row][newRookCol] = rook;

            // Clear the old positions
            Board[kingPos.Row][kingPos.Col] = null;
            Board[rookPos.Row][rookPos.Col] = null;

            // Update the King's position
            if (color == PieceColor.White)
                WhiteKingPosition = (kingPos.Row, newKingCol);
            else
                BlackKingPosition = (kingPos.Row, newKingCol);

            // Return the list of updated positions
            return new List<(int Row, int Col)>
            {
        (kingPos.Row, kingPos.Col), // Original King position
        (rookPos.Row, rookPos.Col), // Original Rook position
        (kingPos.Row, newKingCol),  // New King position
        (rookPos.Row, newRookCol)   // New Rook position
            };
        }

        private void CheckEnPassantEligibility((int Row, int Col) from, (int Row, int Col) to, ChessPiece piece)
        {
            if (Math.Abs(from.Row - to.Row) == 2 && piece is Pawn pawn && pawn.MoveCounter == 1)
            {
                // Check left for en passant eligibility
                int leftRow = to.Row;
                int leftCol = to.Col - 1;

                if (IsWithinBounds(leftRow, leftCol, Board) && Board[leftRow][leftCol] is Pawn leftPawn && leftPawn.Color != piece.Color)
                {
                    RightEnPassantLocation = (leftRow, leftCol); // Store the location of the left pawn
                    leftPawn.EnPassantRight = true;
                }

                // Check right for en passant eligibility
                int rightRow = to.Row;
                int rightCol = to.Col + 1;

                if (IsWithinBounds(rightRow, rightCol, Board) && Board[rightRow][rightCol] is Pawn rightPawn && rightPawn.Color != piece.Color)
                {
                    LeftEnPassantLocation = (rightRow, rightCol); // Store the location of the right pawn
                    rightPawn.EnPassantLeft = true;
                }
            }
        }

        // Undo the last move if it results in a check
        private void UndoMove((int Row, int Col) from, (int Row, int Col) to, ChessPiece piece, ChessPiece? backupTo, int previousFiftyMoveCounter)
        {
            Board[from.Row][from.Col] = piece;
            Board[to.Row][to.Col] = backupTo;

            if (piece is King)
            {
                if (piece.Color == PieceColor.White) WhiteKingPosition = from;
                else BlackKingPosition = from;
            }

            FiftyMoveCounter = previousFiftyMoveCounter;
        }
        private bool IsWithinBounds(int row, int col, ChessPiece?[][] board)
        {
            return row >= 0 && row < board.Length && col >= 0 && col < board[0].Length;
        }
        public void InitializeSenrio3()
        {

            // Set up black pieces
            Board[0][0] = new Rook(PieceColor.Black); // Rook on a8 (0, 0)
            Board[0][2] = new Rook(PieceColor.Black); // Rook on b8 (0, 1)
            Board[3][1] = new King(PieceColor.Black);  // White King on e1 (7, 4)

            // Set up white pieces
            Board[1][1] = new King(PieceColor.White);  // White King on e1 (7, 4)
            BlackKingPosition = (3, 1);
            WhiteKingPosition = (1, 1);

            // Set the current turn
            CurrentTurn = PieceColor.White; // Set the current turn
        }
        public void InitializeSenrio4()
        {

            // Set up black pieces
            Board[0][0] = new Rook(PieceColor.Black); // Rook on a8 (0, 0)
            Board[0][2] = new Rook(PieceColor.Black); // Rook on b8 (0, 1)
            Board[0][1] = new Rook(PieceColor.Black); // Rook on a8 (0, 0)
            Board[2][1] = new King(PieceColor.White);  // White King on e1 (7, 4)

            // Set up white pieces
            Board[3][3] = new King(PieceColor.Black);  // White King on e1 (7, 4)
            BlackKingPosition = (3, 3);
            WhiteKingPosition = (2, 1);

            // Set the current turn
            CurrentTurn = PieceColor.White; // Set the current turn
        }

        // Check if a player's king is in check
        public bool IsCheck(PieceColor player)
        {
            var kingPosition = player == PieceColor.White ? WhiteKingPosition : BlackKingPosition;
            return IsSquareUnderAttack(kingPosition, GetOpponent(player));
        }
        // Check if a player is in checkmate
        public bool NoStepsLeft(PieceColor player)
        {
            // Iterate over each row of the chessboard
            for (int row = 0; row < Rows; row++)
            {
                // Iterate over each column of the chessboard
                for (int col = 0; col < Cols; col++)
                {
                    // Get the piece at the current position
                    var piece = Board[row][col];

                    // Check if the piece is not null and belongs to the current player
                    if (piece != null && piece.Color == player)
                    {
                        // Get all possible moves for the current piece
                        var moves = piece.Range((row, col), Board);

                        // Evaluate each possible move for the piece
                        foreach (var move in moves)
                        {
                            // Backup the current piece's target square
                            var backup = Board[move.Item1][move.Item2];

                            // Move the piece to the target square
                            Board[move.Item1][move.Item2] = piece;
                            // Remove the piece from its original square
                            Board[row][col] = null;

                            // If the piece is a King, update its position
                            if (piece is King)
                            {
                                if (piece.Color == PieceColor.White)
                                    WhiteKingPosition = move; // Update white king position
                                else
                                    BlackKingPosition = move; // Update black king position
                            }

                            // Check if the current player's king is still in check
                            bool stillInCheck = IsCheck(player);

                            // Restore the board to its previous state
                            Board[row][col] = piece; // Move the piece back to its original square
                            Board[move.Item1][move.Item2] = backup; // Restore the target square

                            // If the piece is a King, reset its position
                            if (piece is King)
                            {
                                if (piece.Color == PieceColor.White)
                                    WhiteKingPosition = (row, col); // Reset white king position
                                else
                                    BlackKingPosition = (row, col); // Reset black king position
                            }

                            // If the king is not in check after this move, the player is not in checkmate
                            if (!stillInCheck) return false;
                        }
                    }
                }
            }

            // If all pieces have been evaluated and the king is in check, it's checkmate
            return true;
        }

        // Check if a player is in checkmate
        public bool IsCheckmate(PieceColor player)
        {
            if (IsCheck(player) == false) return false;
            // Iterate over each row of the chessboard
            for (int row = 0; row < Rows; row++)
            {
                // Iterate over each column of the chessboard
                for (int col = 0; col < Cols; col++)
                {
                    // Get the piece at the current position
                    var piece = Board[row][col];

                    // Check if the piece is not null and belongs to the current player
                    if (piece != null && piece.Color == player)
                    {
                        // Get all possible moves for the current piece
                        var moves = piece.Range((row, col), Board);

                        // Evaluate each possible move for the piece
                        foreach (var move in moves)
                        {
                            // Backup the current piece's target square
                            var backup = Board[move.Item1][move.Item2];

                            // Move the piece to the target square
                            Board[move.Item1][move.Item2] = piece;
                            // Remove the piece from its original square
                            Board[row][col] = null;

                            // If the piece is a King, update its position
                            if (piece is King)
                            {
                                if (piece.Color == PieceColor.White)
                                    WhiteKingPosition = move; // Update white king position
                                else
                                    BlackKingPosition = move; // Update black king position
                            }

                            // Check if the current player's king is still in check
                            bool stillInCheck = IsCheck(player);

                            // Restore the board to its previous state
                            Board[row][col] = piece; // Move the piece back to its original square
                            Board[move.Item1][move.Item2] = backup; // Restore the target square

                            // If the piece is a King, reset its position
                            if (piece is King)
                            {
                                if (piece.Color == PieceColor.White)
                                    WhiteKingPosition = (row, col); // Reset white king position
                                else
                                    BlackKingPosition = (row, col); // Reset black king position
                            }

                            // If the king is not in check after this move, the player is not in checkmate
                            if (!stillInCheck) return false;
                        }
                    }
                }
            }

            // If all pieces have been evaluated and the king is in check, it's checkmate
            return true;
        }
        private bool IsKingCaged(PieceColor player)
        {
            int pieceCount = 0; // Count of non-king pieces
            bool kingFound = false; // Track if the king is found

            // Lock object for thread-safe updates
            object lockObject = new object();

            // Check all squares on the board in parallel
            Parallel.For(0, Rows, (row, state) =>
            {
                for (int col = 0; col < Cols; col++)
                {
                    var piece = Board[row][col];

                    if (piece != null)
                    {
                        if (piece.Color == player)
                        {
                            if (piece is King)
                            {
                                // If a king is found, set the kingFound flag
                                lock (lockObject)
                                {
                                    kingFound = true;
                                }
                            }
                            else
                            {
                                // We found a non-king piece, stop checking further
                                lock (lockObject)
                                {
                                    pieceCount++; // Increment the count of non-king pieces
                                    if (pieceCount > 0)
                                    {
                                        state.Break(); // Signal to stop further processing
                                        return; // Exit the current thread
                                    }
                                }
                            }
                        }
                    }
                }
            });

            // If we found any non-king pieces, return false
            if (pieceCount > 0) return false;
            // If only the king is found, check if it has any legal moves and itsnt check and its turn
            return !IsCheck(player)  &&  !CanKingEscape(kingFound ? (player == PieceColor.White ? WhiteKingPosition : BlackKingPosition) : (0, 0), player);
        }
        // Example method for checking if the king can escape (implementation will vary)
        private bool CanKingEscape((int Row, int Col) kingPosition, PieceColor player)
        {
            // Get the king piece at the specified position
            var kingPiece = Board[kingPosition.Row][kingPosition.Col];

            if (kingPiece == null || !(kingPiece is King))
            {
                throw new InvalidOperationException("No king found at the specified position.");
            }

            // Get possible moves for the king using its Range method
            var possibleMoves = kingPiece.Range(kingPosition, Board);

            // Check each possible move
            foreach (var move in possibleMoves)
            {
                // Backup the current position of the king
                var backup = Board[move.Item1][move.Item2]; // Using Item1 and Item2 for row and column

                // Move the king to the potential escape position
                Board[move.Item1][move.Item2] = kingPiece; // Move king to new position
                Board[kingPosition.Row][kingPosition.Col] = null; // Clear original position

                // Check if moving to this square puts the king in check
                bool isSafeMove = !IsSquareUnderAttack(move, GetOpponent(player));

                // Restore the king's original position
                Board[kingPosition.Row][kingPosition.Col] = kingPiece; // Restore original position
                Board[move.Item1][move.Item2] = backup; // Restore backup square

                if (isSafeMove)
                {
                    return true; // The king can escape to this move
                }
            }

            return false; // The king is caged and has no safe moves
        }




        private bool IsStalemate(PieceColor player)
        {
            // Check specific stalemate scenarios in parallel
            var stalemateChecks = new List<Func<bool>>
            {
                () => IsKingVsKing(player),
                () => IsKingVsKingWithPawn(player),
                () => IsKingVsKingWithKnight(player),
                () => IsKingCaged(player),
                () => FiftyMoveCounter >= 50 // Check the Fifty-Move Rule
                

            };

            if (stalemateChecks.AsParallel().Any(check => check()))
            {
                return true; // If any check returns true, it's a stalemate
            }
            return false;
        }

        // Check for King vs. King scenario using parallel processing
        private bool IsKingVsKing(PieceColor player)
        {
            int playerKingCount = 0;
            int opponentKingCount = 0;

            object lockObject = new object(); // Lock object for thread safety

            bool foundNonKingPiece = false;

            Parallel.For(0, Rows, (row, state) =>
            {
                for (int col = 0; col < Cols; col++)
                {
                    var piece = Board[row][col];
                    if (piece is King king)
                    {
                        lock (lockObject) // Ensure thread-safe updates to counts
                        {
                            if (king.Color == player) playerKingCount++;
                            else opponentKingCount++;
                        }
                    }
                    else if (piece != null) // Return false if any other piece is found
                    {
                        foundNonKingPiece = true; // Set the flag to indicate a non-King piece was found
                        state.Break(); // Signal all threads to stop processing
                        return; // Exit the current thread
                    }
                }
            });

            // If a non-King piece was found, return false
            if (foundNonKingPiece) return false;

            return playerKingCount == 1 && opponentKingCount == 1; // Ensure there is one king for each player
        }

        // Check for King vs. King with Pawn scenario using parallel processing
        private bool IsKingVsKingWithPawn(PieceColor player)
        {
            int playerKingCount = 0;
            int opponentKingCount = 0;
            int playerPawnCount = 0;

            object lockObject = new object(); // Lock object for thread safety

            bool foundNonKingPiece = false;

            Parallel.For(0, Rows, (row, state) =>
            {
                for (int col = 0; col < Cols; col++)
                {
                    var piece = Board[row][col];
                    if (piece is King king)
                    {
                        lock (lockObject) // Ensure thread-safe updates to counts
                        {
                            if (king.Color == player) playerKingCount++;
                            else opponentKingCount++;
                        }
                    }
                    else if (piece is Pawn pawn && pawn.Color == player)
                    {
                        lock (lockObject) // Ensure thread-safe updates to counts
                        {
                            playerPawnCount++;
                        }
                    }
                    else if (piece != null) // Return false if any other piece is found
                    {
                        foundNonKingPiece = true; // Set the flag to indicate a non-King piece was found
                        state.Break(); // Signal all threads to stop processing
                        return; // Exit the current thread
                    }
                }
            });

            // If a non-King piece was found, return false
            if (foundNonKingPiece) return false;

            return playerKingCount == 1 && opponentKingCount == 1 && playerPawnCount == 1; // Ensure correct counts
        }

        // Check for King vs. King with Knight scenario using parallel processing
        private bool IsKingVsKingWithKnight(PieceColor player)
        {
            int playerKingCount = 0;
            int opponentKingCount = 0;
            int playerKnightCount = 0;

            object lockObject = new object(); // Lock object for thread safety

            bool foundNonKingPiece = false;

            Parallel.For(0, Rows, (row, state) =>
            {
                for (int col = 0; col < Cols; col++)
                {
                    var piece = Board[row][col];
                    if (piece is King king)
                    {
                        lock (lockObject) // Ensure thread-safe updates to counts
                        {
                            if (king.Color == player) playerKingCount++;
                            else opponentKingCount++;
                        }
                    }
                    else if (piece is Knight knight && knight.Color == player)
                    {
                        lock (lockObject) // Ensure thread-safe updates to counts
                        {
                            playerKnightCount++;
                        }
                    }
                    else if (piece != null) // Return false if any other piece is found
                    {
                        foundNonKingPiece = true; // Set the flag to indicate a non-King piece was found
                        state.Break(); // Signal all threads to stop processing
                        return; // Exit the current thread
                    }
                }
            });

            // If a non-King piece was found, return false
            if (foundNonKingPiece) return false;

            return playerKingCount == 1 && opponentKingCount == 1 && playerKnightCount == 1; // Ensure correct counts
        }


        // Check if a square is under attack
        private bool IsSquareUnderAttack((int Row, int Col) square, PieceColor opponent)
        {
            // Use AsParallel to enable parallel processing of the board evaluation
            var x = Board
               .AsParallel() // Allow processing to occur on multiple threads for better performance
               .SelectMany((row, rowIndex) =>
                   row.Select((piece, colIndex) => (piece, rowIndex, colIndex))) // Flatten the board into a collection of pieces with their coordinates
               .Where(x => x.piece != null && x.piece.Color == opponent) // Filter pieces to find only those that belong to the opponent
               .Any(x =>
                   x.piece != null && // Ensure the piece is not null
                   x.piece.Range((x.rowIndex, x.colIndex), Board).Contains(square) // Check if the opponent's piece can attack the specified square
               );
            return x;
        }


        // Switch the turn to the other player
        public void SwitchTurn()
        {
            CurrentTurn = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
        }

        // Get the opponent's color
        private PieceColor GetOpponent(PieceColor player)
        {
            return player == PieceColor.White ? PieceColor.Black : PieceColor.White;
        }

        // Method to perform en passant capture if applicable
        public (int Row, int Col)? PerformEnPassant((int Row, int Col) from, (int Row, int Col) to)
        {
            if (LeftEnPassantLocation.HasValue && to.Row == from.Row + (CurrentTurn == PieceColor.White ? WhiteDirection : BlackDirection) && to.Col == from.Col - 1)
            {
                // Capture left pawn
                Board[LeftEnPassantLocation.Value.Row][LeftEnPassantLocation.Value.Col - 1] = null; // Remove the captured pawn
                var capturedLocation = (LeftEnPassantLocation.Value.Row, LeftEnPassantLocation.Value.Col - 1); // Store the captured pawn's location
                ResetEnPassantFlags(CurrentTurn);
                return (capturedLocation); // Return the captured location
            }
            else if (RightEnPassantLocation.HasValue && to.Row == from.Row + (CurrentTurn == PieceColor.White ? WhiteDirection : BlackDirection)
                && to.Col == from.Col + 1)
            {
                // Capture right pawn
                Board[RightEnPassantLocation.Value.Row][RightEnPassantLocation.Value.Col + 1] = null; // Remove the captured pawn
                var capturedLocation = (RightEnPassantLocation.Value.Row, RightEnPassantLocation.Value.Col + 1); // Store the captured pawn's location
                ResetEnPassantFlags(CurrentTurn);
                return (capturedLocation); // Return the captured location
            }

            // No en passant capture occurred
            return null; // Return a nullable tuple indicating no capture
        }
        // Method to reset en passant flags if needed
        private void ResetEnPassantFlags(PieceColor player)
        {
            // Reset left pawn's en passant flags
            if (LeftEnPassantLocation.HasValue)
            {
                var leftRow = LeftEnPassantLocation.Value.Row + (player == PieceColor.White ? WhiteDirection : BlackDirection);
                var leftCol = LeftEnPassantLocation.Value.Col - 1;

                if (IsWithinBounds(leftRow, leftCol, Board) && Board[leftRow][leftCol] is Pawn leftPawn)
                {
                    leftPawn.ResetEnPassant(); // Reset left pawn's en passant flags
                    LeftEnPassantLocation = null; // Clear the left en passant location
                }
            }

            // Reset right pawn's en passant flags
            if (RightEnPassantLocation.HasValue)
            {
                var rightRow = RightEnPassantLocation.Value.Row + (player == PieceColor.White ? WhiteDirection : BlackDirection);
                var rightCol = RightEnPassantLocation.Value.Col + 1;

                if (IsWithinBounds(rightRow, rightCol, Board) && Board[rightRow][rightCol] is Pawn rightPawn)
                {
                    rightPawn.ResetEnPassant(); // Reset right pawn's en passant flags
                    RightEnPassantLocation = null; // Clear the right en passant location
                }

            }
        }
            private void ResetEnPassantFlags()
            {
                // Reset left pawn's en passant flags
                if (LeftEnPassantLocation.HasValue)
                {
                    var leftRow = LeftEnPassantLocation.Value.Row ;
                    var leftCol = LeftEnPassantLocation.Value.Col ;

                    if (IsWithinBounds(leftRow, leftCol, Board) && Board[leftRow][leftCol] is Pawn leftPawn)
                    {
                        leftPawn.ResetEnPassant(); // Reset left pawn's en passant flags
                        LeftEnPassantLocation = null; // Clear the left en passant location
                    }
                }

                // Reset right pawn's en passant flags
                if (RightEnPassantLocation.HasValue)
                {
                    var rightRow = RightEnPassantLocation.Value.Row;
                    var rightCol = RightEnPassantLocation.Value.Col;

                    if (IsWithinBounds(rightRow, rightCol, Board) && Board[rightRow][rightCol] is Pawn rightPawn)
                    {
                        rightPawn.ResetEnPassant(); // Reset right pawn's en passant flags
                        RightEnPassantLocation = null; // Clear the right en passant location
                    }

                }
            }

    }
}
