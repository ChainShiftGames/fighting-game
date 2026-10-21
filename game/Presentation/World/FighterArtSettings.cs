using System.Collections.Generic;
using FightingGame.Presentation.World.Projectiles;
using FightingGame.Presentation.World.Sparks;

namespace FightingGame.Presentation.World;

/// <summary>A character's art file: what draws it and how its model's clips match its moves (ADR-0028).</summary>
/// <param name="Art">Placeholder or model; a character without an art file is a placeholder.</param>
/// <param name="Model">The <c>.glb</c> (see <see cref="FighterArtFile.Resolve"/>); empty for a placeholder.</param>
/// <param name="Scale">Multiplies the model's size, for a model not authored in metres.</param>
/// <param name="TurnDegrees">Turns the model about Y, for a model not facing +X.</param>
/// <param name="Animations">Per move ID; a move missing here plays the clip named after it, frame for frame.</param>
/// <param name="Transitions">Clips shown when entering a move from certain others; the first that fits is used.</param>
public sealed record FighterArtSettings(
    FighterArtKind Art,
    string Model,
    float Scale,
    float TurnDegrees,
    IReadOnlyDictionary<string, AnimationSettings> Animations,
    IReadOnlyList<ClipTransition> Transitions)
{
    /// <summary>The clip played once a fighter is KO'd, from that frame on; null keeps showing its moves.</summary>
    public AnimationSettings? KnockOut { get; init; }

    /// <summary>Frames a basic move's clip takes to fade in from the pose before; 0 snaps.</summary>
    public int CrossfadeFrames { get; init; }

    /// <summary>
    /// Frames a hit or block reaction takes to fade in when a hit lands mid-reaction (a gatling, a juggle), from the
    /// reaction shown; 0 snaps, as first hits always do.
    /// </summary>
    public int ReHitFadeFrames { get; init; }

    /// <summary>Frames the model takes to turn round when the facing switches; 0 mirrors at once.</summary>
    public int TurnFrames { get; init; }

    /// <summary>Degrees the model turns toward the camera at rest (0 to 90): more shows more face, 0 is side-on.</summary>
    public float CameraTurnDegrees { get; init; }

    /// <summary>How each move's projectiles are drawn, by the move ID that spawns them; a move missing here has no effects.</summary>
    public IReadOnlyDictionary<string, ProjectileArtSettings> Projectiles { get; init; } =
        new Dictionary<string, ProjectileArtSettings>();

    /// <summary>
    /// How each move's hit sparks are drawn, by move ID: one entry per hit, in the move's hit order. A move or hit
    /// missing here gets the sparks its hit data implies (<see cref="HitSparkLooks"/>).
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<HitSparkSettings>> Sparks { get; init; } =
        new Dictionary<string, IReadOnlyList<HitSparkSettings>>();

    /// <summary>Frames each pose is shown for, for every move without its own setting: 1, or 2-3 for animation on 2s or 3s.</summary>
    public int HoldFrames { get; init; } = 1;

    /// <summary>Models to compare with <see cref="Model"/>, playing the same clips; picked with <see cref="WithModel"/>.</summary>
    public IReadOnlyList<FighterModelVariant> OtherModels { get; init; } = [];

    /// <summary>
    /// These settings drawn with model <paramref name="variant"/>: 0 is <see cref="Model"/>, 1 on the other models in
    /// order, counting round past the last.
    /// </summary>
    public FighterArtSettings WithModel(int variant)
    {
        int index = variant % (OtherModels.Count + 1);
        if (index <= 0)
        {
            return this;
        }

        FighterModelVariant other = OtherModels[index - 1];
        return this with { Model = other.Model, Scale = other.Scale, TurnDegrees = other.TurnDegrees };
    }

    public static FighterArtSettings Placeholder { get; } =
        new(FighterArtKind.Placeholder, string.Empty, 1f, 0f, new Dictionary<string, AnimationSettings>(), []);

    /// <summary>The index of the first transition from <paramref name="fromMove"/> into <paramref name="toMove"/>; -1 if none.</summary>
    public int TransitionBetween(string fromMove, string toMove)
    {
        for (int i = 0; i < Transitions.Count; i++)
        {
            if (Transitions[i].Joins(fromMove, toMove))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>How a move is animated: its own entry, or the clip named after it in the model, frame for frame.</summary>
    public AnimationSettings For(string moveId) =>
        Animations.TryGetValue(moveId, out AnimationSettings? settings) ? settings : AnimationSettings.Default;

    /// <summary>Frames a move's clip fades in over: its own setting or the art file's.</summary>
    public int CrossfadeFramesFor(string moveId) => For(moveId).CrossfadeFrames ?? CrossfadeFrames;

    /// <summary>The clip a move plays: its own entry's clip, or the move ID.</summary>
    public string ClipFor(string moveId) => For(moveId).Clip ?? moveId;

    public ContactFrames? ContactsFor(string moveId) => For(moveId).Contacts;

    /// <summary>A move's timing keys, in phase order; empty when it has none.</summary>
    public IReadOnlyList<TimingKey> KeysFor(string moveId) => For(moveId).Keys ?? [];
}
