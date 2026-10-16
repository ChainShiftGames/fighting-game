namespace Sim.Data.Validation;

/// <summary>Checks the <c>schemaVersion</c> field every data file starts with (ARCHITECTURE.md §7).</summary>
public static class SchemaVersion
{
    public const string FieldName = "schemaVersion";

    /// <summary>
    /// True when the file has the version this build reads. Otherwise the error is reported and the caller should
    /// stop reading the file, because its other fields may mean something else.
    /// </summary>
    public static bool Check(FieldReader reader, int supported)
    {
        int version = reader.RequiredInt(FieldName, 1, int.MaxValue);
        if (version == supported)
        {
            return true;
        }

        reader.Error(FieldName, $"this build reads schema version {supported}, the file has {version}");
        return false;
    }
}
