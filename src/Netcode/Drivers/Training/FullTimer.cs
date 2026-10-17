using Sim.State;

namespace Netcode.Drivers.Training;

/// <summary>Holds the round timer at its start value, so training never times out.</summary>
public sealed class FullTimer : ITrainingAdjustment<MatchState>
{
    private readonly int _timerFrames;

    /// <param name="timerFrames">The match rules' round timer; 0 (no time limit) leaves the timer alone.</param>
    public FullTimer(int timerFrames)
    {
        _timerFrames = timerFrames;
    }

    public void Apply(ref MatchState state)
    {
        if (state.Round.Phase == RoundPhase.Fighting)
        {
            state.Round.TimerFramesLeft = _timerFrames;
        }
    }
}
