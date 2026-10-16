namespace Sim.Events;

/// <summary>What a frame event reports; presentation picks its effects (spark, sound, shake) by kind.</summary>
/// <remarks>
/// Gameplay tasks add kinds as they start emitting them. Append with the next number and never renumber or reuse
/// one: events are part of the game state, so desync dumps and tools decode these values. Each kind documents what
/// <see cref="FrameEvent.Fighter"/>, <see cref="FrameEvent.Position"/> and <see cref="FrameEvent.Detail"/> mean
/// for it; the task that first emits a kind may refine that text.
/// </remarks>
public enum FrameEventKind
{
    /// <summary>No event; an empty slot. Never emitted.</summary>
    None = 0,

    /// <summary>
    /// An attack hit. Fighter: the attacker. Position: the centre of the overlap of the first hitbox and hurtbox
    /// that touched. Detail: the move.
    /// </summary>
    Hit = 1,

    /// <summary>An attack was blocked. Fighter: the attacker. Position and Detail: as for <see cref="Hit"/>.</summary>
    Block = 2,

    /// <summary>
    /// An attack became active: plays the swing (whiff) sound. Fighter: the attacker. Position: the fighter's
    /// position. Detail: the move.
    /// </summary>
    Swing = 3,

    /// <summary>A fighter touched the floor after being airborne. Fighter: who. Position: where. Detail: 0.</summary>
    Landing = 4,

    /// <summary>
    /// Both attacks connected on the same frame and cancelled out (<c>TradeRule.Clash</c>); one event per fighter.
    /// Fighter: an attacker. Position: where its hitbox met the other's hurtbox. Detail: its move.
    /// </summary>
    Clash = 5,

    /// <summary>
    /// A fighter's health reached 0 and the round ended; one event per KO'd fighter (two in a double KO). Fighter: the
    /// KO'd fighter. Position: its position. Detail: 0.
    /// </summary>
    KnockOut = 6,

    /// <summary>The round timer ran out and ended the round. Fighter: none. Position: 0. Detail: 0.</summary>
    TimeOut = 7,

    /// <summary>
    /// The fighters were reset and a new round began. Fighter: none. Position: 0. Detail: the round number. Not
    /// emitted for round 1, which begins with the match's start state.
    /// </summary>
    RoundStart = 8,

    /// <summary>
    /// The match has a result (after the last round-over pause). Fighter: none. Position: 0. Detail: the winners'
    /// player bits (<c>Winners.Players</c>; both bits = a draw).
    /// </summary>
    MatchEnd = 9,

    /// <summary>
    /// A projectile appeared (from a move or a module's spawn request; P1-21). Fighter: its owner. Position: where it
    /// appeared. Detail: its index in the owner's character's projectile list.
    /// </summary>
    ProjectileSpawn = 10,

    /// <summary>
    /// A projectile hit the owner's opponent and disappeared. Fighter: its owner. Position: the centre of the overlap
    /// of its first hitbox and the defender's hurtbox that touched. Detail: as for <see cref="ProjectileSpawn"/>.
    /// </summary>
    ProjectileHit = 11,

    /// <summary>
    /// A projectile was blocked and disappeared. Fighter, Position and Detail: as for <see cref="ProjectileHit"/>.
    /// </summary>
    ProjectileBlock = 12,

    /// <summary>
    /// A projectile lost durability against an opposing projectile (it disappears at 0), or was cancelled by the
    /// <c>TradeRule.Clash</c> rule in a same-frame exchange; one event per projectile and frame. Fighter: its owner.
    /// Position: the centre of its first overlap with the other projectile (or the defender). Detail: as for
    /// <see cref="ProjectileSpawn"/>.
    /// </summary>
    ProjectileClash = 13,

    /// <summary>
    /// A projectile's lifetime ran out and it disappeared. Fighter: its owner. Position: its last position. Detail: as
    /// for <see cref="ProjectileSpawn"/>.
    /// </summary>
    ProjectileExpire = 14,
}
