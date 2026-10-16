using Sim.Characters;
using Sim.Events;
using Sim.Fighters.States;
using Sim.State;

namespace Sim.Throws;

/// <summary>A throw tech: both fighters let go and separate, each with its character's throw tech move.</summary>
/// <remarks>
/// The tech move (basic state <see cref="BasicState.ThrowTech"/>) carries the separation as velocity data, so how far
/// fighters slide apart is character data. Both fighters are treated alike, whoever threw.
/// </remarks>
public static class ThrowTechs
{
    public static void Separate(ref MatchState state, CharacterData player1, CharacterData player2)
    {
        Tech(ref state, 0, player1);
        Tech(ref state, 1, player2);
    }

    private static void Tech(ref MatchState state, int fighter, CharacterData character)
    {
        ref FighterState teching = ref state.Fighters[fighter];
        ThrowHolds.Clear(ref teching);
        MoveEntry.EnterBasic(ref teching, FighterAction.ThrowTech, character);
        state.Events.Emit(state.Frame, FrameEventKind.ThrowTech, fighter, teching.Position, detail: 0);
    }
}
