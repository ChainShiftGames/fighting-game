using System.Text.Json;
using Netcode.Online;

namespace FightingGame.Settings;

/// <summary>
/// Reads and writes the <see cref="PlayerSettings"/> file's JSON. A value out of range or an unreadable file falls back
/// to the default with a warning: a bad settings file must never stop the game.
/// </summary>
public static class PlayerSettingsJson
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static string Write(PlayerSettings settings) => JsonSerializer.Serialize(settings, _json);

    /// <param name="warning">Why a value or the whole file was replaced by the default; empty when nothing was.</param>
    public static PlayerSettings Read(string json, out string warning)
    {
        PlayerSettings settings;
        try
        {
            settings = JsonSerializer.Deserialize<PlayerSettings>(json, _json) ?? new PlayerSettings();
        }
        catch (JsonException exception)
        {
            warning = $"unreadable, using the defaults: {exception.Message}";
            return new PlayerSettings();
        }

        warning = string.Empty;
        if (settings.InputDelayOverrideFrames is { } frames and
            (< InputDelaySettings.MinOverrideFrames or > InputDelaySettings.MaxOverrideFrames))
        {
            warning = $"inputDelayOverrideFrames {frames} is outside {InputDelaySettings.MinOverrideFrames}–" +
                $"{InputDelaySettings.MaxOverrideFrames}, using the automatic delay";
            settings = settings with { InputDelayOverrideFrames = null };
        }

        return settings;
    }
}
