using System.Collections.Immutable;
using System.Threading.Tasks;
using Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Sim.Analyzers.Tests;

public sealed class UnorderedIterationAnalyzerTests
{
    private const string Collections = """
        var dictionary = new System.Collections.Generic.Dictionary<int, int> { [1] = 2 };
        var set = new System.Collections.Generic.HashSet<int> { 1 };
        System.Collections.Generic.IReadOnlyDictionary<int, int> readOnly = dictionary;
        """;

    [Theory]
    [InlineData("foreach (var pair in dictionary) { result += pair.Value; }")]
    [InlineData("foreach (int key in dictionary.Keys) { result += key; }")]
    [InlineData("foreach (int value in dictionary.Values) { result += value; }")]
    [InlineData("foreach (int value in set) { result += value; }")]
    [InlineData("foreach (var pair in readOnly) { result += pair.Key; }")]
    public async Task ForeachOverHashCollectionIsReported(string statements)
    {
        ImmutableArray<Diagnostic> diagnostics =
            await SimAnalysis.AnalyzeAsync(Snippet.InMethod(Collections + statements));

        Assert.Contains(diagnostics, d => d.Id == UnorderedIterationAnalyzer.DiagnosticId);
    }

    [Theory]
    [InlineData("var e = dictionary.GetEnumerator();")]
    [InlineData("var e = set.GetEnumerator();")]
    public async Task ExplicitEnumeratorIsReported(string statements)
    {
        ImmutableArray<Diagnostic> diagnostics =
            await SimAnalysis.AnalyzeAsync(Snippet.InMethod(Collections + statements));

        Assert.Contains(diagnostics, d => d.Id == SimAnalysis.BannedApiId);
    }

    [Theory]
    [InlineData("if (dictionary.TryGetValue(1, out int value)) { result = value; }")]
    [InlineData("result = set.Contains(1) ? dictionary[1] : dictionary.Count;")]
    [InlineData("foreach (int value in new[] { 1, 2 }) { result += value; }")]
    [InlineData("foreach (int value in new System.Collections.Generic.List<int> { 1 }) { result += value; }")]
    [InlineData("foreach (var pair in new System.Collections.Generic.SortedDictionary<int, int>()) { result++; }")]
    public async Task LookupsAndOrderedIterationAreAllowed(string statements)
    {
        ImmutableArray<Diagnostic> diagnostics =
            await SimAnalysis.AnalyzeAsync(Snippet.InMethod(Collections + statements));

        Assert.Empty(diagnostics);
    }
}
