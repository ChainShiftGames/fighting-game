using System;
using Netcode.Drivers;
using Sim.Input;
using Sim.State;

namespace Netcode.Tests.Online;

/// <summary>A match driver whose round phase and readiness the test sets.</summary>
internal sealed class ScriptedRoundDriver : IMatchDriver<MatchState>, IConfirmedFrameReadout
{
    private MatchState _state;

    public RoundPhase Phase
    {
        get => _state.Round.Phase;
        set => _state.Round.Phase = value;
    }

    /// <summary>False to make frames wait, as online while the other PC is behind.</summary>
    public bool CanAdvance { get; set; } = true;

    public bool Disposed { get; private set; }

    public int LocalPlayerCount => 1;

    public int CurrentFrame { get; private set; }

    /// <summary>Frames the test has made final; it sets them.</summary>
    public int ConfirmedFrame { get; set; }

    public ref readonly MatchState State => ref _state;

    public bool TryAdvanceFrame(ReadOnlySpan<InputWord> localInputs)
    {
        if (CanAdvance)
        {
            CurrentFrame++;
        }

        return CanAdvance;
    }

    public void Dispose() => Disposed = true;
}
