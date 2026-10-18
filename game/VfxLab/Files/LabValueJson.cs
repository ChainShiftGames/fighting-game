using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FightingGame.VfxLab.Files;

/// <summary>
/// Writes a <see cref="LabValue"/> as JSON the way people write it: a number, <c>true</c>/<c>false</c>, a
/// <c>"#rrggbb"</c> colour or an array of numbers.
/// </summary>
public sealed class LabValueJson : JsonConverter<LabValue>
{
    // Developer setting: decimals kept when the Lab writes a value, so feedback files stay readable.
    private const int Decimals = 4;

    public override LabValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return LabValue.Number(reader.GetSingle());
            case JsonTokenType.True:
            case JsonTokenType.False:
                return LabValue.Toggle(reader.GetBoolean());
            case JsonTokenType.String:
                string text = reader.GetString() ?? string.Empty;
                return LabColourHex.TryParse(text, out LabValue colour)
                    ? colour
                    : throw new JsonException($"\"{text}\" is not a #rrggbb or #rrggbbaa colour");
            case JsonTokenType.StartArray:
                float[] numbers = JsonSerializer.Deserialize<float[]>(ref reader, options) ?? [];
                return numbers.Length is >= 1 and <= 4
                    ? LabValue.Vector(numbers)
                    : throw new JsonException("a vector has 1 to 4 numbers");
            default:
                throw new JsonException("a value is a number, true/false, a \"#rrggbb\" colour or an array of numbers");
        }
    }

    public override void Write(Utf8JsonWriter writer, LabValue value, JsonSerializerOptions options)
    {
        switch (value.Format)
        {
            case LabValueFormat.Toggle:
                writer.WriteBooleanValue(value.First > 0.5f);
                break;
            case LabValueFormat.Colour:
                writer.WriteStringValue(LabColourHex.Format(value));
                break;
            case LabValueFormat.Vector:
                writer.WriteStartArray();
                foreach (float number in value.Numbers)
                {
                    writer.WriteNumberValue(Math.Round(number, Decimals));
                }

                writer.WriteEndArray();
                break;
            default:
                writer.WriteNumberValue(Math.Round(value.First, Decimals));
                break;
        }
    }
}
