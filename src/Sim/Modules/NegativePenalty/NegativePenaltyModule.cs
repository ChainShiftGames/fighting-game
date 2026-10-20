using Sim.Fighters.States;
using Sim.Rules;
using Sim.State;

namespace Sim.Modules.NegativePenalty;

/// <summary>
/// Negative penalty (P3-21): a fighter who keeps retreating builds up a counter, and at
/// <see cref="NegativePenaltyConfig.PenaltyFrames"/> loses meter and tells presentation (a module event). Moving
/// forward wins it back. Off, running away costs nothing.
/// </summary>
/// <remarks>
/// A frame counts as retreating when the fighter ended it further back than it began by its own movement: walking
/// back, backdashing (ground or air) or jumping back. Being pushed (by the opponent's pushbox, pushback, hitstun or a
/// throw) doesn't count, so pressure never punishes the defender. The meter goes through the core (<see cref="ModuleContext.TrySpendMeter"/>),
/// so without a meter module a penalty only sends its event. Both fighters are treated the same way.
/// </remarks>
public sealed class NegativePenaltyModule : IMechanicModule, IAfterInputHook, IEndOfFrameHook
{
    /// <summary>The code of the module event a penalty emits (<see cref="ModuleEventRequest.Code"/>).</summary>
    public const int PenaltyCode = 0;

    // Only keeps data sane: a minute of retreating is far beyond any tuning.
    private const int MaxPenaltyFrames = 3600;
    private const int MaxForwardRecovery = 60;

    // Task number of P3-21, so module tasks running in parallel don't pick the same id.
    public ModuleId Id => new(21);

    public string Name => "negative_penalty";

    public ModuleConfigLayout ConfigLayout { get; } = ModuleConfigLayout.For<NegativePenaltyConfig>(
        new ModuleConfigField("penaltyFrames", 1, MaxPenaltyFrames, "Frames of retreating that build up to a penalty."),
        new ModuleConfigField("forwardRecovery", 0, MaxForwardRecovery, "Retreat frames each frame of moving forward takes off again."),
        new ModuleConfigField("meterLoss", 0, IMeterGauge.FullMeter, "Meter a penalty drains, in hundredths of a percent."));

    public void AfterInput(ModuleContext context)
    {
        ref NegativePenaltyState penalty = ref context.GetState<NegativePenaltyState>();
        ref readonly MatchState match = ref context.State;
        if (match.Round.Round != penalty.Round)
        {
            penalty.Round = match.Round.Round;
            penalty.Player1Retreat = 0;
            penalty.Player2Retreat = 0;
        }

        penalty.Player1StartX = match.Fighters[0].Position.X;
        penalty.Player2StartX = match.Fighters[1].Position.X;
    }

    public void EndOfFrame(ModuleContext context)
    {
        ref NegativePenaltyState penalty = ref context.GetState<NegativePenaltyState>();
        ref readonly NegativePenaltyConfig config = ref context.GetConfig<NegativePenaltyConfig>();
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            ref readonly FighterState state = ref context.State.Fighters[fighter];
            int startX = fighter == 0 ? penalty.Player1StartX : penalty.Player2StartX;
            int moved = (state.Position.X - startX) * state.Facing;
            ref int retreat = ref fighter == 0 ? ref penalty.Player1Retreat : ref penalty.Player2Retreat;
            if (moved > 0)
            {
                retreat = retreat > config.ForwardRecovery ? retreat - config.ForwardRecovery : 0;
            }
            else if (moved < 0 && IsRetreat(state.Action) && ++retreat >= config.PenaltyFrames)
            {
                retreat = 0;
                Penalize(context, fighter, in state, config.MeterLoss);
            }
        }
    }

    private void Penalize(ModuleContext context, int fighter, in FighterState state, int meterLoss)
    {
        int meter = context.MeterOf(fighter);
        context.TrySpendMeter(fighter, meter < meterLoss ? meter : meterLoss);
        context.RequestEvent(
            fighter, new ModuleEventRequest { Module = Id, Code = PenaltyCode, Position = state.Position });
    }

    // The fighter's own movement only: an opponent's push, pushback or a throw never counts.
    private static bool IsRetreat(FighterAction action) =>
        action is FighterAction.WalkBack or FighterAction.Backdash or FighterAction.AirBackdash or FighterAction.Jump;
}
