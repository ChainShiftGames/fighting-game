using System;
using Sim.Input;

namespace Netcode.Drivers;

/// <summary>
/// Training mode: the one player on this PC against a dummy. Each frame runs once (nothing to roll back); both
/// fighters' inputs come from <see cref="ITrainingInputs{TState}"/> and training conveniences apply after the frame.
/// </summary>
/// <remarks>
/// Extension points: the frame meter (P3-31) observes the simulation passed in, as the event effects do; record and
/// playback (P3-32) are <see cref="ITrainingInputs{TState}"/>; savestates (P3-33) use
/// <see cref="ITrainingMatchDriver{TState}.Restore"/>.
/// </remarks>
public sealed class TrainingMatchDriver<TState> : ITrainingMatchDriver<TState>
    where TState : unmanaged
{
    private readonly IMatchSimulation<TState> _simulation;
    private readonly ITrainingInputs<TState> _inputs;
    private readonly ITrainingAdjustment<TState>[] _adjustments;
    private TState _state;
    private int _currentFrame;

    /// <param name="inputs">Both fighters' input words from the training player's one.</param>
    /// <param name="adjustments">Applied in order after every frame.</param>
    public TrainingMatchDriver(
        IMatchSimulation<TState> simulation,
        ITrainingInputs<TState> inputs,
        ITrainingAdjustment<TState>[] adjustments)
    {
        _simulation = simulation;
        _inputs = inputs;
        _adjustments = adjustments;
        _state = simulation.CreateInitialState();
    }

    /// <summary>The training player; the dummy is played by <see cref="ITrainingInputs{TState}"/>.</summary>
    public int LocalPlayerCount => 1;

    /// <summary>Frames run since training started; it keeps counting across <see cref="Restart"/> and <see cref="Restore"/>.</summary>
    public int CurrentFrame => _currentFrame;

    public ref readonly TState State => ref _state;

    public bool TryAdvanceFrame(ReadOnlySpan<InputWord> localInputs)
    {
        MatchPlayers.EnsureInputCount(localInputs, LocalPlayerCount);
        FrameInputs inputs = _inputs.Next(in _state, localInputs[0]);
        _simulation.Tick(ref _state, inputs.Player1, inputs.Player2);
        foreach (ITrainingAdjustment<TState> adjustment in _adjustments)
        {
            adjustment.Apply(ref _state);
        }

        _currentFrame++;
        return true;
    }

    public void Restart() => _state = _simulation.CreateInitialState();

    public void Restore(in TState state) => _state = state;

    // Owns no session or socket.
    public void Dispose()
    {
    }
}
