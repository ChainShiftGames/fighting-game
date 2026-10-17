using System.Globalization;

namespace FightingGame.Training;

/// <summary>The frame meter's numbers as the bar shows them, e.g. "Startup 5   Advantage +2".</summary>
public static class FrameMeterText
{
    public static string Describe(FrameMeter meter) =>
        $"Startup {Number(meter.Startup)}   Advantage {Signed(meter.Advantage)}";

    private static string Number(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "-";

    private static string Signed(int? value) => value switch
    {
        null => "-",
        > 0 => $"+{value.Value.ToString(CultureInfo.InvariantCulture)}",
        _ => value.Value.ToString(CultureInfo.InvariantCulture),
    };
}
