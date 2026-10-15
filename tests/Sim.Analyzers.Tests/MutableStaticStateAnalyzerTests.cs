using System.Collections.Immutable;
using System.Threading.Tasks;
using Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Sim.Analyzers.Tests;

public sealed class MutableStaticStateAnalyzerTests
{
    [Theory]
    [InlineData("public static int FrameCount;")]
    [InlineData("public static int FrameCount { get; set; }")]
    [InlineData("public static event System.Action? Hit;")]
    public async Task MutableStaticIsReported(string members)
    {
        ImmutableArray<Diagnostic> diagnostics = await SimAnalysis.AnalyzeAsync(Snippet.InClass(members));

        Assert.Contains(diagnostics, d => d.Id == MutableStaticStateAnalyzer.DiagnosticId);
    }

    [Theory]
    [InlineData("public const int MaxRollbackFrames = 8;")]
    [InlineData("public static readonly int[] SineTable = [0, 17, 34];")]
    [InlineData("public static int MaxHealth { get; } = 10000;")]
    [InlineData("public int FrameCount { get; set; }")]
    public async Task ImmutableStaticAndInstanceStateAreAllowed(string members)
    {
        ImmutableArray<Diagnostic> diagnostics = await SimAnalysis.AnalyzeAsync(Snippet.InClass(members));

        Assert.Empty(diagnostics);
    }
}
