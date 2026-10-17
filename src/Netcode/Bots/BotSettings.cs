using System;

namespace Netcode.Bots;

/// <summary>How a CPU bot plays: difficulty is <see cref="ReactionDelayFrames"/>, the rest is its style.</summary>
/// <remarks>
/// Chances are percentages rolled on the bot's own seeded random generator, so the same seed and the same
/// observations always give the same inputs. None of this changes game logic: it only decides which input words the
/// bot gives, as a player's skill would (ARCHITECTURE.md §6).
/// </remarks>
public sealed record BotSettings
{
    /// <summary>The slowest reaction a bot can have: one second.</summary>
    public const int MaxReactionDelayFrames = 60;

    /// <summary>Roughly an average human's reaction time (about 250 ms).</summary>
    public const int DefaultReactionDelayFrames = 15;

    /// <summary>
    /// Frames between something happening on screen and the bot acting on it: the difficulty. A <b>player setting</b>
    /// in versus CPU, a <b>developer setting</b> for soak tests. 0 reacts on the next frame.
    /// </summary>
    public int ReactionDelayFrames { get; init; } = DefaultReactionDelayFrames;

    /// <summary>Chance to block an attack or projectile it sees coming (developer setting).</summary>
    public int BlockChancePercent { get; init; } = 80;

    /// <summary>Chance per frame, within reach, to start an attack (developer setting).</summary>
    public int AttackChancePercent { get; init; } = 15;

    /// <summary>Chance per frame, while approaching, to dash in (developer setting).</summary>
    public int DashChancePercent { get; init; } = 2;

    /// <summary>Chance per frame, while approaching, to jump in (developer setting).</summary>
    public int JumpChancePercent { get; init; } = 1;

    /// <summary>Chance per frame, while approaching, to throw a projectile if it has one (developer setting).</summary>
    public int ProjectileChancePercent { get; init; } = 1;

    /// <exception cref="ArgumentOutOfRangeException">A value is outside its range.</exception>
    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ReactionDelayFrames, nameof(ReactionDelayFrames));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            ReactionDelayFrames, MaxReactionDelayFrames, nameof(ReactionDelayFrames));
        EnsurePercent(BlockChancePercent, nameof(BlockChancePercent));
        EnsurePercent(AttackChancePercent, nameof(AttackChancePercent));
        EnsurePercent(DashChancePercent, nameof(DashChancePercent));
        EnsurePercent(JumpChancePercent, nameof(JumpChancePercent));
        EnsurePercent(ProjectileChancePercent, nameof(ProjectileChancePercent));
    }

    private static void EnsurePercent(int value, string name)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value, name);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, name);
    }
}
