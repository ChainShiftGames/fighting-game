using System;
using System.IO;
using Sim.Data.Sources;
using Xunit;

namespace Sim.Data.Tests.Sources;

public sealed class DirectoryDataSourceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "sim-data-tests-" + Guid.NewGuid().ToString("N"));

    public DirectoryDataSourceTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "characters", "b_man"));
        Directory.CreateDirectory(Path.Combine(_root, "characters", "a_man"));
        Directory.CreateDirectory(Path.Combine(_root, "characters", "a"));
        File.WriteAllText(Path.Combine(_root, "characters", "a_man", "character.json"), "{}");
        File.WriteAllText(Path.Combine(_root, "characters", "a_man", "Moves.json"), "{}");
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void ListingsAreSortedOrdinally()
    {
        DirectoryDataSource source = new(_root);

        Assert.Equal(["a", "a_man", "b_man"], source.ListDirectories("characters"));
        Assert.Equal(["Moves.json", "character.json"], source.ListFiles("characters/a_man"));
    }

    // Windows would find "Character.json" anyway; Linux and the Steam Deck would not.
    [Fact]
    public void PathsAreCaseSensitiveOnEveryPlatform()
    {
        DirectoryDataSource source = new(_root);

        Assert.True(source.FileExists("characters/a_man/character.json"));
        Assert.False(source.FileExists("characters/a_man/Character.json"));
        Assert.False(source.FileExists("Characters/a_man/character.json"));
        Assert.False(source.FileExists("characters/a_man/moves.json"));
    }

    [Theory]
    [InlineData(@"characters\a_man\character.json")]
    [InlineData("characters/../secret.json")]
    public void PathsMustBeRelativeWithForwardSlashes(string path)
    {
        DirectoryDataSource source = new(_root);

        Assert.Throws<ArgumentException>(() => source.FileExists(path));
    }
}
