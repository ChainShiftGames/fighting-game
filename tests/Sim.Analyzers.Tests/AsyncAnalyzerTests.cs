using System.Collections.Immutable;
using System.Threading.Tasks;
using Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Sim.Analyzers.Tests;

public sealed class AsyncAnalyzerTests
{
    [Theory]
    [InlineData("public async void Fire() { }")]
    [InlineData("public void Fire() { System.Action a = async () => { }; }")]
    [InlineData("public void Fire() { System.Action a = async delegate { }; }")]
    [InlineData("public void Fire() { Local(); async void Local() { } }")]
    public async Task AsyncIsReported(string members)
    {
        ImmutableArray<Diagnostic> diagnostics = await SimAnalysis.AnalyzeAsync(Snippet.InClass(members));

        Assert.Contains(diagnostics, d => d.Id == AsyncAnalyzer.DiagnosticId);
    }
}
