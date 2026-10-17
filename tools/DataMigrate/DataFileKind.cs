namespace DataMigrate;

/// <summary>Which schema a data file follows, told apart by its path under the data root.</summary>
public enum DataFileKind
{
    /// <summary>Not a versioned data file the migrator knows (schemas, unknown folders).</summary>
    Other,

    Character,
    Move,
    Rules,
    Input,
}
