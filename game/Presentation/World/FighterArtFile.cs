using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using FightingGame.Presentation.World.Projectiles;
using FightingGame.Presentation.World.Sparks;
using Sim.Fighters.States;

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

    /// <summary>The folder holding one folder per character.</summary>
    public const string CharactersFolder = "res://Assets/Characters";

    /// <summary>The art file's <c>res://</c> path for a character.</summary>
    public static string PathFor(string characterId) => $"{FolderFor(characterId)}/{characterId}_art.json";

    /// <summary>The folder the art file, and the model it names, live in.</summary>
    public static string FolderFor(string characterId) => $"{CharactersFolder}/{characterId}";

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

        if (file.CrossfadeFrames < 0 || file.TurnFrames < 0 || file.HoldFrames < 1)
        {
            return "crossfadeFrames and turnFrames can't be negative, and holdFrames must be at least 1";
        }

        if (file.CameraTurnDegrees is < 0f or > 90f)
        {
            return $"cameraTurnDegrees {file.CameraTurnDegrees} must be from 0 to 90";
        }

        for (int i = 0; i < file.OtherModels.Count; i++)
        {
            ModelJson other = file.OtherModels[i];
            if (string.IsNullOrWhiteSpace(other.Model) || other.Scale <= 0f)
            {
                return $"otherModels[{i}]: name its model file, with a scale above 0";
            }
        }

        foreach (KeyValuePair<string, AnimationJson> entry in file.Animations)
        {
            string problem = CheckAnimation(entry.Value);
            if (problem.Length > 0)
            {
                return $"animations.{entry.Key}: {problem}";
            }
        }

        if (file.KnockOut is { } knockOut && CheckAnimation(knockOut) is { Length: > 0 } knockOutProblem)
        {
            return $"knockOut: {knockOutProblem}";
        }

        foreach (KeyValuePair<string, ProjectileJson> entry in file.Projectiles)
        {
            string problem = CheckProjectile(entry.Value);
            if (problem.Length > 0)
            {
                return $"projectiles.{entry.Key}: {problem}";
            }
        }

        foreach (KeyValuePair<string, List<SparkJson>> entry in file.Sparks)
        {
            for (int hit = 0; hit < entry.Value.Count; hit++)
            {
                string problem = CheckSpark(entry.Value[hit]);
                if (problem.Length > 0)
                {
                    return $"sparks.{entry.Key}[{hit}]: {problem}";
                }
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

    private static string CheckProjectile(ProjectileJson projectile)
    {
        if (!ProjectileEffects.FlightExists(projectile.Flight))
        {
            return $"no flight effect is called '{projectile.Flight}'";
        }

        if (projectile.Charge is { } charge && !ProjectileEffects.ChargeExists(charge))
        {
            return $"no charge effect is called '{charge}'";
        }

        return projectile.Impact is { } impact && !ProjectileEffects.ImpactExists(impact)
            ? $"no impact effect is called '{impact}'"
            : string.Empty;
    }

    private static string CheckSpark(SparkJson spark)
    {
        if (spark.Angle is < -360f or > 360f)
        {
            return $"angle {spark.Angle} must be from -360 to 360 degrees";
        }

        return spark.Scale <= 0f ? $"scale {spark.Scale} must be above 0" : string.Empty;
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

        if (animation.ClipFrom < 0 || animation.ClipTo <= (animation.ClipFrom ?? 0))
        {
            return "clipFrom must be 0 or more, and clipTo after it";
        }

        if (animation.Speed <= 0f)
        {
            return $"speed {animation.Speed} must be above 0";
        }

        if (CheckKeys(animation) is { Length: > 0 } keyProblem)
        {
            return keyProblem;
        }

        return animation.HoldFrames < 1 || animation.CrossfadeFrames < 0
            ? "holdFrames must be at least 1 and crossfadeFrames 0 or more"
            : string.Empty;
    }

    // Keys pin frames inside the spans the contact frames set, in phase order and in order within each phase, so the
    // retiming can walk them once.
    private static string CheckKeys(AnimationJson animation)
    {
        if (animation.Keys is not { Count: > 0 } keys)
        {
            return string.Empty;
        }

        if (animation.ActiveFrom is null || animation.Fit)
        {
            return "keys need activeFrom and recoveryFrom, and don't go with fit";
        }

        for (int i = 0; i < keys.Count; i++)
        {
            KeyJson key = keys[i];
            if (key.Phase == MovePhase.None || key.Of < 2 || key.Frame < 2 || key.Frame > key.Of || key.Clip < 0)
            {
                return $"keys[{i}]: needs a phase (startup, active or recovery), frame from 2 to of, and clip 0 or more";
            }

            if (i > 0 && !ComesAfter(key, keys[i - 1]))
            {
                return $"keys[{i}]: keys go in phase order, and later in their phase than the key before";
            }
        }

        return string.Empty;
    }

    private static bool ComesAfter(KeyJson key, KeyJson before) => key.Phase > before.Phase ||
        (key.Phase == before.Phase && ToKey(key).Position > ToKey(before).Position);

    private static TimingKey ToKey(KeyJson key) => new(key.Phase, key.Frame, key.Of, key.Clip);

    private static FighterArtSettings ToSettings(ArtFileJson file)
    {
        var animations = new Dictionary<string, AnimationSettings>(System.StringComparer.Ordinal);
        foreach (KeyValuePair<string, AnimationJson> entry in file.Animations)
        {
            AnimationJson animation = entry.Value;
            ContactFrames? contacts = animation.ActiveFrom is { } active && animation.RecoveryFrom is { } recovery
                ? new ContactFrames(active, recovery)
                : null;
            animations[entry.Key] = ToSettings(animation, contacts);
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
            file.Art, file.Model ?? string.Empty, file.Scale, file.TurnDegrees, animations, transitions)
        {
            KnockOut = file.KnockOut is { } knockOut ? ToSettings(knockOut, null) : null,
            CrossfadeFrames = file.CrossfadeFrames,
            TurnFrames = file.TurnFrames,
            CameraTurnDegrees = file.CameraTurnDegrees,
            HoldFrames = file.HoldFrames,
            Projectiles = ToSettings(file.Projectiles),
            OtherModels = file.OtherModels.ConvertAll(
                other => new FighterModelVariant(other.Model, other.Scale, other.TurnDegrees)),
            Sparks = ToSettings(file.Sparks),
        };
    }

    private static Dictionary<string, ProjectileArtSettings> ToSettings(Dictionary<string, ProjectileJson> projectiles)
    {
        var settings = new Dictionary<string, ProjectileArtSettings>(System.StringComparer.Ordinal);
        foreach (KeyValuePair<string, ProjectileJson> entry in projectiles)
        {
            settings[entry.Key] = new ProjectileArtSettings(entry.Value.Flight, entry.Value.Charge, entry.Value.Impact);
        }

        return settings;
    }

    private static Dictionary<string, IReadOnlyList<HitSparkSettings>> ToSettings(
        Dictionary<string, List<SparkJson>> sparks)
    {
        var settings = new Dictionary<string, IReadOnlyList<HitSparkSettings>>(System.StringComparer.Ordinal);
        foreach (KeyValuePair<string, List<SparkJson>> entry in sparks)
        {
            settings[entry.Key] = entry.Value.ConvertAll(spark => new HitSparkSettings(
                spark.Angle, spark.Offset?.X ?? 0f, spark.Offset?.Y ?? 0f, spark.Style, spark.Scale));
        }

        return settings;
    }

    private static AnimationSettings ToSettings(AnimationJson animation, ContactFrames? contacts) => new(
        animation.Clip,
        contacts,
        animation.File,
        animation.Loop,
        animation.InPlace,
        animation.ClipFrom,
        animation.ClipTo,
        animation.Fit,
        animation.HoldHeight,
        animation.HoldFrames,
        animation.CrossfadeFrames,
        animation.Speed,
        animation.Reverse,
        animation.Keys?.ConvertAll(ToKey));

    private sealed class ArtFileJson
    {
        [JsonRequired]
        public int ArtVersion { get; init; }

        [JsonRequired]
        public FighterArtKind Art { get; init; }

        public string? Model { get; init; }

        public float Scale { get; init; } = 1f;

        public float TurnDegrees { get; init; }

        public List<ModelJson> OtherModels { get; init; } = [];

        public Dictionary<string, AnimationJson> Animations { get; init; } = [];

        public List<TransitionJson> Transitions { get; init; } = [];

        public AnimationJson? KnockOut { get; init; }

        public Dictionary<string, ProjectileJson> Projectiles { get; init; } = [];

        public Dictionary<string, List<SparkJson>> Sparks { get; init; } = [];

        public int CrossfadeFrames { get; init; }

        public int TurnFrames { get; init; }

        public float CameraTurnDegrees { get; init; }

        public int HoldFrames { get; init; } = 1;
    }

    private sealed class ModelJson
    {
        [JsonRequired]
        public string Model { get; init; } = string.Empty;

        public float Scale { get; init; } = 1f;

        public float TurnDegrees { get; init; }
    }

    private sealed class ProjectileJson
    {
        [JsonRequired]
        public string Flight { get; init; } = string.Empty;

        public string? Charge { get; init; }

        public string? Impact { get; init; }
    }

    private sealed class SparkJson
    {
        public float? Angle { get; init; }

        public OffsetJson? Offset { get; init; }

        public HitSparkStyle? Style { get; init; }

        public float Scale { get; init; } = 1f;
    }

    private sealed class OffsetJson
    {
        [JsonRequired]
        public float X { get; init; }

        [JsonRequired]
        public float Y { get; init; }
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

        public int? ClipFrom { get; init; }

        public int? ClipTo { get; init; }

        public bool Fit { get; init; }

        public bool HoldHeight { get; init; }

        public int? HoldFrames { get; init; }

        public int? CrossfadeFrames { get; init; }

        public float Speed { get; init; } = 1f;

        public bool Reverse { get; init; }

        public List<KeyJson>? Keys { get; init; }
    }

    private sealed class KeyJson
    {
        [JsonRequired]
        public MovePhase Phase { get; init; }

        [JsonRequired]
        public int Frame { get; init; }

        [JsonRequired]
        public int Of { get; init; }

        [JsonRequired]
        public int Clip { get; init; }
    }
}
