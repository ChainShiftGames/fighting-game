using FightingGame.Debug;

namespace FightingGame.Loop;

/// <summary>
/// Pauses a local match and runs it one logic frame per step request (developer setting). Input is still read
/// immediately before each stepped frame, so a held direction or button goes into it. The pause menu also holds it
/// (<see cref="IsHeld"/>), which no step gets past.
/// </summary>
public sealed class FrameStepping
{
    private readonly DeveloperToggle _pause;
    private int _pendingSteps;

    public FrameStepping(DeveloperToggle pause)
    {
        _pause = pause;
    }

    public bool IsPaused => _pause.Enabled;

    /// <summary>Held by the pause menu: no frames run until it is let go.</summary>
    public bool IsHeld { get; set; }

    /// <summary>Queues one frame to run while paused; ignored while running.</summary>
    public void StepOnce()
    {
        if (_pause.Enabled)
        {
            _pendingSteps++;
        }
    }

    /// <summary>The logic frames to run now, given how many the clock says are due.</summary>
    /// <remarks>The clock keeps counting while paused, so resuming continues at normal speed instead of catching up.</remarks>
    public int Allow(int dueFrames)
    {
        int allowed = IsHeld ? 0 : _pause.Enabled ? _pendingSteps : dueFrames;
        _pendingSteps = 0;
        return allowed;
    }
}
