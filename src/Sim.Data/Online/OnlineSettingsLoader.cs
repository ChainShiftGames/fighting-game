using Sim.Data.Sources;
using Sim.Data.Validation;

namespace Sim.Data.Online;

/// <summary>Reads and validates <c>netcode/online.json</c>.</summary>
/// <remarks>Separate from <see cref="GameDataLoader"/>: these settings don't change game results (no data hash).</remarks>
public static class OnlineSettingsLoader
{
    /// <exception cref="DataValidationException">The file is missing or invalid.</exception>
    public static OnlineSettings Load(IDataSource source)
    {
        DataErrors errors = new();
        OnlineSettings? settings = Read(source, errors);
        errors.ThrowIfAny();
        return settings!;
    }

    private static OnlineSettings? Read(IDataSource source, DataErrors errors)
    {
        FieldReader? file = JsonDataFile.Open(source, OnlineSchema.File, errors);
        if (file is null || !SchemaVersion.Check(file, OnlineSchema.Version))
        {
            return null;
        }

        file.Ignore("$schema");
        int rollbackFrames = file.RequiredInt("rollbackFrames", 1, OnlineSchema.MaxRollbackFrames);
        file.Finish();
        return errors.Count > 0 ? null : new OnlineSettings(rollbackFrames);
    }
}
