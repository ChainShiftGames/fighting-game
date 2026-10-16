using System.Collections.Generic;
using Sim.Input;

namespace Scenarios.Tests.Runner;

/// <summary>Each player's input per frame, facing-relative (<see cref="InputNotation"/>); neutral when not given.</summary>
public sealed class ScenarioInputs
{
    private readonly Dictionary<(int Player, int Frame), (InputWord Input, ScenarioLocation Location)> _inputs = [];

    /// <summary>The last frame any input is given for, or 0.</summary>
    public int LastFrame { get; private set; }

    public InputWord Get(int player, int frame) =>
        _inputs.TryGetValue((player, frame), out var entry) ? entry.Input : InputWord.Neutral;

    internal void Set(int player, int frame, InputWord input, ScenarioLocation location)
    {
        if (_inputs.TryGetValue((player, frame), out var existing))
        {
            throw new ScenarioSyntaxException(
                location,
                $"{ScenarioPlayers.Label(player)} already has an input on frame {frame} (line {existing.Location.Line})");
        }

        _inputs[(player, frame)] = (input, location);
        if (frame > LastFrame)
        {
            LastFrame = frame;
        }
    }
}
