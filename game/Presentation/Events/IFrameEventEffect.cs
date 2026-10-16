using Sim.Events;

namespace FightingGame.Presentation.Events;

/// <summary>An effect (spark, sound, shake, rumble) that starts on a frame event and stops if rollback removes it.</summary>
/// <remarks>
/// <see cref="FrameEventReconciler"/> calls <see cref="Play"/> once per event id and <see cref="Cancel"/> only for an
/// event it played earlier. Each effect ignores the kinds it has nothing for. Effects never change game state
/// (ARCHITECTURE.md §9).
/// </remarks>
public interface IFrameEventEffect
{
    /// <summary>Starts the effect for an event seen for the first time.</summary>
    void Play(in FrameEvent frameEvent);

    /// <summary>Stops the effect of a played event that a rollback's re-simulation no longer produces.</summary>
    void Cancel(in FrameEvent frameEvent);
}
