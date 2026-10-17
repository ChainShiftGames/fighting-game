using Sim.State;

namespace Netcode.Drivers.Training;

/// <summary>Keeps training going after a KO: nobody keeps round wins and every round counts as the first.</summary>
/// <remarks>
/// The logic still plays the KO and the round-over pause, then starts a fresh round (fighters back at their start
/// positions), but it never reaches a match result, also not the draw after the last round.
/// </remarks>
public sealed class EndlessRounds : ITrainingAdjustment<MatchState>
{
    private const int FirstRound = 1;

    public void Apply(ref MatchState state)
    {
        state.Round.Wins = default;
        state.Round.Round = FirstRound;
    }
}
