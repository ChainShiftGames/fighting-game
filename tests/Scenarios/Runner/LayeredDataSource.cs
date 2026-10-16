using System;
using System.Collections.Generic;
using System.Linq;
using Sim.Data.Sources;

namespace Scenarios.Tests.Runner;

/// <summary>Several data folders read as one, so fixture characters load with the real button set from data/.</summary>
/// <remarks>A file may exist in only one layer: a fixture never silently replaces real data.</remarks>
public sealed class LayeredDataSource : IDataSource
{
    private readonly IDataSource[] _layers;

    public LayeredDataSource(params IDataSource[] layers)
    {
        _layers = layers;
    }

    public bool FileExists(string path) => LayersWith(path).Any();

    public byte[] ReadFile(string path)
    {
        List<IDataSource> layers = LayersWith(path).ToList();
        if (layers.Count > 1)
        {
            throw new InvalidOperationException($"Data file '{path}' exists in more than one data folder.");
        }

        return layers[0].ReadFile(path);
    }

    public IReadOnlyList<string> ListFiles(string directory) =>
        Union(layer => layer.ListFiles(directory));

    public IReadOnlyList<string> ListDirectories(string directory) =>
        Union(layer => layer.ListDirectories(directory));

    private IEnumerable<IDataSource> LayersWith(string path) => _layers.Where(layer => layer.FileExists(path));

    private List<string> Union(Func<IDataSource, IReadOnlyList<string>> list) =>
        _layers.SelectMany(list).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
}
