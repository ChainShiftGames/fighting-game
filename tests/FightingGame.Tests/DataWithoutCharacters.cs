using System;
using System.Collections.Generic;
using Sim.Data.Sources;

namespace FightingGame.Tests;

/// <summary>A data folder seen with an empty characters/ folder, like data/ before the first real character.</summary>
/// <remarks>Keeps the tests on the fixture character after real characters land in data/.</remarks>
internal sealed class DataWithoutCharacters : IDataSource
{
    private readonly IDataSource _data;

    public DataWithoutCharacters(IDataSource data)
    {
        _data = data;
    }

    public bool FileExists(string path) => !IsCharacterPath(path) && _data.FileExists(path);

    public byte[] ReadFile(string path) => _data.ReadFile(path);

    public IReadOnlyList<string> ListFiles(string directory) => IsCharacterPath(directory) ? [] : _data.ListFiles(directory);

    public IReadOnlyList<string> ListDirectories(string directory) =>
        IsCharacterPath(directory) ? [] : _data.ListDirectories(directory);

    private static bool IsCharacterPath(string path) =>
        path == "characters" || path.StartsWith("characters/", StringComparison.Ordinal);
}
