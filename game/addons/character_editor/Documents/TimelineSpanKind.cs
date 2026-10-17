namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>What a stretch of frames on the move timeline is; the timeline gives each its own colour.</summary>
public enum TimelineSpanKind
{
    Startup,
    Active,
    Recovery,

    /// <summary>A basic move's loop.</summary>
    Loop,
    Invulnerable,
    HurtBox,
    HitBox,
    PushBox,
    ThrowBox,

    /// <summary>A box whose kind the editor doesn't know (validation reports it).</summary>
    OtherBox,
    Cancel,
    Velocity,
    ProjectileSpawn,
}
