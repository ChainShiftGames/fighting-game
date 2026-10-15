using System.Collections.Generic;
using System.Globalization;

namespace Benchmarks;

/// <summary>The CI gates of ADR-0003: no measured operation allocates, and the tick stays under a time limit.</summary>
internal static class BudgetCheck
{
    public static List<string> FindViolations(
        IReadOnlyList<OperationResult> results,
        OperationResult tick,
        double? maxTickMicroseconds)
    {
        var violations = new List<string>();
        foreach (OperationResult result in results)
        {
            if (result.AllocatedBytes > 0)
            {
                violations.Add($"{result.Name} allocated {result.AllocatedBytes} bytes over {result.Calls} calls.");
            }
        }

        if (tick.P99Microseconds > maxTickMicroseconds)
        {
            violations.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"{tick.Name} p99 is {tick.P99Microseconds:F4} µs, over the {maxTickMicroseconds} µs limit."));
        }

        return violations;
    }
}
