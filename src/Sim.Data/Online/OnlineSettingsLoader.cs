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
        FieldReader? delay = file.RequiredObject("automaticInputDelay");
        AutomaticInputDelaySettings? automaticDelay = delay is null ? null : ReadAutomaticDelay(delay, errors);
        file.Finish();
        return errors.Count > 0 ? null : new OnlineSettings(rollbackFrames, automaticDelay!);
    }

    private static AutomaticInputDelaySettings ReadAutomaticDelay(FieldReader delay, DataErrors errors)
    {
        int errorsBefore = errors.Count;
        int minimumFrames = delay.RequiredInt("minimumFrames", 0, OnlineSchema.MaxInputDelayFrames);
        int maximumFrames = delay.RequiredInt("maximumFrames", 0, OnlineSchema.MaxInputDelayFrames);

        // Only compare values that were read: an invalid one is already reported.
        if (errors.Count == errorsBefore && maximumFrames < minimumFrames)
        {
            delay.Error("maximumFrames", "must be at least minimumFrames");
        }

        var settings = new AutomaticInputDelaySettings(
            minimumFrames,
            maximumFrames,
            delay.RequiredInt("pingStepMilliseconds", 1, OnlineSchema.MaxPingStepMilliseconds),
            delay.RequiredInt("pingSampleIntervalFrames", 1, OnlineSchema.MaxPingSampleIntervalFrames),
            delay.RequiredInt("pingSamples", 1, OnlineSchema.MaxPingSamples));
        delay.Finish();
        return settings;
    }
}
