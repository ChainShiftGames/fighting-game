namespace FightingGame.Input.Profiles;

/// <summary>
/// The menu inputs a profile can rebind: confirm and back. Directions, tabs and start keep the fixed menu mapping,
/// and holding the default back control always leaves a screen (<c>MenuBindings</c>), so nobody locks themselves out.
/// </summary>
public static class MenuInputs
{
    public const string Confirm = "confirm";

    public const string Back = "back";

    public static string[] All { get; } = [Confirm, Back];
}
