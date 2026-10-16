namespace Netcode.Online;

/// <summary>Backdash's view of an online match's connection plus the rollbacks so far (developer overlay, P0-22).</summary>
/// <param name="Frame">The logic frame the match was at when this was read.</param>
/// <param name="PingMilliseconds">Round trip as Backdash measures it, through whatever transport the match uses.</param>
/// <param name="LocalFramesBehind">Frames this PC is behind the other one; time sync waits these out.</param>
/// <param name="RemoteFramesBehind">Frames the other PC is behind this one.</param>
/// <param name="PendingInputs">This PC's inputs the other PC hasn't acknowledged yet.</param>
/// <param name="Rollbacks">Rollbacks since the match started.</param>
/// <param name="RolledBackFrames">Frames resimulated by all those rollbacks together.</param>
/// <param name="LongestRollbackFrames">Frames resimulated by the longest single rollback.</param>
public readonly record struct ConnectionStats(
    int Frame,
    int PingMilliseconds,
    int LocalFramesBehind,
    int RemoteFramesBehind,
    int PendingInputs,
    int Rollbacks,
    long RolledBackFrames,
    int LongestRollbackFrames);
