using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Sim.Analyzers.Tests;

public sealed class ClockTests
{
    [Theory]
    [InlineData("result = System.DateTime.Now.Second;", "DateTime")]
    [InlineData("result = System.DateTime.UtcNow.Millisecond;", "DateTime")]
    [InlineData("result = System.DateTimeOffset.Now.Second;", "DateTimeOffset")]
    [InlineData("result = (int)System.Diagnostics.Stopwatch.GetTimestamp();", "Stopwatch")]
    [InlineData("result = System.Environment.TickCount;", "Environment")]
    [InlineData("result = (int)System.TimeProvider.System.GetTimestamp();", "TimeProvider")]
    public async Task ClockIsReported(string statements, string bannedSymbol)
    {
        ImmutableArray<Diagnostic> diagnostics = await SimAnalysis.AnalyzeAsync(Snippet.InMethod(statements));

        Assert.Contains(
            diagnostics,
            d => d.Id == SimAnalysis.BannedApiId && d.GetMessage().Contains(bannedSymbol));
    }
}
