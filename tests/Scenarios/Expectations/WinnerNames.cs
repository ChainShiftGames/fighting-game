using System;
using System.Globalization;
using Sim.State;

namespace Scenarios.Tests.Expectations;

/// <summary>How scenarios write a <see cref="Winners"/> value: <c>none</c>, <c>p1</c>, <c>p2</c> or <c>draw</c>.</summary>
public static class WinnerNames
{
    /// <exception cref="FormatException">Not one of the names; the message lists them.</exception>
    public static int Parse(string text) => text switch
    {
        "none" => Winners.None.Players,
        "p1" => Winners.Only(0).Players,
        "p2" => Winners.Only(1).Players,
        "draw" => Winners.Both.Players,
        _ => throw new FormatException($"'{text}' isn't a winner; use none, p1, p2 or draw"),
    };

    // A value with other bits set is a bug in the logic; show the raw number rather than hiding it.
    public static string Format(int players) => players switch
    {
        0 => "none",
        1 => "p1",
        2 => "p2",
        3 => "draw",
        _ => players.ToString(CultureInfo.InvariantCulture),
    };
}
