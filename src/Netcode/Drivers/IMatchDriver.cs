using System;
using Sim.Input;

namespace Netcode.Drivers;

/// <summary>Runs one match of any type (local, online, replay, training, test) frame by frame (ARCHITECTURE.md §8).</summary>
/// <remarks>
/// The host's 60 Hz loop calls <see cref="TryAdvanceFrame"/> once per due logic frame with the input words it just
/// read, then draws <see cref="State"/> (ARCHITECTURE.md §3). Rollback, prediction and resimulation all happen
/// inside the call, so the host never sees them. Only the composition root (the code that starts a match) picks the
/// implementation: local, SyncTest, replay, test, and later training and online.
/// </remarks>
public interface IMatchDriver<TState> : IDisposable
    where TState : unmanaged
{
    /// <summary>
    /// Input words <see cref="TryAdvanceFrame"/> expects: one per player on this PC, in port order. 0 when no one on
    /// this PC plays (replay, bot match).
    /// </summary>
    int LocalPlayerCount { get; }

    /// <summary>The logic frame <see cref="State"/> is at (0 before the first frame).</summary>
    int CurrentFrame { get; }

    /// <summary>The newest state, possibly built on predicted inputs. Read it; never change it (ARCHITECTURE.md §9).</summary>
    ref readonly TState State { get; }

    /// <summary>
    /// Runs one logic frame with this PC's input words. Returns false when the frame can't run yet (e.g. online,
    /// waiting for the other player); the host then tries again on its next logic frame.
    /// </summary>
    /// <exception cref="Desync.DesyncDetectedException">The match driver found a desync.</exception>
    bool TryAdvanceFrame(ReadOnlySpan<InputWord> localInputs);
}
