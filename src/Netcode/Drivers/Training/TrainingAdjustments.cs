using Sim.Composition;
using Sim.State;

namespace Netcode.Drivers.Training;

/// <summary>The training conveniences every training match applies after each frame, registered in one place.</summary>
public static class TrainingAdjustments
{
    public static ITrainingAdjustment<MatchState>[] Create(MatchSimulation simulation, TrainingTimings timings) =>
    [
        new HealthRefill([simulation.Player1, simulation.Player2], timings.HealthRefillDelayFrames),
        new FullTimer(simulation.Rules.RoundTimerFrames),
        new EndlessRounds(),
    ];
}
