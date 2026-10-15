using System;
using System.Collections.Generic;
using System.Linq;

namespace Architecture.Tests;

/// <summary>The solid arrows of ARCHITECTURE.md §2 between the runtime assemblies; change both together.</summary>
internal static class RuntimeDependencyGraph
{
    private static readonly IReadOnlyDictionary<string, string[]> _directDependencies =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Sim"] = [],
            ["Sim.Data"] = ["Sim"],
            ["Netcode"] = ["Sim"],
            ["Platform.Steam"] = ["Netcode"],
            ["FightingGame"] = ["Netcode", "Platform.Steam", "Sim.Data"],
        };

    public static IEnumerable<string> Assemblies => _directDependencies.Keys.Order(StringComparer.Ordinal);

    /// <summary>
    /// True when <paramref name="to"/> is reachable from <paramref name="from"/> along the arrows. Reachable, not
    /// only adjacent: the game draws Sim state it gets through Netcode, so its compiled code names Sim types. What
    /// the graph forbids is an arrow pointing the wrong way (e.g. Sim → Netcode) or between unrelated nodes.
    /// </summary>
    public static bool CanDependOn(string from, string to)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<string>(_directDependencies[from]);
        while (pending.Count > 0)
        {
            string next = pending.Pop();
            if (next == to)
            {
                return true;
            }

            if (visited.Add(next))
            {
                foreach (string dependency in _directDependencies[next])
                {
                    pending.Push(dependency);
                }
            }
        }

        return false;
    }

    public static bool Contains(string assemblyName) => _directDependencies.ContainsKey(assemblyName);

    public static IEnumerable<string> AllowedDependenciesOf(string from) =>
        Assemblies.Where(to => CanDependOn(from, to));
}
