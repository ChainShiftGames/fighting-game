using System.Runtime.CompilerServices;
using Backdash;
using Backdash.Core;
using Sim.Input;

namespace Netcode.Drivers;

/// <summary>
/// Builds a Backdash SyncTest session: every frame is rolled back and resimulated, and a checksum mismatch between
/// the runs is a desync (ADR-0006). Pass it to <see cref="RollbackMatchDriver{TState}"/>.
/// </summary>
public static class SyncTestSession
{
    /// <param name="checkDistanceFrames">How far each check rolls back: the rollback window (developer setting, ADR-0003).</param>
    public static INetcodeSession<InputWord> Build<TState>(int checkDistanceFrames)
        where TState : unmanaged =>
        RollbackNetcode
            .WithInputType(type => type.Struct<InputWord>())
            .WithPlayerCount(MatchPlayers.Count)
            .WithStateSizeHint(Unsafe.SizeOf<TState>())
            .WithLogLevel(LogLevel.Warning, appendLevel: true)
            .ForSyncTest(options => options.CheckDistance(checkDistanceFrames).ThrowError(true))
            .Build();
}
