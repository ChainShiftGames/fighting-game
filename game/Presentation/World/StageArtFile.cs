using System.Text.Json;
using System.Text.Json.Serialization;

namespace FightingGame.Presentation.World;

/// <summary>
/// Reads the stage art file, <c>game/Assets/Stages/stage_art.json</c> (developer setting): the background picture
/// the placeholder stage shows. An invalid file is refused with the reason, never fixed up.
/// </summary>
public static class StageArtFile
{
    public const int CurrentVersion = 1;

    /// <summary>The folder the stage art file, and the picture it names, live in.</summary>
    public const string Folder = "res://Assets/Stages";

    public const string Path = Folder + "/stage_art.json";

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    /// <summary>Where the picture is: a <c>res://</c> path as written, anything else relative to <see cref="Folder"/>.</summary>
    public static string Resolve(string file) =>
        file.StartsWith("res://", System.StringComparison.Ordinal) ? file : $"{Folder}/{file}";

    /// <param name="error">Why the file was refused; empty when it was read.</param>
    public static StageArtSettings? Read(string json, out string error)
    {
        StageArtJson? file;
        try
        {
            file = JsonSerializer.Deserialize<StageArtJson>(json, _json);
        }
        catch (JsonException exception)
        {
            error = exception.Message;
            return null;
        }

        error = file switch
        {
            null => "the file is empty",
            { ArtVersion: not CurrentVersion } => $"artVersion is {file.ArtVersion}, this build reads {CurrentVersion}",
            { GroundLine: < 0f or >= 1f } => $"groundLine {file.GroundLine} must be from 0 up to 1",
            { Depth: <= 0f } => $"depth {file.Depth} must be above 0",
            { Parallax: < 0f or >= 1f } => $"parallax {file.Parallax} must be from 0 up to 1",
            _ => string.Empty,
        };
        return error.Length > 0 ? null : new StageArtSettings(file!.Background ?? string.Empty, file.GroundLine, file.Depth, file.Parallax);
    }

    private sealed class StageArtJson
    {
        [JsonRequired]
        public int ArtVersion { get; init; }

        public string? Background { get; init; }

        public float GroundLine { get; init; }

        public float Depth { get; init; } = 4f;

        public float Parallax { get; init; }
    }
}
