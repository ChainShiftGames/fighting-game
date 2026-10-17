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
    public IReadOnlyList<DataError> Validate(CharacterDocument document) =>
        Validate(ChangedFiles(document), document.RemovedFiles);

    /// <summary>
    /// Writes the changed files and deletes the removed move files when the data is valid that way; otherwise
    /// changes nothing on disk.
    /// </summary>
    /// <returns>The validation errors; empty when the files were saved.</returns>
    public IReadOnlyList<DataError> Save(CharacterDocument document)
    {
        Dictionary<string, byte[]> changed = ChangedFiles(document);
        IReadOnlyList<DataError> errors = Validate(changed, document.RemovedFiles);
        if (errors.Count > 0)
        {
            return errors;
        }

        // Deletes first: on a file system that ignores case, a new "5A.json" and a deleted "5a.json" are one file.
        foreach (string path in document.RemovedFiles)
        {
            File.Delete(DataFilePath.In(_dataRoot, path));
        }

        foreach ((string path, byte[] bytes) in changed)
        {
            File.WriteAllBytes(DataFilePath.In(_dataRoot, path), bytes);
        }

        document.MarkSaved();
        return errors;
    }

    private IReadOnlyList<DataError> Validate(IReadOnlyDictionary<string, byte[]> changed, IEnumerable<string> removed)
    {
        try
        {
            new GameDataLoader(new OverlayDataSource(new DirectoryDataSource(_dataRoot), changed, removed)).Load();
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
                files[file.Path] = _utf8WithoutBom.GetBytes(JsonDataWriter.Write(file.Root, file.Layout));
            }
        }

        return files;
    }
}
