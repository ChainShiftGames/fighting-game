namespace Sim.Data.Online;

/// <summary>Version, path and bounds of the online match settings file (data/schema/online.schema.json).</summary>
public static class OnlineSchema
{
    public const int Version = 1;

    public const string File = "netcode/online.json";

    /// <summary>
    /// Upper bound for the rollback window: one displayed frame may re-run this many logic frames, and at the 1 ms
    /// budget per frame (ADR-0003) 12 still leaves time to draw within a 60 Hz frame.
    /// </summary>
    public const int MaxRollbackFrames = 12;
}
