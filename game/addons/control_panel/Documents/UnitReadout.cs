using System.Globalization;
using Sim.Math;

namespace FightingGame.Addons.ControlPanel.Documents;

/// <summary>Shows a stored number in human terms: frames as seconds, subpixels as pixels, permille as a percentage.</summary>
public static class UnitReadout
{
    private const double FramesPerSecond = 60.0;

    // Below a second, frames are the clearer unit, and they are what the box already shows.
    private const double SecondsFromFrames = FramesPerSecond;

    /// <returns>The readout, e.g. <c>1.65 s</c>; empty for <see cref="SettingUnit.None"/> and under a second of frames.</returns>
    public static string Of(SettingUnit unit, double value) => unit switch
    {
        SettingUnit.Frames when value >= SecondsFromFrames => $"{Text(value / FramesPerSecond, "0.##")} s",
        SettingUnit.Subpixels => $"{Text(value / Subpixels.PerPixel, "0.#")} px",
        SettingUnit.SubpixelsPerFrame => $"{Text(value / Subpixels.PerPixel, "0.##")} px/f",
        SettingUnit.Permille => $"{Text(value / 10.0, "0.#")} %",
        _ => string.Empty,
    };

    private static string Text(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);
}
