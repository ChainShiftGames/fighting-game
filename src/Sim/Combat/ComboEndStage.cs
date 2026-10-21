using Sim.Fighters.States;
using Sim.Pipeline;
using Sim.State;

namespace Sim.Combat;

/// <summary>Ends a fighter's combo count once it can act or block again, so only true combos count.</summary>
/// <remarks>
/// Runs right after the state machines and before hit resolution: a fighter whose hitstun ran out this frame is
/// free again before the next hit lands, and that hit starts a new count. A combo lasts while the fighter is in
/// hitstun, air hitstun, knockdown or being thrown; blockstun, wake-up and anything it does itself end it. Hits are
/// counted where they land (<see cref="HitReactions.TakeHit"/>). Each fighter is judged on its own state alone.
/// </remarks>
public sealed class ComboEndStage : ITickStage
{
    public void Run(ref MatchState state, in FrameActivity activity)
    {
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            ref FighterState current = ref state.Fighters[fighter];
            if (!IsInCombo(current.Action))
            {
                current.ComboHits = 0;
            }
        }
    }

    private static bool IsInCombo(FighterAction action) => action is FighterAction.Hitstun
        or FighterAction.CrouchHitstun or FighterAction.AirHitstun or FighterAction.Knockdown or FighterAction.Thrown;
}
