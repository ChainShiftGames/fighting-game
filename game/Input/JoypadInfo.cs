namespace FightingGame.Input;

/// <summary>A joypad Godot reports as connected.</summary>
/// <param name="Device">Godot's joypad index.</param>
/// <param name="IsSteamInput">A virtual pad Steam Input made from a physical controller.</param>
/// <param name="IgnoredBySteam">
/// A physical controller Steam Input already re-presents as a virtual pad (it is on SDL's ignore list).
/// </param>
public readonly record struct JoypadInfo(int Device, string Name, bool IsSteamInput, bool IgnoredBySteam);
