using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Sim.Analyzers.Tests;

public sealed class IoTests
{
    [Theory]
    [InlineData("result = System.IO.File.ReadAllText(\"moves.json\").Length;", "System.IO")]
    [InlineData("System.Console.WriteLine(result);", "Console")]
    [InlineData("var client = new System.Net.Http.HttpClient();", "System.Net")]
    [InlineData("result = System.Diagnostics.Process.GetCurrentProcess().Id;", "Process")]
    public async Task IoIsReported(string statements, string bannedSymbol)
    {
        ImmutableArray<Diagnostic> diagnostics = await SimAnalysis.AnalyzeAsync(Snippet.InMethod(statements));

        Assert.Contains(
            diagnostics,
            d => d.Id == SimAnalysis.BannedApiId && d.GetMessage().Contains(bannedSymbol));
    }
}
