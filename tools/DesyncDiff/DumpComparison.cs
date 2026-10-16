using System.Collections.Generic;
using System.Linq;
using Netcode.Desync;

namespace DesyncDiff;

/// <summary>Compares two desync dumps: header, every state both hold field by field, and the inputs.</summary>
public static class DumpComparison
{
    public static DumpDiff Compare(DesyncDump first, DesyncDump second)
    {
        Dictionary<int, DumpedState> firstStates = StatesByFrame(first);
        Dictionary<int, DumpedState> secondStates = StatesByFrame(second);

        List<StateDifference> states = firstStates.Keys.Intersect(secondStates.Keys)
            .Order()
            .Select(frame => new StateDifference(frame, CompareFields(firstStates[frame], secondStates[frame])))
            .ToList();
        List<int> oneDumpOnly = firstStates.Keys.Union(secondStates.Keys)
            .Except(states.Select(state => state.Frame))
            .Order()
            .ToList();

        Dictionary<int, DumpedInputs> secondInputs = second.Inputs.ToDictionary(inputs => inputs.Frame);
        List<DumpedInputs> sharedInputs = first.Inputs.Where(inputs => secondInputs.ContainsKey(inputs.Frame)).ToList();
        List<InputDifference> inputs = sharedInputs
            .Where(inputs => inputs != secondInputs[inputs.Frame])
            .Select(inputs => new InputDifference(inputs, secondInputs[inputs.Frame]))
            .ToList();

        return new DumpDiff(Warnings(first, second), states, oneDumpOnly, inputs, sharedInputs.Count);
    }

    private static List<string> Warnings(DesyncDump first, DesyncDump second)
    {
        var warnings = new List<string>();
        if (first.GameVersion != second.GameVersion)
        {
            warnings.Add($"Game versions differ: {first.GameVersion} vs {second.GameVersion}.");
        }

        if (first.DataHash != second.DataHash)
        {
            warnings.Add($"Data hashes differ: {first.DataHash} vs {second.DataHash}.");
        }

        if (first.LocalPlayer == second.LocalPlayer)
        {
            warnings.Add($"Both dumps come from P{first.LocalPlayer}: they are not the two sides of one desync.");
        }

        if (first.FirstMismatchFrame != second.FirstMismatchFrame)
        {
            warnings.Add(
                $"The PCs found the desync at different frames: {first.FirstMismatchFrame} vs {second.FirstMismatchFrame}.");
        }

        return warnings;
    }

    // Layout order of the first dump, then any field only the second has (a different state layout).
    private static List<FieldDifference> CompareFields(DumpedState first, DumpedState second)
    {
        var differences = new List<FieldDifference>();
        foreach ((string field, long value) in first.Fields)
        {
            if (!second.Fields.TryGetValue(field, out long other))
            {
                differences.Add(new FieldDifference(field, value, null));
            }
            else if (value != other)
            {
                differences.Add(new FieldDifference(field, value, other));
            }
        }

        foreach ((string field, long value) in second.Fields)
        {
            if (!first.Fields.ContainsKey(field))
            {
                differences.Add(new FieldDifference(field, null, value));
            }
        }

        return differences;
    }

    private static Dictionary<int, DumpedState> StatesByFrame(DesyncDump dump)
    {
        var states = new Dictionary<int, DumpedState>();
        foreach (DumpedState? state in new[] { dump.LastAgreedState, dump.FirstMismatchState })
        {
            if (state is not null)
            {
                states[state.Frame] = state;
            }
        }

        return states;
    }
}
