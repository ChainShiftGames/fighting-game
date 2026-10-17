using System.Text.Json;
using System.Text.Json.Serialization;

namespace FightingGame.Training;

/// <summary>
/// Reads and writes the <see cref="TrainingSettings"/> file's JSON; enums are saved by name. An unreadable file or an
/// unknown value falls back to the defaults with a warning: a bad settings file must never stop the game.
/// </summary>
public static class TrainingSettingsJson
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
    };

    public static string Write(TrainingSettings settings) => JsonSerializer.Serialize(settings, _json);

    /// <param name="warning">Why the file was replaced by the defaults; empty when it wasn't.</param>
    public static TrainingSettings Read(string json, out string warning)
    {
        warning = string.Empty;
        try
        {
            return JsonSerializer.Deserialize<TrainingSettings>(json, _json) ?? new TrainingSettings();
        }
        catch (JsonException exception)
        {
            warning = $"unreadable, using the defaults: {exception.Message}";
            return new TrainingSettings();
        }
    }
}
