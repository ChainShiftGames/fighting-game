using System;
using System.Collections.Generic;
using System.IO;
using Sim.Data.Sources;

namespace FightingGame.Debug;

/// <summary>
/// The data on disk with the editor's unsaved files laid over it, and its deleted files hidden, so Sim.Data can
/// validate the data exactly as the game would load it after saving, before anything is written.
/// </summary>
public sealed class OverlayDataSource : IDataSource
{
    private readonly IDataSource _disk;
    private readonly IReadOnlyDictionary<string, byte[]> _files;
    private readonly HashSet<string> _removed;

    /// <param name="files">Contents by data-relative path; they replace or add to <paramref name="disk"/>'s files.</param>
    /// <param name="removed">Data-relative paths of files that are deleted on saving.</param>
    public OverlayDataSource(
        IDataSource disk, IReadOnlyDictionary<string, byte[]> files, IEnumerable<string>? removed = null)
    {
        _disk = disk;
        _files = files;
        _removed = new HashSet<string>(removed ?? [], StringComparer.Ordinal);
    }

    public bool FileExists(string path) =>
        _files.ContainsKey(path) || (!_removed.Contains(path) && _disk.FileExists(path));

    public byte[] ReadFile(string path)
    {
        if (_files.TryGetValue(path, out byte[]? bytes))
        {
            return bytes;
        }

        return _removed.Contains(path) ? throw new FileNotFoundException("Deleted in the editor.", path)
            : _disk.ReadFile(path);
    }

    public IReadOnlyList<string> ListFiles(string directory)
    {
        SortedSet<string> names = new(StringComparer.Ordinal);
        foreach (string name in _disk.ListFiles(directory))
        {
            if (!_removed.Contains(directory + "/" + name))
            {
                names.Add(name);
            }
        }

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

    // A folder whose files are all deleted stays listed: saving deletes files, not folders.
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
