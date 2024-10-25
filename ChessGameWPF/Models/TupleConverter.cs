using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessGameWPF.Models
{
    public class TupleConverter : JsonConverter<(int Row, int Col)>
    {
        public override (int Row, int Col) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Read the start of the object
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object");

            reader.Read(); // Move to first property

            // Read "Item1"
            if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() != "Item1")
                throw new JsonException("Expected property name 'Item1'");

            reader.Read(); // Move to value of Item1
            var row = reader.GetInt32(); // Read Item1 value

            // Read "Item2"
            reader.Read(); // Move to next property
            if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() != "Item2")
                throw new JsonException("Expected property name 'Item2'");

            reader.Read(); // Move to value of Item2
            var col = reader.GetInt32(); // Read Item2 value

            reader.Read(); // Read end of object
            if (reader.TokenType != JsonTokenType.EndObject)
                throw new JsonException("Expected end of object");

            return (row, col);
        }

        public override void Write(Utf8JsonWriter writer, (int Row, int Col) value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("Item1", value.Row);
            writer.WriteNumber("Item2", value.Col);
            writer.WriteEndObject();
        }
    }
}
