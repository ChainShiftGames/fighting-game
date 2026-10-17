using FightingGame.Presentation.Events;
using Netcode.Drivers;
using Sim.State;

namespace FightingGame.Training;

/// <summary>
/// What the training menu does to the running training match: start over, and save and load a savestate. Starting
/// over and loading also clear the frame meter and stop running effects, which belonged to the state left behind.
/// </summary>
/// <remarks>
/// A savestate is a copy of the match state, the same plain-data snapshot rollback keeps (ADR-0007 §4). It lives
/// only in memory, for this training match: never saved to disk, so it never needs versioning, and gone when the
/// match is rebuilt (e.g. with reloaded data, whose moves it may not fit).
/// </remarks>
public sealed class TrainingSession
{
    private readonly ITrainingMatchDriver<MatchState> _driver;
    private readonly FrameMeter _meter;
    private readonly IEffectReset _effects;
    private MatchState _saved;

    public TrainingSession(ITrainingMatchDriver<MatchState> driver, FrameMeter meter, IEffectReset effects)
    {
        _driver = driver;
        _meter = meter;
        _effects = effects;
    }

    public bool HasSavestate { get; private set; }

    /// <summary>Both fighters back at their start positions with full health; the savestate is kept.</summary>
    public void Restart()
    {
        _driver.Restart();
        _meter.Clear();
        _effects.ResetEffects();
    }

    /// <summary>Remembers the match as it is now, replacing an earlier savestate.</summary>
    public void SaveState()
    {
        _saved = _driver.State;
        HasSavestate = true;
    }

    /// <summary>Continues from the savestate on the next frame; does nothing before one is saved.</summary>
    public void LoadState()
    {
        if (!HasSavestate)
        {
            return;
        }

        _driver.Restore(in _saved);
        _meter.Clear();
        _effects.ResetEffects();
    }
}
