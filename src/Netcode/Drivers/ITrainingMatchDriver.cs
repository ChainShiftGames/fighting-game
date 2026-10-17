namespace Netcode.Drivers;

/// <summary>A training match: one player against a dummy, which can start over or jump to a saved state at any time.</summary>
/// <remarks>
/// Savestates (P3-33) copy <see cref="IMatchDriver{TState}.State"/> to save and call <see cref="Restore"/> to load;
/// live data reload (P3-43) and the developer panel (P3-34) build a new match, or call <see cref="Restart"/>.
/// </remarks>
public interface ITrainingMatchDriver<TState> : IMatchDriver<TState>
    where TState : unmanaged
{
    /// <summary>Back to the match's first state, as if training had just started.</summary>
    void Restart();

    /// <summary>Continues from <paramref name="state"/> on the next frame: a savestate saved from this match.</summary>
    void Restore(in TState state);
}
