using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Sim.Analyzers.Tests;

public sealed class LinqTests
{
    [Theory]
    [InlineData("result = a.Count();")]
    [InlineData("result = a.Where(v => v > 1).Sum();")]
    [InlineData("result = (from v in a where v > 1 select v).First();")]
    [InlineData("result = Enumerable.Range(0, 3).Max();")]
    public async Task LinqIsReported(string statement)
    {
        string source = $$"""
            using System.Linq;

            namespace Sim;

            public static class Probe
            {
                public static int Run(int[] a)
                {
                    int result;
                    {{statement}}
                    return result;
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await SimAnalysis.AnalyzeAsync(source);

        Assert.Contains(diagnostics, d => d.Id == SimAnalysis.BannedApiId && d.GetMessage().Contains("System.Linq"));
    }
}
