using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Sim.Analyzers.Tests;

public sealed class ThreadingTests
{
    [Theory]
    [InlineData("System.Threading.Tasks.Task.Run(() => { });")]
    [InlineData("System.Threading.Tasks.ValueTask done = System.Threading.Tasks.ValueTask.CompletedTask;")]
    [InlineData("System.Threading.Tasks.Parallel.For(0, 2, i => { });")]
    [InlineData("new System.Threading.Thread(() => { }).Start();")]
    [InlineData("System.Threading.Interlocked.Increment(ref result);")]
    [InlineData("var map = new System.Collections.Concurrent.ConcurrentDictionary<int, int>();")]
    public async Task ThreadingApiIsReported(string statements)
    {
        ImmutableArray<Diagnostic> diagnostics = await SimAnalysis.AnalyzeAsync(Snippet.InMethod(statements));

        Assert.Contains(diagnostics, d => d.Id == SimAnalysis.BannedApiId);
    }
}
