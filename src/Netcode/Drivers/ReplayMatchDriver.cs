using System;
using System.Collections.Generic;
using Sim.Input;

namespace Netcode.Drivers;

/// <summary>Plays a recorded match back: both players' inputs come from the recording, none from this PC.</summary>
/// <remarks>The recording's file format and the recorder are P2-10's; this driver only needs the frames.</remarks>
public sealed class ReplayMatchDriver<TState> : IMatchDriver<TState>
    where TState : unmanaged
{
    private readonly IMatchSimulation<TState> _simulation;
    private readonly IReadOnlyList<FrameInputs> _recording;
    private TState _state;
    private int _currentFrame;

    /// <param name="recording">Every frame's inputs from the first frame on; frame n is at index n - 1.</param>
    public ReplayMatchDriver(IMatchSimulation<TState> simulation, IReadOnlyList<FrameInputs> recording)
    {
        _simulation = simulation;
        _recording = recording;
        _state = simulation.CreateInitialState();
    }

    public int LocalPlayerCount => 0;

    public int CurrentFrame => _currentFrame;

    public ref readonly TState State => ref _state;

    /// <summary>The recording has run out: every recorded frame has been played.</summary>
    public bool IsFinished => _currentFrame == _recording.Count;

    /// <summary>Plays the next recorded frame; false once the recording has run out.</summary>
    public bool TryAdvanceFrame(ReadOnlySpan<InputWord> localInputs)
    {
        MatchPlayers.EnsureInputCount(localInputs, LocalPlayerCount);
        if (IsFinished)
        {
            return false;
        }

        FrameInputs inputs = _recording[_currentFrame];
        _simulation.Tick(ref _state, inputs.Player1, inputs.Player2);
        _currentFrame++;
        return true;
    }

    // Owns no session or socket.
    public void Dispose()
    {
    }
}
