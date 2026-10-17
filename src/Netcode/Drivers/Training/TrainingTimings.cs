namespace Netcode.Drivers.Training;

/// <summary>How long training waits before resetting things: developer settings (ARCHITECTURE.md §6).</summary>
/// <param name="GuardResetFrames">Frames a "block after first hit" dummy must be free before the next hit lands again.</param>
/// <param name="HealthRefillDelayFrames">Frames a fighter must be free before its health refills.</param>
public readonly record struct TrainingTimings(int GuardResetFrames, int HealthRefillDelayFrames);
