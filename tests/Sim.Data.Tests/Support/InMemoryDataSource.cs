using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sim.Data.Sources;

namespace Sim.Data.Tests.Support;

/// <summary>Data files held in memory, so a test can break one file without touching disk.</summary>
public sealed class InMemoryDataSource : IDataSource
{
    private readonly SortedDictionary<string, byte[]> _files = new(StringComparer.Ordinal);

    public IEnumerable<string> Paths => _files.Keys;

    public void Set(string path, byte[] bytes) => _files[path] = bytes;

    public void SetText(string path, string text) => Set(path, Encoding.UTF8.GetBytes(text));

    public string GetText(string path) => Encoding.UTF8.GetString(_files[path]);

    public void Remove(string path) => _files.Remove(path);

    /// <summary>Replaces text that must exist in the file, so a typo in a test can't silently test nothing.</summary>
    public void Replace(string path, string oldText, string newText)
    {
        string text = GetText(path);
        if (!text.Contains(oldText, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"'{oldText}' does not occur in {path}.");
        }

        SetText(path, text.Replace(oldText, newText, StringComparison.Ordinal));
    }

    public bool FileExists(string path) => _files.ContainsKey(path);

    public byte[] ReadFile(string path) => _files[path];

    public IReadOnlyList<string> ListFiles(string directory) =>
        Children(directory).Where(child => !child.Contains('/', StringComparison.Ordinal)).ToList();

    public IReadOnlyList<string> ListDirectories(string directory) =>
        Children(directory)
            .Where(child => child.Contains('/', StringComparison.Ordinal))
            .Select(child => child[..child.IndexOf('/', StringComparison.Ordinal)])
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private IEnumerable<string> Children(string directory) =>
        _files.Keys
            .Where(path => path.StartsWith(directory + "/", StringComparison.Ordinal))
            .Select(path => path[(directory.Length + 1)..]);
}
