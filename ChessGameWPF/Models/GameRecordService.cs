using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessGameWPF.Models
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Configuration;
    using Microsoft.Data.SqlClient;

    public class GameRecordService
    {
        private readonly string _connectionString;

        public GameRecordService()
        {
            // Initialize connection string from configuration
            _connectionString = ConfigurationManager.ConnectionStrings["ChessDB"].ConnectionString;
        }

        public void SaveGame(int userId, int gameId, PieceColor userColor, PieceColor computerColor)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
            INSERT INTO Games (GameId, UserId, GameDate, Winner, WinMethod, ComputerColor, UserColor)
            VALUES (@GameId, @UserId, @GameDate, NULL, NULL,@ComputerColor,@UserColor)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@GameId", gameId);
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@GameDate", DateTime.Now);
                    command.Parameters.AddWithValue("@ComputerColor", computerColor == PieceColor.White);
                    command.Parameters.AddWithValue("@UserColor", userColor == PieceColor.White);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        public void SaveStep(int gameId, (int Row, int Col) from, (int Row, int Col) to, int stepOrder, string pieceType, string? promotion, bool enPassant, bool castling)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
            INSERT INTO GameSteps (GameId, StepOrder, FromPosition, ToPosition, PieceType, Promotion, EnPassant, Castling)
            VALUES (@GameId, @StepOrder, @FromPosition, @ToPosition, @PieceType, @Promotion, @EnPassant, @Castling)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@GameId", gameId);
                    command.Parameters.AddWithValue("@StepOrder", stepOrder);
                    command.Parameters.AddWithValue("@FromPosition", $"{from.Row},{from.Col}");
                    command.Parameters.AddWithValue("@ToPosition", $"{to.Row},{to.Col}");
                    command.Parameters.AddWithValue("@PieceType", pieceType ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Promotion", promotion ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@EnPassant", enPassant);
                    command.Parameters.AddWithValue("@Castling", castling);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateGameWinner(int gameId, string winner, string winMethod)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
                UPDATE Games
                SET Winner = @Winner, WinMethod = @WinMethod
                WHERE GameId = @GameId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@GameId", gameId);
                    command.Parameters.AddWithValue("@Winner", winner);
                    command.Parameters.AddWithValue("@WinMethod", winMethod);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        public async Task<List<MoveRecord>> GetMovesForGameAsync(int gameId)
        {
            List<MoveRecord> moves = new List<MoveRecord>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
            SELECT StepOrder, FromPosition, ToPosition, PieceType, Promotion, EnPassant, Castling
            FROM GameSteps
            WHERE GameId = @GameId
            ORDER BY StepOrder ASC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@GameId", gameId);

                    connection.Open();
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int stepOrder = reader.GetInt32(0);
                            (int fromRow, int fromCol) = ParsePosition(reader.GetString(1));
                            (int toRow, int toCol) = ParsePosition(reader.GetString(2));
                            string pieceType = reader.GetString(3);
                            string? promotion = reader.IsDBNull(4) ? null : reader.GetString(4);
                            bool enPassant = reader.GetBoolean(5);
                            bool castling = reader.GetBoolean(6);

                            moves.Add(new MoveRecord(stepOrder, fromRow, fromCol, toRow, toCol, promotion, enPassant, castling));
                        }
                    }
                }
            }

            return moves;
        }


        public async Task<List<GameSummary>> GetAllGamesAsync()
        {
            List<GameSummary> games = new List<GameSummary>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
            SELECT GameId, GameDate, ComputerColor, UserColor
            FROM Games
            ORDER BY GameDate DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int gameId = reader.GetInt32(0);
                            DateTime gameDate = reader.GetDateTime(1);
                            bool computerColor = reader.GetBoolean(2);
                            bool userColor = reader.GetBoolean(3);

                            games.Add(new GameSummary(gameId, gameDate, userColor, computerColor));
                        }
                    }
                }
            }

            return games;
        }

        public async Task<GameRecord> GetGameDetailsAsync(int gameId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
            SELECT Winner, WinMethod, ComputerColor, UserColor
            FROM Games
            WHERE GameId = @GameId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@GameId", gameId);

                    connection.Open();
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string winner = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0);
                            string winMethod = reader.IsDBNull(1) ? "Unknown method" : reader.GetString(1);
                            bool computerColor = reader.GetBoolean(2);
                            bool userColor = reader.GetBoolean(3);

                            return new GameRecord(gameId, winner, winMethod, userColor, computerColor);
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Game with ID {gameId} not found.");
        }

        private (int Row, int Col) ParsePosition(string position)
        {
            var parts = position.Split(',');
            return (int.Parse(parts[0]), int.Parse(parts[1]));
        }
    }

}