namespace FightingGame.Input.Profiles;

/// <summary>A layout a player can load onto a device ("Pad: classic", "Leverless"); its kind is the profile's.</summary>
/// <param name="NameKey">The translation key of its name in the menu.</param>
public sealed record ControlPreset(string Id, string NameKey, BindingProfile Profile);
