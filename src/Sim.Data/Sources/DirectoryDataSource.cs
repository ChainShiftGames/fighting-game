using System;
using System.Collections.Generic;
using System.IO;

namespace Sim.Data.Sources;

/// <summary>Data files read from a folder on disk, e.g. the repository's <c>data/</c> folder.</summary>
public sealed class DirectoryDataSource : IDataSource
{
    private readonly string _root;

    public DirectoryDataSource(string root)
    {
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"Data folder '{root}' does not exist.");
        }

        _root = Path.GetFullPath(root);
    }

    public bool FileExists(string path) => File.Exists(ToFullPath(path)) && HasExactCase(path);

    public byte[] ReadFile(string path) => File.ReadAllBytes(ToFullPath(path));

    public IReadOnlyList<string> ListFiles(string directory) =>
        Names(Directory.Exists(ToFullPath(directory)) ? Directory.GetFiles(ToFullPath(directory)) : []);

    public IReadOnlyList<string> ListDirectories(string directory) =>
        Names(Directory.Exists(ToFullPath(directory)) ? Directory.GetDirectories(ToFullPath(directory)) : []);

    private string ToFullPath(string path)
    {
        if (path.Contains('\\', StringComparison.Ordinal) || path.Contains("..", StringComparison.Ordinal)
            || Path.IsPathRooted(path))
        {
            throw new ArgumentException($"Data path '{path}' must be relative and use '/' separators.", nameof(path));
        }

        return Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar));
    }

    // Windows file systems ignore case, Linux doesn't: reject a wrongly cased path here too, so a casing mistake
    // fails on the developer's PC instead of only on the Steam Deck.
    private bool HasExactCase(string path)
    {
        string current = _root;
        foreach (string part in path.Split('/'))
        {
            string[] matches = Directory.GetFileSystemEntries(current, part);
            if (matches.Length != 1 || !string.Equals(Path.GetFileName(matches[0]), part, StringComparison.Ordinal))
            {
                return false;
            }

            current = matches[0];
        }

        return true;
    }

    private static string[] Names(string[] paths)
    {
        string[] names = new string[paths.Length];
        for (int i = 0; i < paths.Length; i++)
        {
            names[i] = Path.GetFileName(paths[i]);
        }

        Array.Sort(names, StringComparer.Ordinal);
        return names;
    }
}
