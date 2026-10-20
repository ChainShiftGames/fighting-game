namespace FightingGame.Addons.ControlPanel.Documents;

/// <summary>What kind of JSON value a control panel setting edits, and so which control it gets.</summary>
public enum SettingKind
{
    /// <summary>A whole number (game data is all integers).</summary>
    Whole,

    /// <summary>A number with a fractional part (presentation settings).</summary>
    Decimal,

    /// <summary>true or false.</summary>
    Flag,

    /// <summary>One of a fixed list of names.</summary>
    Choice,
}
