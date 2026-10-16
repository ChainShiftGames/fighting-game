using System;
using System.Collections.Generic;
using System.Linq;

namespace Scenarios.Tests.Expectations;

/// <summary>Every <see cref="IExpectationKind"/> in this assembly, found by reflection and sorted by keyword.</summary>
/// <remarks>
/// Reflection rather than a list so that tasks adding kinds in parallel never edit the same file. A duplicate
/// keyword fails every scenario test, naming both kinds.
/// </remarks>
public static class ExpectationKinds
{
    public static IReadOnlyList<IExpectationKind> All { get; } = Discover();

    /// <summary>The integer kinds, in keyword order; the failure report lists their actual values.</summary>
    public static IReadOnlyList<ValueKind> Values { get; } = All.OfType<ValueKind>().ToList();

    public static bool TryFind(string keyword, out IExpectationKind? kind)
    {
        kind = All.FirstOrDefault(candidate => candidate.Keyword == keyword);
        return kind is not null;
    }

    private static List<IExpectationKind> Discover()
    {
        List<IExpectationKind> kinds = typeof(IExpectationKind).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(IExpectationKind).IsAssignableFrom(type))
            .Select(type => (IExpectationKind)Activator.CreateInstance(type)!)
            .OrderBy(kind => kind.Keyword, StringComparer.Ordinal)
            .ToList();

        for (int i = 1; i < kinds.Count; i++)
        {
            if (kinds[i].Keyword == kinds[i - 1].Keyword)
            {
                throw new InvalidOperationException(
                    $"{kinds[i - 1].GetType().Name} and {kinds[i].GetType().Name} share the keyword '{kinds[i].Keyword}'.");
            }
        }

        return kinds;
    }
}
