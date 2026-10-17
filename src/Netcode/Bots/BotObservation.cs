using System;

namespace Netcode.Bots;

/// <summary>What a bot sees of the match on one frame: the narrow, player-visible view it decides from.</summary>
/// <param name="IsFighting">The round is on and players have control (no round-over or match-over pause).</param>
/// <param name="Self">The bot's own fighter.</param>
/// <param name="Opponent">The other fighter.</param>
/// <param name="IncomingProjectileDistance">
/// Horizontal subpixels to the nearest opposing projectile flying towards <see cref="Self"/>, or
/// <see cref="NoProjectile"/>.
/// </param>
public readonly record struct BotObservation(
    bool IsFighting,
    FighterView Self,
    FighterView Opponent,
    int IncomingProjectileDistance)
{
    /// <summary>No opposing projectile is flying towards the bot.</summary>
    public const int NoProjectile = int.MaxValue;

    /// <summary>Horizontal subpixels between the two fighters' origins.</summary>
    public int Distance => Math.Abs(Opponent.X - Self.X);

    /// <summary>+1 when the opponent is to the right, -1 to the left; at the same x, the way the bot faces.</summary>
    public int OpponentSide => Opponent.X == Self.X ? Self.Facing : Math.Sign(Opponent.X - Self.X);
}
