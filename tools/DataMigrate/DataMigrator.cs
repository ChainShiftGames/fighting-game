using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using Sim.Data.Json;

namespace DataMigrate;

/// <summary>Brings every data file under a root to the newest version its migrations reach (ADR-0021).</summary>
/// <remarks>
/// A file is rewritten only when a step applies, in the shared hand-written layout (<see cref="JsonDataWriter"/>),
/// keeping its hand-wrapped lists; up-to-date files are left byte for byte, so running it twice changes nothing.
/// </remarks>
public sealed class DataMigrator
{
    private const string VersionField = "schemaVersion";

    private readonly IReadOnlyList<IDataMigration> _migrations;

    public DataMigrator(IReadOnlyList<IDataMigration> migrations)
    {
        _migrations = migrations;
    }

    /// <summary>Migrates the files under <paramref name="dataRoot"/>; returns the paths it rewrote, relative.</summary>
    /// <param name="write">False only reports what would change.</param>
    public IReadOnlyList<string> Run(string dataRoot, bool write)
    {
        List<string> changed = [];
        foreach (string file in Directory.EnumerateFiles(dataRoot, "*.json", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(dataRoot, file).Replace('\\', '/');
            byte[] bytes = File.ReadAllBytes(file);
            string? migrated = Migrate(relative, bytes);
            if (migrated is null)
            {
                continue;
            }

            changed.Add(relative);
            if (write)
            {
                File.WriteAllText(file, migrated, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
        }

        changed.Sort(StringComparer.Ordinal);
        return changed;
    }

    /// <summary>The file's new text, or null when no step applies.</summary>
    public string? Migrate(string relativePath, ReadOnlySpan<byte> json)
    {
        DataFileKind kind = DataFileKinds.Of(relativePath);
        if (kind == DataFileKind.Other || JsonNode.Parse(json) is not JsonObject root ||
            root[VersionField] is not JsonValue versionValue || !versionValue.TryGetValue(out int version))
        {
            return null;
        }

        int start = version;
        for (IDataMigration? step = Find(kind, version); step is not null; step = Find(kind, version))
        {
            step.Apply(root, relativePath);
            version++;
            root[VersionField] = version;
        }

        return version == start ? null : JsonDataWriter.Write(root, ListLineStarts.Read(json));
    }

    private IDataMigration? Find(DataFileKind kind, int version)
    {
        foreach (IDataMigration migration in _migrations)
        {
            if (migration.Kind == kind && migration.FromVersion == version)
            {
                return migration;
            }
        }

        return null;
    }
}
