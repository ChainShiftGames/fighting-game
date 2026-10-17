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

    /// <summary>Upper bound for any input delay: the most a player may choose (ADR-0005 §4).</summary>
    public const int MaxInputDelayFrames = 4;

    /// <summary>Upper bound for the ping step: a second of round trip is beyond any playable match.</summary>
    public const int MaxPingStepMilliseconds = 1000;

    /// <summary>Upper bound for the time between ping readings: one minute of logic frames.</summary>
    public const int MaxPingSampleIntervalFrames = 3600;

    /// <summary>Upper bound for the readings averaged.</summary>
    public const int MaxPingSamples = 600;

    /// <summary>
    /// Lower bound for the disconnect warning: above Backdash's 200 ms keep-alive, so a quiet but healthy connection
    /// never warns.
    /// </summary>
    public const int MinDisconnectWarningMilliseconds = 250;

    /// <summary>Upper bound for both disconnect timeouts: a minute of silence is a lost connection.</summary>
    public const int MaxDisconnectMilliseconds = 60_000;

    /// <summary>
    /// Lower bound for the desync history: one second, past the largest rollback window plus the time the other PC's
    /// checksums take to arrive.
    /// </summary>
    public const int MinDesyncHistoryFrames = 60;

    /// <summary>Upper bound for the desync history: a minute of frames (each keeps a whole state).</summary>
    public const int MaxDesyncHistoryFrames = 3600;
}
