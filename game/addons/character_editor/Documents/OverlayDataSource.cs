using System;
using System.Collections.Generic;
using Sim.Data.Sources;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// The data on disk with the editor's unsaved files laid over it, so Sim.Data can validate the data exactly as the
/// game would load it after saving, before anything is written.
/// </summary>
public sealed class OverlayDataSource : IDataSource
{
    private readonly IDataSource _disk;
    private readonly IReadOnlyDictionary<string, byte[]> _files;

    /// <param name="files">Contents by data-relative path; they replace or add to <paramref name="disk"/>'s files.</param>
    public OverlayDataSource(IDataSource disk, IReadOnlyDictionary<string, byte[]> files)
    {
        _disk = disk;
        _files = files;
    }

    public bool FileExists(string path) => _files.ContainsKey(path) || _disk.FileExists(path);

    public byte[] ReadFile(string path) => _files.TryGetValue(path, out byte[]? bytes) ? bytes : _disk.ReadFile(path);

    public IReadOnlyList<string> ListFiles(string directory)
    {
        SortedSet<string> names = new(_disk.ListFiles(directory), StringComparer.Ordinal);
        foreach (string path in _files.Keys)
        {
            string? child = ChildOf(directory, path);
            if (child is not null && !child.Contains('/', StringComparison.Ordinal))
            {
                names.Add(child);
            }
        }

        return [.. names];
    }

    public IReadOnlyList<string> ListDirectories(string directory)
    {
        SortedSet<string> names = new(_disk.ListDirectories(directory), StringComparer.Ordinal);
        foreach (string path in _files.Keys)
        {
            string? child = ChildOf(directory, path);
            int slash = child?.IndexOf('/', StringComparison.Ordinal) ?? -1;
            if (slash > 0)
            {
                names.Add(child![..slash]);
            }
        }

        return [.. names];
    }

    private static string? ChildOf(string directory, string path)
    {
        string prefix = directory + "/";
        return path.StartsWith(prefix, StringComparison.Ordinal) ? path[prefix.Length..] : null;
    }
}
