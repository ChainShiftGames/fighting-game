using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FightingGame.Debug;
using Sim.Data;
using Sim.Data.Sources;
using Sim.Data.Validation;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// Saves a character's changed files to the data folder, but only when the whole game data, with the changes in
/// place, passes the same Sim.Data validation the game runs when it loads (ARCHITECTURE.md §7).
/// </summary>
public sealed class CharacterSaver
{
    private static readonly UTF8Encoding _utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly string _dataRoot;

    /// <param name="dataRoot">Full path of the <c>data/</c> folder.</param>
    public CharacterSaver(string dataRoot)
    {
        _dataRoot = dataRoot;
    }

    /// <summary>What the game would report loading the data with the document's changes; empty when valid.</summary>
    public IReadOnlyList<DataError> Validate(CharacterDocument document) => Validate(ChangedFiles(document));

    /// <summary>
    /// Writes the changed files when the data is valid with them; otherwise writes nothing.
    /// </summary>
    /// <returns>The validation errors; empty when the files were saved.</returns>
    public IReadOnlyList<DataError> Save(CharacterDocument document)
    {
        Dictionary<string, byte[]> changed = ChangedFiles(document);
        IReadOnlyList<DataError> errors = Validate(changed);
        if (errors.Count > 0)
        {
            return errors;
        }

        foreach (EditedFile file in document.Files)
        {
            if (file.IsChanged)
            {
                File.WriteAllBytes(DataFilePath.In(_dataRoot, file.Path), changed[file.Path]);
                file.MarkSaved();
            }
        }

        return errors;
    }

    private IReadOnlyList<DataError> Validate(IReadOnlyDictionary<string, byte[]> changed)
    {
        try
        {
            new GameDataLoader(new OverlayDataSource(new DirectoryDataSource(_dataRoot), changed)).Load();
            return [];
        }
        catch (DataValidationException exception)
        {
            return exception.Errors;
        }
    }

    // Validation reads exactly the bytes that saving writes.
    private static Dictionary<string, byte[]> ChangedFiles(CharacterDocument document)
    {
        Dictionary<string, byte[]> files = new(StringComparer.Ordinal);
        foreach (EditedFile file in document.Files)
        {
            if (file.IsChanged)
            {
                files[file.Path] = _utf8WithoutBom.GetBytes(JsonDataWriter.Write(file.Root));
            }
        }

        return files;
    }
}
