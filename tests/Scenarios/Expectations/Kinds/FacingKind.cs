using System;
using System.Globalization;
using Scenarios.Tests.Runner;
using Sim.State;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>p1 facing</c>: <c>left</c> or <c>right</c>.</summary>
public sealed class FacingKind : ValueKind, ISettableKind
{
    private const int Right = 1;
    private const int Left = -1;

    public override string Keyword => "facing";

    public override bool IsPerFighter => true;

    public override bool IsOrdered => false;

    public override int Read(ScenarioFrame frame, int player) => frame.After.Fighters[player].Facing;

    public override int ParseValue(string text, ScenarioSetup setup, int player) => text switch
    {
        "right" => Right,
        "left" => Left,
        _ => throw new FormatException($"'{text}' isn't a facing; use left or right"),
    };

    // A value other than ±1 is a bug in the logic; show the raw number rather than hiding it.
    public override string FormatValue(int value, ScenarioSetup setup, int player) => value switch
    {
        Right => "right",
        Left => "left",
        _ => value.ToString(CultureInfo.InvariantCulture),
    };

    public void Set(ref MatchState state, int player, int value) => state.Fighters[player].Facing = value;
}
