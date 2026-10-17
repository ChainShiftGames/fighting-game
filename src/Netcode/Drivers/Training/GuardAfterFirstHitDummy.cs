using Sim.Input;
using Sim.State;

namespace Netcode.Drivers.Training;

/// <summary>
/// A dummy that stands until it is hit, then blocks everything (<see cref="GuardingDummy"/>) until it has been free for
/// a while: the first hit of a string lands, the rest are blocked, so the player sees what isn't a true combo.
/// </summary>
public sealed class GuardAfterFirstHitDummy : IDummyBehaviour, ITrainingReset
{
    private readonly GuardingDummy _guard;
    private readonly int _resetFrames;
    private bool _isGuarding;
    private int _freeFrames;

    /// <param name="resetFrames">Frames the dummy must be free before it lets the next hit in again.</param>
    public GuardAfterFirstHitDummy(GuardingDummy guard, int resetFrames)
    {
        _guard = guard;
        _resetFrames = resetFrames;
    }

    public InputWord Next(in MatchState state)
    {
        FighterState dummy = state.Fighters[TrainingSides.Dummy];
        if (FighterCondition.IsHit(in dummy))
        {
            _isGuarding = true;
            _freeFrames = 0;
        }
        else if (_isGuarding && FighterCondition.IsFree(in dummy) && ++_freeFrames > _resetFrames)
        {
            _isGuarding = false;
        }
        else if (!FighterCondition.IsFree(in dummy))
        {
            _freeFrames = 0;
        }

        return _isGuarding ? _guard.Next(in state) : InputWord.Neutral;
    }

    /// <summary>Lets the next hit in again.</summary>
    public void Reset()
    {
        _isGuarding = false;
        _freeFrames = 0;
    }
}
