using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChessGameWPF.Models;
namespace ChessGameWPF.Models
{
    public class ChessPieceArrayConverter : JsonConverter<ChessPiece?[][]>
    {
        public override ChessPiece?[][] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var rows = new List<ChessPiece?[]>();

            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected StartArray token.");

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                var row = new List<ChessPiece?>();

                if (reader.TokenType != JsonTokenType.StartArray)
                    throw new JsonException("Expected StartArray token for row.");

                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType == JsonTokenType.Null)
                    {
                        row.Add(null);
                    }
                    else if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        var pieceElement = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
                        var piece = CreatePieceWithProperties(pieceElement);
                        row.Add(piece);
                    }
                    else
                    {
                        throw new JsonException("Expected either null or a piece object.");
                    }
                }

                rows.Add(row.ToArray());
            }

            return rows.ToArray();
        }

        public override void Write(Utf8JsonWriter writer, ChessPiece?[][] value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            foreach (var row in value)
            {
                writer.WriteStartArray();
                foreach (var piece in row)
                {
                    if (piece == null)
                    {
                        writer.WriteNullValue();
                    }
                    else
                    {
                        JsonSerializer.Serialize(writer, piece, options);
                    }
                }
                writer.WriteEndArray();
            }

            writer.WriteEndArray();
        }

        private ChessPiece CreatePieceWithProperties(JsonElement element)
        {
            // Safely get the "Type" property and ensure it's not null.
            string type = element.GetProperty("Type").GetString()
                          ?? throw new JsonException("Piece type cannot be null.");
            PieceColor color = (PieceColor)element.GetProperty("Color").GetInt32();

            return type switch
            {
                "King" => CreateKingWithProperties(element, color),
                "Rook" => CreateRookWithProperties(element, color),
                "Pawn" => CreatePawnWithProperties(element, color),
                "Knight" => new Knight(color),
                "Bishop" => new Bishop(color),
                _ => throw new JsonException($"Unknown piece type: {type}")
            };
        }

        private King CreateKingWithProperties(JsonElement element, PieceColor color)
        {
            bool canCastle = element.TryGetProperty("CanCastle", out JsonElement canCastleElement) && canCastleElement.GetBoolean();
            return new King(color,canCastle);
        }

        private Rook CreateRookWithProperties(JsonElement element, PieceColor color)
        {
            bool canCastle = element.TryGetProperty("CanCastle", out JsonElement canCastleElement) && canCastleElement.GetBoolean();
            return new Rook(color, canCastle);
        }

        private Pawn CreatePawnWithProperties(JsonElement element, PieceColor color)
        {
            int moveCounter = element.TryGetProperty("MoveCounter", out JsonElement moveCounterElement)
                ? moveCounterElement.GetInt32()
                : 0;

            bool enPassantLeft = element.TryGetProperty("EnPassantLeft", out JsonElement enPassantLeftElement) && enPassantLeftElement.GetBoolean();
            bool enPassantRight = element.TryGetProperty("EnPassantRight", out JsonElement enPassantRightElement) && enPassantRightElement.GetBoolean();

            int whiteDirection = element.TryGetProperty("WhiteDirection", out JsonElement whiteDirectionElement)
                ? whiteDirectionElement.GetInt32()
                : 1; // Default to 1 if not present

            int blackDirection = element.TryGetProperty("BlackDirection", out JsonElement blackDirectionElement)
                ? blackDirectionElement.GetInt32()
                : -1; // Default to -1 if not present

            return new Pawn(color, moveCounter, enPassantLeft, enPassantRight, whiteDirection, blackDirection);
        }
    }
}

