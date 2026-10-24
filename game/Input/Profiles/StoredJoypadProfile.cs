namespace FightingGame.Input.Profiles;

/// <summary>One joypad's own layout, kept by its id (ADR-0038 §2); the name only makes the file readable.</summary>
public sealed record StoredJoypadProfile(InputDeviceId Device, string Name, BindingProfile Profile);
