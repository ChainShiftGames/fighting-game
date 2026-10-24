namespace FightingGame.Input.Profiles;

/// <summary>What kind of physical control a binding names.</summary>
public enum BoundControlKind
{
    /// <summary>A keyboard key, by physical position.</summary>
    Key,

    /// <summary>A joypad button, d-pad included.</summary>
    PadButton,

    /// <summary>An analog trigger used as a button.</summary>
    PadTrigger,
}
