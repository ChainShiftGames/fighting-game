namespace FightingGame.VfxLab.Files;

/// <summary>A part as a sheet writes it: every field optional, so a variant can change one thing of the part it extends.</summary>
public sealed record LabPartDraft
{
    // Developer settings (presentation): what a part gets when neither it nor what it extends says.
    private const float DefaultCardSize = 2.6f;
    private const float DefaultGrowFrames = 12f;
    public const float DefaultStartScale = 0.2f;
    private const float DefaultModelSize = 0.6f;

    public string? Shader { get; init; }

    /// <summary>Width and height in metres.</summary>
    public float[]? Card { get; init; }

    /// <summary>The card's centre from the effect's point, in metres (+X the way it flies).</summary>
    public float[]? Centre { get; init; }

    public float? Spin { get; init; }

    public float? GrowFrames { get; init; }

    public float? StartScale { get; init; }

    /// <summary>A <c>res://</c> path to a model drawn in front of the card; see <see cref="LabPart.Model"/>.</summary>
    public string? Model { get; init; }

    public float? ModelSize { get; init; }

    public float? ModelRoll { get; init; }

    /// <summary>This draft's fields, with <paramref name="below"/>'s where this one has none.</summary>
    public LabPartDraft Over(LabPartDraft? below) => below is null ? this : new LabPartDraft
    {
        Shader = Shader ?? below.Shader,
        Card = Card ?? below.Card,
        Centre = Centre ?? below.Centre,
        Spin = Spin ?? below.Spin,
        GrowFrames = GrowFrames ?? below.GrowFrames,
        StartScale = StartScale ?? below.StartScale,
        Model = Model ?? below.Model,
        ModelSize = ModelSize ?? below.ModelSize,
        ModelRoll = ModelRoll ?? below.ModelRoll,
    };

    /// <summary>The finished part, or null when no shader was named.</summary>
    public LabPart? ToPart() => Shader is null ? null : new LabPart(
        Shader,
        Card is [float width, ..] ? width : DefaultCardSize,
        Card is [_, float height, ..] ? height : DefaultCardSize,
        Centre is [float x, ..] ? x : 0f,
        Centre is [_, float y, ..] ? y : 0f,
        Spin ?? 0f,
        GrowFrames ?? DefaultGrowFrames,
        StartScale ?? DefaultStartScale,
        Model,
        ModelSize ?? DefaultModelSize,
        ModelRoll ?? 0f);
}
