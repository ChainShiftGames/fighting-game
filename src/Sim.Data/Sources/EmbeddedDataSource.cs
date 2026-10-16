using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Sim.Data.Sources;

/// <summary>The data files embedded in Sim.Data.dll at build time, used by exported builds (ADR-0015).</summary>
public sealed class EmbeddedDataSource : IDataSource
{
    // Matches the LogicalName prefix in Sim.Data.csproj.
    private const string ResourcePrefix = "data/";

    private readonly Assembly _assembly;
    private readonly SortedDictionary<string, string> _resourceByPath = new(StringComparer.Ordinal);

    public EmbeddedDataSource()
    {
        _assembly = typeof(EmbeddedDataSource).Assembly;
        foreach (string resource in _assembly.GetManifestResourceNames())
        {
            // MSBuild's RecursiveDir uses '\' when building on Windows; paths here always use '/'.
            string path = resource.Replace('\\', '/');
            if (path.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            {
                _resourceByPath.Add(path[ResourcePrefix.Length..], resource);
            }
        }
    }

    /// <summary>Every embedded path, sorted ordinally.</summary>
    public IEnumerable<string> Paths => _resourceByPath.Keys;

    public bool FileExists(string path) => _resourceByPath.ContainsKey(path);

    public byte[] ReadFile(string path)
    {
        using Stream stream = _assembly.GetManifestResourceStream(_resourceByPath[path])!;
        byte[] bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }

    public IReadOnlyList<string> ListFiles(string directory)
    {
        List<string> files = [];
        foreach (string child in Children(directory))
        {
            if (!child.Contains('/', StringComparison.Ordinal))
            {
                files.Add(child);
            }
        }

        return files;
    }

    public IReadOnlyList<string> ListDirectories(string directory)
    {
        List<string> directories = [];
        foreach (string child in Children(directory))
        {
            int slash = child.IndexOf('/', StringComparison.Ordinal);
            if (slash > 0 && !directories.Contains(child[..slash]))
            {
                directories.Add(child[..slash]);
            }
        }

        directories.Sort(StringComparer.Ordinal);
        return directories;
    }

    private IEnumerable<string> Children(string directory)
    {
        string prefix = directory + "/";
        foreach (string path in _resourceByPath.Keys)
        {
            if (path.StartsWith(prefix, StringComparison.Ordinal))
            {
                yield return path[prefix.Length..];
            }
        }
    }
}
