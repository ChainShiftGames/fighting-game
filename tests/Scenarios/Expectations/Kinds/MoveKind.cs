using System;
using System.Globalization;
using Scenarios.Tests.Runner;
using Sim.Characters;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>p1 move</c>: the id of the move playing, from the character's data (<c>5a</c>, <c>idle</c>…).</summary>
public sealed class MoveKind : ValueKind
{
    public override string Keyword => "move";

    public override bool IsPerFighter => true;

    public override bool IsOrdered => false;

    public override int Read(ScenarioFrame frame, int player) => frame.After.Fighters[player].Move;

    public override int ParseValue(string text, ScenarioSetup setup, int player)
    {
        CharacterData character = setup.Character(player);
        ReadOnlySpan<string> moveIds = character.MoveIds;
        for (int move = 0; move < moveIds.Length; move++)
        {
            if (moveIds[move] == text)
            {
                return move;
            }
        }

        throw new FormatException(
            $"{character.Id} has no move '{text}'; moves: {string.Join(", ", moveIds.ToArray())}");
    }

    public override string FormatValue(int value, ScenarioSetup setup, int player)
    {
        ReadOnlySpan<string> moveIds = setup.Character(player).MoveIds;
        return value >= 0 && value < moveIds.Length ? moveIds[value] : value.ToString(CultureInfo.InvariantCulture);
    }
}
