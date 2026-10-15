using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Sim.Analyzers.Tests;

/// <summary>A file on disk passed to analyzers as an MSBuild AdditionalFiles item.</summary>
internal sealed class AdditionalTextFile(string path) : AdditionalText
{
    public override string Path { get; } = path;

    public override SourceText GetText(CancellationToken cancellationToken = default) =>
        SourceText.From(File.ReadAllText(Path));
}
