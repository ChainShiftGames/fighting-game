using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FightingGame.Presentation.World;

/// <summary>
/// Reads a character's art file, <c>game/Assets/Characters/&lt;id&gt;/&lt;id&gt;_art.json</c> (developer setting,
/// ADR-0028). An invalid file is refused with the reason, never fixed up.
/// </summary>
public static class FighterArtFile
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
    };

    /// <summary>
    /// Where a file the art file names is: a <c>res://</c> path as written, anything else relative to the character's
    /// folder.
    /// </summary>
    public static string Resolve(string characterId, string file) =>
        file.StartsWith("res://", System.StringComparison.Ordinal) ? file : $"{FolderFor(characterId)}/{file}";

    /// <summary>The art file's <c>res://</c> path for a character.</summary>
    public static string PathFor(string characterId) => $"{FolderFor(characterId)}/{characterId}_art.json";

    /// <summary>The folder the art file, and the model it names, live in.</summary>
    public static string FolderFor(string characterId) => $"res://Assets/Characters/{characterId}";

    /// <param name="error">Why the file was refused; empty when it was read.</param>
    /// <returns>The settings, or null when the file is invalid.</returns>
    public static FighterArtSettings? Read(string json, out string error)
    {
        ArtFileJson? file;
        try
        {
            file = JsonSerializer.Deserialize<ArtFileJson>(json, _json);
        }
        catch (JsonException exception)
        {
            error = exception.Message;
            return null;
        }

        error = Check(file);
        return error.Length > 0 ? null : ToSettings(file!);
    }

    private static string Check(ArtFileJson? file)
    {
        if (file is null)
        {
            return "the file is empty";
        }

        if (file.ArtVersion != CurrentVersion)
        {
            return $"artVersion is {file.ArtVersion}, this build reads {CurrentVersion}";
        }

        if (file.Art == FighterArtKind.Model && string.IsNullOrWhiteSpace(file.Model))
        {
            return "art is model but no model file is named";
        }

        if (file.Scale <= 0f)
        {
            return $"scale {file.Scale} must be above 0";
        }

        foreach (KeyValuePair<string, AnimationJson> entry in file.Animations)
        {
            string problem = CheckAnimation(entry.Value);
            if (problem.Length > 0)
            {
                return $"animations.{entry.Key}: {problem}";
            }
        }

        for (int i = 0; i < file.Transitions.Count; i++)
        {
            string problem = CheckTransition(file.Transitions[i]);
            if (problem.Length > 0)
            {
                return $"transitions[{i}]: {problem}";
            }
        }

        return string.Empty;
    }

    private static string CheckTransition(TransitionJson transition)
    {
        if (transition.From.Count == 0 || transition.To.Count == 0)
        {
            return "from and to each need at least one move";
        }

        if (transition.Frames < 1)
        {
            return $"frames {transition.Frames} must be at least 1";
        }

        return string.IsNullOrWhiteSpace(transition.Clip) && string.IsNullOrWhiteSpace(transition.File)
            ? "name its clip, its file, or both"
            : string.Empty;
    }

    private static string CheckAnimation(AnimationJson animation)
    {
        if (animation.ActiveFrom is null != animation.RecoveryFrom is null)
        {
            return "activeFrom and recoveryFrom go together";
        }

        if (animation.ActiveFrom is { } active && animation.RecoveryFrom is { } recovery &&
            (active < 0 || recovery < active))
        {
            return $"contact frames must have 0 ≤ activeFrom ≤ recoveryFrom, not {active} and {recovery}";
        }

        return string.Empty;
    }

    private static FighterArtSettings ToSettings(ArtFileJson file)
    {
        var animations = new Dictionary<string, AnimationSettings>(System.StringComparer.Ordinal);
        foreach (KeyValuePair<string, AnimationJson> entry in file.Animations)
        {
            AnimationJson animation = entry.Value;
            ContactFrames? contacts = animation.ActiveFrom is { } active && animation.RecoveryFrom is { } recovery
                ? new ContactFrames(active, recovery)
                : null;
            animations[entry.Key] = new AnimationSettings(
                animation.Clip,
                contacts,
                animation.File,
                animation.Loop,
                animation.InPlace);
        }

        var transitions = new List<ClipTransition>(file.Transitions.Count);
        foreach (TransitionJson transition in file.Transitions)
        {
            transitions.Add(new ClipTransition(
                transition.From,
                transition.To,
                new AnimationSettings(transition.Clip, null, transition.File, null, transition.InPlace),
                transition.Frames,
                transition.Reverse));
        }

        return new FighterArtSettings(
            file.Art, file.Model ?? string.Empty, file.Scale, file.TurnDegrees, animations, transitions);
    }

    private sealed class ArtFileJson
    {
        [JsonRequired]
        public int ArtVersion { get; init; }

        [JsonRequired]
        public FighterArtKind Art { get; init; }

        public string? Model { get; init; }

        public float Scale { get; init; } = 1f;

        public float TurnDegrees { get; init; }

        public Dictionary<string, AnimationJson> Animations { get; init; } = [];

        public List<TransitionJson> Transitions { get; init; } = [];
    }

    private sealed class TransitionJson
    {
        [JsonRequired]
        public List<string> From { get; init; } = [];

        [JsonRequired]
        public List<string> To { get; init; } = [];

        public string? File { get; init; }

        public string? Clip { get; init; }

        [JsonRequired]
        public int Frames { get; init; }

        public bool Reverse { get; init; }

        public bool InPlace { get; init; }
    }

    private sealed class AnimationJson
    {
        public string? Clip { get; init; }

        public int? ActiveFrom { get; init; }

        public int? RecoveryFrom { get; init; }

        public string? File { get; init; }

        public bool? Loop { get; init; }

        public bool InPlace { get; init; }
    }
}
