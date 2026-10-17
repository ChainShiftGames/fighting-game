using System.Globalization;
using Netcode.Online;

namespace FightingGame.UI;

/// <summary>The options menu's input delay entry: Auto, then each fixed delay a player may choose (ADR-0005 §4).</summary>
public static class InputDelayOption
{
    /// <summary>The choice after <paramref name="frames"/>: Auto → 0 → … → 4 → Auto.</summary>
    public static int? Next(int? frames) => frames switch
    {
        null => InputDelaySettings.MinOverrideFrames,
        >= InputDelaySettings.MaxOverrideFrames => null,
        _ => frames + 1,
    };

    public static string Label(int? frames) =>
        frames is { } fixedFrames ? fixedFrames.ToString(CultureInfo.InvariantCulture) : "Auto";
}
