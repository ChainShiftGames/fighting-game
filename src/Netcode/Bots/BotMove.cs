using Sim.Characters;

namespace Netcode.Bots;

/// <summary>One attack a bot can do, as a player knows it from playing the character.</summary>
/// <param name="Category">Normal, special, super or throw.</param>
/// <param name="Motion">Numpad notation read as a number (236, 5…), from the move's data.</param>
/// <param name="ButtonMask">The input word bits of the move's button(s).</param>
/// <param name="Stance">Started on the ground or in the air.</param>
/// <param name="StartupFrames">Frames until it can first hit: how fast it is.</param>
/// <param name="Reach">
/// Horizontal subpixels between the two fighters' origins within which it can connect; <see cref="FullScreen"/> for
/// projectiles.
/// </param>
public readonly record struct BotMove(
    MoveCategory Category,
    int Motion,
    int ButtonMask,
    MoveStance Stance,
    int StartupFrames,
    int Reach)
{
    /// <summary>The reach of a projectile: it travels, so any distance will do.</summary>
    public const int FullScreen = int.MaxValue;

    public bool IsProjectile => Reach == FullScreen;
}
