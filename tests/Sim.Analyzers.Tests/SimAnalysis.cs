using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Sim.Analyzers.Tests;

/// <summary>Compiles C# source with Sim's analyzers and BannedSymbols.txt and returns what they report.</summary>
internal static class SimAnalysis
{
    /// <summary>BannedApiAnalyzers' "do not use banned APIs" diagnostic.</summary>
    public const string BannedApiId = "RS0030";

    private static readonly ImmutableArray<MetadataReference> _frameworkReferences = LoadFrameworkReferences();
    private static readonly ImmutableArray<DiagnosticAnalyzer> _analyzers = LoadAnalyzers();

    // The real file from src/Sim, copied next to the test binaries by the project file.
    private static readonly AnalyzerOptions _options =
        new([new AdditionalTextFile(Path.Combine(AppContext.BaseDirectory, "BannedSymbols.txt"))]);

    public static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SyntaxTree tree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.Latest),
            cancellationToken: cancellationToken);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Sim",
            [tree],
            _frameworkReferences,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        // A snippet that doesn't compile would prove nothing about the analyzers.
        Diagnostic[] compileErrors = [.. compilation.GetDiagnostics(cancellationToken)
            .Where(d => d.Severity == DiagnosticSeverity.Error)];
        Assert.True(compileErrors.Length == 0, "Snippet doesn't compile:\n" + string.Join("\n", compileErrors));

        return await compilation.WithAnalyzers(_analyzers, _options).GetAnalyzerDiagnosticsAsync(cancellationToken);
    }

    private static ImmutableArray<MetadataReference> LoadFrameworkReferences()
    {
        string frameworkDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        string trustedAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        return [.. trustedAssemblies
            .Split(Path.PathSeparator)
            .Where(path => Path.GetDirectoryName(path) == frameworkDirectory)
            .Select(path => MetadataReference.CreateFromFile(path))];
    }

    // Loaded from the DLLs, the same way the compiler loads them for Sim, so new analyzers are picked up automatically.
    private static ImmutableArray<DiagnosticAnalyzer> LoadAnalyzers()
    {
        string bannedApiDirectory = Path.Combine(AppContext.BaseDirectory, "BannedApiAnalyzers");
        string[] analyzerFiles =
        [
            typeof(FloatingPointAnalyzer).Assembly.Location,
            .. Directory.GetFiles(bannedApiDirectory, "*.dll"),
        ];

        var loader = new AnalyzerAssemblyLoader();
        return [.. analyzerFiles.SelectMany(file =>
            new AnalyzerFileReference(file, loader).GetAnalyzers(LanguageNames.CSharp))];
    }
}
