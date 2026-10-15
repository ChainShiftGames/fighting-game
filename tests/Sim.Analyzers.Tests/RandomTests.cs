using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Sim.Analyzers.Tests;

public sealed class RandomTests
{
    [Theory]
    [InlineData("result = new System.Random(1).Next();", "Random")]
    [InlineData("result = System.Random.Shared.Next();", "Random")]
    [InlineData("result = System.Security.Cryptography.RandomNumberGenerator.GetInt32(6);", "Cryptography")]
    [InlineData("result = System.Guid.NewGuid().GetHashCode();", "NewGuid")]
    [InlineData("result = System.HashCode.Combine(1, 2);", "HashCode")]
    [InlineData("result = \"5A\".GetHashCode();", "GetHashCode")]
    public async Task RandomnessIsReported(string statements, string bannedSymbol)
    {
        ImmutableArray<Diagnostic> diagnostics = await SimAnalysis.AnalyzeAsync(Snippet.InMethod(statements));

        Assert.Contains(
            diagnostics,
            d => d.Id == SimAnalysis.BannedApiId && d.GetMessage().Contains(bannedSymbol));
    }
}
