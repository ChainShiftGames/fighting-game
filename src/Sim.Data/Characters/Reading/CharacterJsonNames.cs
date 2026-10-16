using Sim.Characters;
using Sim.Data.Validation;

namespace Sim.Data.Characters.Reading;

/// <summary>JSON spellings of the character data enums; data/schema/*.schema.json lists the same names.</summary>
public static class CharacterJsonNames
{
    public static readonly JsonNames<MoveCategory> Categories = new(
        ("basic", MoveCategory.Basic),
        ("normal", MoveCategory.Normal),
        ("special", MoveCategory.Special),
        ("super", MoveCategory.Super),
        ("throw", MoveCategory.Throw));

    /// <summary>Categories a cancel can target: everything except basic moves.</summary>
    public static readonly JsonNames<MoveCategory> CancelCategories = new(
        ("normal", MoveCategory.Normal),
        ("special", MoveCategory.Special),
        ("super", MoveCategory.Super),
        ("throw", MoveCategory.Throw));

    public static readonly JsonNames<MoveStance> Stances = new(
        ("ground", MoveStance.Ground),
        ("air", MoveStance.Air));

    public static readonly JsonNames<BoxKind> BoxKinds = new(
        ("hurt", BoxKind.Hurt),
        ("hit", BoxKind.Hit),
        ("push", BoxKind.Push),
        ("throw", BoxKind.Throw));

    public static readonly JsonNames<Guard> Guards = new(
        ("mid", Guard.Mid),
        ("low", Guard.Low),
        ("overhead", Guard.Overhead),
        ("unblockable", Guard.Unblockable));

    public static readonly JsonNames<HitReaction> Reactions = new(
        ("hitstun", HitReaction.Hitstun),
        ("knockdown", HitReaction.Knockdown));

    public static readonly JsonNames<CancelOn> CancelOutcomes = new(
        ("hit", CancelOn.Hit),
        ("block", CancelOn.Block),
        ("whiff", CancelOn.Whiff));

    public static readonly JsonNames<DashStyle> DashStyles = new(
        ("run", DashStyle.Run),
        ("step", DashStyle.Step));

    /// <summary>Keys of the character file's <c>states</c> object, one per <see cref="BasicState"/>.</summary>
    public static readonly (string Name, BasicState State)[] BasicStates =
    [
        ("idle", BasicState.Idle),
        ("crouch", BasicState.Crouch),
        ("walkForward", BasicState.WalkForward),
        ("walkBack", BasicState.WalkBack),
        ("prejump", BasicState.Prejump),
        ("jump", BasicState.Jump),
        ("landing", BasicState.Landing),
        ("dash", BasicState.Dash),
        ("backdash", BasicState.Backdash),
        ("hitstun", BasicState.Hitstun),
        ("crouchHitstun", BasicState.CrouchHitstun),
        ("airHitstun", BasicState.AirHitstun),
        ("blockstun", BasicState.Blockstun),
        ("crouchBlockstun", BasicState.CrouchBlockstun),
        ("knockdown", BasicState.Knockdown),
        ("wakeUp", BasicState.WakeUp),
    ];
}
