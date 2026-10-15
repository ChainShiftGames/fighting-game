using System.Collections.Immutable;
using System.Threading.Tasks;
using Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Sim.Analyzers.Tests;

public sealed class FloatingPointAnalyzerTests
{
    [Theory]
    [InlineData("float x = 1;")]
    [InlineData("double x = 1;")]
    [InlineData("decimal x = 1;")]
    [InlineData("System.Single x = 1;")]
    [InlineData("var x = 2.5;")]
    [InlineData("var x = 2.5f;")]
    [InlineData("result = (int)(result * 1.5);")]
    [InlineData("result = (int)System.Math.Sqrt(result);")]
    [InlineData("result = (int)System.Math.PI;")]
    [InlineData("float[] speeds = new float[4];")]
    [InlineData("float? speed = null;")]
    [InlineData("System.Func<double, int> round = d => (int)d;")]
    [InlineData("System.Half x = default;")]
    public async Task FloatInMethodIsReported(string statements)
    {
        ImmutableArray<Diagnostic> diagnostics = await SimAnalysis.AnalyzeAsync(Snippet.InMethod(statements));

        Assert.Contains(
            diagnostics,
            d => d.Id == FloatingPointAnalyzer.DiagnosticId && d.Severity == DiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData("public float Speed;")]
    [InlineData("public double Gravity { get; set; }")]
    [InlineData("public int Scale(float factor) => 0;")]
    [InlineData("public decimal Total() => 0;")]
    public async Task FloatInDeclarationIsReported(string members)
    {
        ImmutableArray<Diagnostic> diagnostics = await SimAnalysis.AnalyzeAsync(Snippet.InClass(members));

        Assert.Contains(diagnostics, d => d.Id == FloatingPointAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task FloatCannotBeSuppressedWithPragma()
    {
        string source = Snippet.InMethod("""
            #pragma warning disable FG0001
            float x = 1;
            #pragma warning restore FG0001
            """);

        ImmutableArray<Diagnostic> diagnostics = await SimAnalysis.AnalyzeAsync(source);

        Assert.Contains(diagnostics, d => d.Id == FloatingPointAnalyzer.DiagnosticId && !d.IsSuppressed);
    }
}
