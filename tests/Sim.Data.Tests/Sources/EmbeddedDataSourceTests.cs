using System;
using System.IO;
using System.Linq;
using Sim.Data.Sources;
using Sim.Data.Tests.Support;
using Xunit;

namespace Sim.Data.Tests.Sources;

/// <summary>ADR-0015: exported builds read data embedded in Sim.Data.dll; it must be exactly the data/ folder.</summary>
public sealed class EmbeddedDataSourceTests
{
    private readonly EmbeddedDataSource _embedded = new();
    private readonly DirectoryDataSource _folder = new(Fixture.RepositoryDataRoot);

    [Fact]
    public void EmbedsEveryDataFileExceptTheSchemas()
    {
        string root = Fixture.RepositoryDataRoot;
        string[] expected = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(path => !path.StartsWith("schema/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, _embedded.Paths.ToArray());
    }

    [Fact]
    public void EmbeddedFilesMatchTheFolderByteForByte()
    {
        foreach (string path in _embedded.Paths)
        {
            Assert.Equal(_folder.ReadFile(path), _embedded.ReadFile(path));
        }
    }

    [Fact]
    public void EmbeddedDataHashesLikeTheFolder()
    {
        Assert.Equal(Fixture.Load(_folder).Hash, Fixture.Load(_embedded).Hash);
    }

    [Fact]
    public void SchemasAreNotEmbedded()
    {
        Assert.False(_embedded.FileExists("schema/character.schema.json"));
        Assert.Empty(_embedded.ListDirectories("schema"));
    }
}
