using System;
using Scenarios.Tests.Runner;
using Sim.Input;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>p1 input</c>: the input the logic recorded this frame (after SOCD), in facing-relative notation.</summary>
/// <remarks>Relative to the facing at the start of the frame, the same facing the runner used to send it.</remarks>
public sealed class InputKind : ValueKind
{
    public override string Keyword => "input";

    public override bool IsPerFighter => true;

    public override bool IsOrdered => false;

    public override int Read(ScenarioFrame frame, int player)
    {
        InputWord recorded = frame.After.Inputs[player].FramesAgo(0);
        return InputNotation.ForFacing(recorded, frame.Before.Fighters[player].Facing).Bits;
    }

    public override int ParseValue(string text, ScenarioSetup setup, int player) => InputNotation.Parse(text).Bits;

    public override string FormatValue(int value, ScenarioSetup setup, int player) =>
        InputNotation.Format(new InputWord(checked((ushort)value)));
}
