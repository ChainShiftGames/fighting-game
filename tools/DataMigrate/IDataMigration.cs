using System.Text.Json.Nodes;

namespace DataMigrate;

/// <summary>One format change of one file kind: rewrites a file of version <see cref="FromVersion"/> to the next.</summary>
public interface IDataMigration
{
    DataFileKind Kind { get; }

    int FromVersion { get; }

    /// <summary>Changes the file's tree in place; the migrator then sets its schema version to the next one.</summary>
    /// <param name="file">The file's root object.</param>
    /// <param name="path">The file's path relative to the data root, for error messages.</param>
    void Apply(JsonObject file, string path);
}
