using Sim.Characters;
using Sim.Math;
using Sim.Rules;
using Sim.State;

namespace Sim.Modules.Meter;

/// <summary>
/// Meter and supers (P3-13): a Tension-style gauge of 0–100 % per fighter, gained by attacking (hits and blocks) and
/// by moving forward; a super costs <see cref="MeterConfig.SuperCost"/>, and without that much it can't come out.
/// Off (not in the lineup), supers cost nothing.
/// </summary>
/// <remarks>
/// Per frame: a new round sets the meters up (after input); with too little meter, supers are locked for the state
/// machine step (move lock request), so the input falls through to the next move it matches; a super the step started
/// is paid for right after it; hits and blocks pay the attacker; at the end of the frame, moving forward pays. Also the
/// lineup's <see cref="IMeterGauge"/>, so other modules read and spend meter through the core (README.md here).
/// </remarks>
public sealed class MeterModule :
    IMechanicModule,
    IAfterInputHook,
    IBeforeStateMachinesHook,
    IAfterStateMachinesHook,
    IOnHitHook,
    IOnBlockHook,
    IEndOfFrameHook,
    IMeterGauge
{
    // Task number of P3-13, so module tasks running in parallel don't pick the same id.
    public ModuleId Id => new(13);

    public string Name => "meter";

    public ModuleConfigLayout ConfigLayout { get; } = ModuleConfigLayout.For<MeterConfig>(
        new ModuleConfigField("superCost", 0, IMeterGauge.FullMeter),
        new ModuleConfigField("hitGain", 0, IMeterGauge.FullMeter),
        new ModuleConfigField("blockGain", 0, IMeterGauge.FullMeter),
        new ModuleConfigField("forwardGain", 0, IMeterGauge.FullMeter),
        new ModuleConfigField("roundStartMeter", 0, IMeterGauge.FullMeter),
        new ModuleConfigField("carryOverRounds", 0, 1),
        new ModuleConfigField("channel", 0, IMeterGauge.ChannelCount - 1));

    public void AfterInput(ModuleContext context)
    {
        ref MeterState meter = ref context.GetState<MeterState>();
        ref readonly MatchState match = ref context.State;
        if (match.Round.Round != meter.Round)
        {
            StartRound(ref meter, match.Round.Round, in context.GetConfig<MeterConfig>());
        }

        meter.Player1StartX = match.Fighters[0].Position.X;
        meter.Player2StartX = match.Fighters[1].Position.X;
    }

    public void BeforeStateMachines(ModuleContext context)
    {
        ref MeterState meter = ref context.GetState<MeterState>();
        int cost = context.GetConfig<MeterConfig>().SuperCost;
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            if (MeterOf(ref meter, fighter) < cost)
            {
                context.RequestMoveLock(fighter, new MoveLockRequest { Categories = MoveCategory.Super });
            }
        }
    }

    public void AfterStateMachines(ModuleContext context)
    {
        ref MeterState meter = ref context.GetState<MeterState>();
        int cost = context.GetConfig<MeterConfig>().SuperCost;
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            // A super entered without the lock (a module's cancel request) still pays what there is.
            if (context.State.Fighters[fighter].StartedMoveCategory == MoveCategory.Super)
            {
                Add(ref meter, fighter, -cost);
            }
        }
    }

    public void OnHit(ModuleContext context, in FighterContact contact) =>
        Add(ref context.GetState<MeterState>(), contact.Attacker, context.GetConfig<MeterConfig>().HitGain);

    public void OnBlock(ModuleContext context, in FighterContact contact) =>
        Add(ref context.GetState<MeterState>(), contact.Attacker, context.GetConfig<MeterConfig>().BlockGain);

    public void EndOfFrame(ModuleContext context)
    {
        ref MeterState meter = ref context.GetState<MeterState>();
        int gain = context.GetConfig<MeterConfig>().ForwardGain;
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            ref readonly FighterState state = ref context.State.Fighters[fighter];
            int startX = fighter == 0 ? meter.Player1StartX : meter.Player2StartX;
            if ((state.Position.X - startX) * state.Facing > 0)
            {
                Add(ref meter, fighter, gain);
            }
        }
    }

    public int ChannelOf(ref readonly ModuleConfig config) => ModuleConfigView.As<MeterConfig>(in config).Channel;

    public int MeterOf(ModuleContext context, int fighter) => MeterOf(ref context.GetState<MeterState>(), fighter);

    public bool TrySpend(ModuleContext context, int fighter, int amount)
    {
        ref MeterState meter = ref context.GetState<MeterState>();
        if (MeterOf(ref meter, fighter) < amount)
        {
            return false;
        }

        Add(ref meter, fighter, -amount);
        return true;
    }

    private static void StartRound(ref MeterState meter, int round, in MeterConfig config)
    {
        // Round 1 is the match start: nothing to carry over yet.
        bool isFirstSetup = meter.Round == 0;
        meter.Round = round;
        if (isFirstSetup || config.CarryOverRounds == 0)
        {
            meter.Player1Meter = config.RoundStartMeter;
            meter.Player2Meter = config.RoundStartMeter;
        }
    }

    private static int MeterOf(ref MeterState meter, int fighter) =>
        fighter == 0 ? meter.Player1Meter : meter.Player2Meter;

    // Clamped to the gauge: gains past full are lost, a spend never goes below empty.
    private static void Add(ref MeterState meter, int fighter, int amount)
    {
        ref int value = ref fighter == 0 ? ref meter.Player1Meter : ref meter.Player2Meter;
        value = IntMath.Clamp(value + amount, 0, IMeterGauge.FullMeter);
    }
}
