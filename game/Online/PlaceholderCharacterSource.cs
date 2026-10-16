using System;
using System.Collections.Generic;
using Sim.Data.Characters.Reading;
using Sim.Data.Sources;

namespace FightingGame.Online;

/// <summary>
/// Game data whose characters come from a placeholder data folder for as long as the real data has none; buttons,
/// motions and rules always come from the real data.
/// </summary>
/// <remarks>
/// Lets local matches run the real logic before the first real character exists (Testman, P1-17). As soon as the
/// real data has a character, the placeholder is ignored and this is a plain pass-through.
/// </remarks>
public sealed class PlaceholderCharacterSource : IDataSource
{
    private const string CharactersPrefix = CharacterSchema.CharactersDirectory + "/";

    private readonly IDataSource _data;
    private readonly IDataSource _characters;

    public PlaceholderCharacterSource(IDataSource data, IDataSource placeholder)
    {
        _data = data;
        UsesPlaceholder = data.ListDirectories(CharacterSchema.CharactersDirectory).Count == 0;
        _characters = UsesPlaceholder ? placeholder : data;
    }

    /// <summary>True when the real data has no characters, so they come from the placeholder folder.</summary>
    public bool UsesPlaceholder { get; }

    public bool FileExists(string path) => SourceOf(path).FileExists(path);

    public byte[] ReadFile(string path) => SourceOf(path).ReadFile(path);

    public IReadOnlyList<string> ListFiles(string directory) => SourceOf(directory).ListFiles(directory);

    public IReadOnlyList<string> ListDirectories(string directory) => SourceOf(directory).ListDirectories(directory);

    private IDataSource SourceOf(string path) =>
        string.Equals(path, CharacterSchema.CharactersDirectory, StringComparison.Ordinal)
        || path.StartsWith(CharactersPrefix, StringComparison.Ordinal)
            ? _characters
            : _data;
}
