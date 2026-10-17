namespace Sim.Modules.RomanCancel;

/// <summary>The Roman Cancel module's settings in its <c>MatchRules</c> slot (developer settings).</summary>
public struct RomanCancelConfig : IModuleConfig
{
    /// <summary>Input word bits of the buttons that make up the input (e.g. A, B and C: bits 4–6 = 112).</summary>
    public int ButtonMask;

    /// <summary>How many of those buttons must be pressed together (3: any three of them).</summary>
    public int ButtonCount;

    /// <summary>Frames between the first and the last of those presses for them to count as together.</summary>
    public int PressWindow;

    /// <summary>Meter a Roman Cancel costs, in hundredths of a percent (5000 = 50 %); 0 is free, also without meter.</summary>
    public int MeterCost;

    /// <summary>Which variants can be used: 1 red, 2 purple, 4 blue, 8 yellow (<see cref="RomanCancelVariants"/>).</summary>
    public int Variants;

    /// <summary>Match frames the whole match freezes; more than the button buffer, so the input starts no move.</summary>
    public int FreezeFrames;

    /// <summary>Match frames the opponent runs slowed after a red, purple or blue Roman Cancel; 0 for none.</summary>
    public int SlowdownFrames;

    /// <summary>The opponent advances this many frames…</summary>
    public int SlowdownNumerator;

    /// <summary>…per this many match frames while slowed (1/2 = half speed).</summary>
    public int SlowdownDenominator;
}
