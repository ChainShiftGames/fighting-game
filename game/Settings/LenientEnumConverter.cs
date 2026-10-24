using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FightingGame.Settings;

/// <summary>
/// Writes an enum by name and reads a name it doesn't know as an undefined value, so the settings check replaces that
/// one value with its default instead of the whole file failing to load.
/// </summary>
public sealed class LenientEnumConverter<T> : JsonConverter<T>
    where T : struct, Enum
{
    // No enum here uses -1, so it marks "unknown" for the settings check.
    private const int Unknown = -1;

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && Enum.TryParse(reader.GetString(), ignoreCase: true, out T value))
        {
            return value;
        }

        if (reader.TokenType is not (JsonTokenType.String or JsonTokenType.Number))
        {
            reader.Skip();
        }

        return (T)Enum.ToObject(typeof(T), Unknown);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        writer.WriteStringValue(JsonNamingPolicy.CamelCase.ConvertName(value.ToString()));
}
