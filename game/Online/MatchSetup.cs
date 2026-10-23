namespace FightingGame.Online;

/// <summary>
/// What the menus picked for a match on this PC, filled in screen by screen and handed to
/// <see cref="MatchLauncher.Start"/> at the end. A rematch plays the same setup again.
/// </summary>
/// <remarks>Devices travel through <c>PlayerDevicesChoice</c> (ADR-0038); stages join once there are any (UI-06).</remarks>
public sealed record MatchSetup(LaunchMode Mode, FighterPick Player1, FighterPick Player2)
{
    /// <summary>A setup for the mode with nothing picked yet: the default lineup.</summary>
    public static MatchSetup For(LaunchMode mode) => new(mode, FighterPick.Default, FighterPick.Default);
}
