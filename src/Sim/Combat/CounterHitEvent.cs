using Sim.Events;
using Sim.Math;
using Sim.State;

namespace Sim.Combat;

/// <summary>Emits a counter hit or punish counter event; shared by fighter attacks and projectiles.</summary>
public static class CounterHitEvent
{
    /// <summary>Emits nothing for a plain hit (<see cref="CounterHitKind.None"/>).</summary>
    public static void Emit(ref MatchState state, CounterHitKind counter, int attacker, IntVector2 point)
    {
        if (counter == CounterHitKind.None)
        {
            return;
        }

        FrameEventKind kind =
            counter == CounterHitKind.CounterHit ? FrameEventKind.CounterHit : FrameEventKind.PunishCounter;
        state.Events.Emit(state.Frame, kind, attacker, point, detail: 0);
    }
}
