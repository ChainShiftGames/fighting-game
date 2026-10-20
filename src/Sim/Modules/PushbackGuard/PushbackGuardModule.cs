using Sim.Characters;
using Sim.Fighters.States;
using Sim.Input;
using Sim.Math;
using Sim.Rules;
using Sim.State;

namespace Sim.Modules.PushbackGuard;

/// <summary>
/// Pushback guard (P3-15), Faultless Defense style: blocking while holding the guard buttons spends meter and pushes the
/// attacker away further, so pressure ends sooner. Holding it through blockstun keeps draining meter. Off, every block
/// pushes the same.
/// </summary>
/// <remarks>
/// Holding back with the guard buttons is the guard stance: the fighter's attacks are locked for that step (move lock
/// request), so pressing the buttons never starts a throw or a normal instead. For strikes and projectiles alike, a
/// block made guarding with enough meter is adjusted before it lands (<see cref="IBlockAdjustmentHook"/>): more
/// pushback and, with <see cref="PushbackGuardConfig.NoChip"/>, no chip. The block hook then pays the meter and tells
/// presentation (a module event). In the corner the longer slide pushes the attacker back instead, the core's corner
/// rule. At the end of each frame a fighter guarding in blockstun pays the drain. Meter goes through the core
/// (<see cref="ModuleContext.TrySpendMeter"/>), so with a cost it needs a meter module. Stateless.
/// </remarks>
public sealed class PushbackGuardModule :
    IMechanicModule, IBeforeStateMachinesHook, IBlockAdjustmentHook, IOnBlockHook, IEndOfFrameHook
{
    /// <summary>The code of the module event a guarded block emits (<see cref="ModuleEventRequest.Code"/>).</summary>
    public const int GuardCode = 0;

    private const MoveCategory Attacks = MoveCategory.Normal | MoveCategory.Special | MoveCategory.Super | MoveCategory.Throw;
    private const int MaxButtonCount = 4;
    private const int Permille = 1000;
    private const int MaxPushbackPermille = 10_000;
    private const int InputBits = 16;

    // Task number of P3-15, so module tasks running in parallel don't pick the same id.
    public ModuleId Id => new(15);

    public string Name => "pushback_guard";

    public ModuleConfigLayout ConfigLayout { get; } = ModuleConfigLayout.For<PushbackGuardConfig>(
        new ModuleConfigField("buttonMask", 1, ushort.MaxValue, "Button bits: A 16, B 32, C 64, D 128, Special 256, Dash 512; add them up (48 = A+B). The guard buttons held while blocking."),
        new ModuleConfigField("buttonCount", 1, MaxButtonCount, "How many of the guard buttons must be held (2 = both)."),
        new ModuleConfigField("pushbackPermille", 0, MaxPushbackPermille, "How far a guarded block pushes, in thousandths of the hit's pushback (2000 = twice as far)."),
        new ModuleConfigField("meterCost", 0, IMeterGauge.FullMeter, "Meter each guarded block costs (hundredths of a percent). Without it, the block is a normal one."),
        new ModuleConfigField("drainPerFrame", 0, IMeterGauge.FullMeter, "Meter drained per frame of guarding in blockstun."),
        new ModuleConfigField("noChip", 0, 1, "1: guarded blocks take no chip damage. 0: chip as usual."));

    public void BeforeStateMachines(ModuleContext context)
    {
        ref readonly PushbackGuardConfig config = ref context.GetConfig<PushbackGuardConfig>();
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            InputWord input = context.State.Inputs[fighter].FramesAgo(0);
            int facing = context.State.Fighters[fighter].Facing;
            if (IsGuarding(input, in config) && NumpadDirection.IsBack(NumpadDirection.Of(input, facing)))
            {
                context.RequestMoveLock(fighter, new MoveLockRequest { Categories = Attacks });
            }
        }
    }

    public void AdjustBlock(ModuleContext context, in FighterContact contact, ref HitData hit)
    {
        ref readonly PushbackGuardConfig config = ref context.GetConfig<PushbackGuardConfig>();
        if (!IsGuardedBlock(context, contact.Defender, in config))
        {
            return;
        }

        hit = hit with
        {
            Pushback = IntMath.MulDiv(hit.Pushback, config.PushbackPermille, Permille),
            ChipDamage = config.NoChip != 0 ? 0 : hit.ChipDamage,
        };
    }

    // Asked again after the block: the meter hasn't changed since the adjustment, so this is the block that was guarded.
    public void OnBlock(ModuleContext context, in FighterContact contact)
    {
        ref readonly PushbackGuardConfig config = ref context.GetConfig<PushbackGuardConfig>();
        int defender = contact.Defender;
        if (!IsGuardedBlock(context, defender, in config))
        {
            return;
        }

        if (config.MeterCost > 0)
        {
            context.TrySpendMeter(defender, config.MeterCost);
        }

        context.RequestEvent(
            defender,
            new ModuleEventRequest { Module = Id, Code = GuardCode, Position = context.State.Fighters[defender].Position });
    }

    public void EndOfFrame(ModuleContext context)
    {
        ref readonly PushbackGuardConfig config = ref context.GetConfig<PushbackGuardConfig>();
        if (config.DrainPerFrame == 0)
        {
            return;
        }

        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            if (IsInBlockstun(context.State.Fighters[fighter].Action)
                && IsGuarding(context.State.Inputs[fighter].FramesAgo(0), in config))
            {
                int meter = context.MeterOf(fighter);
                context.TrySpendMeter(fighter, meter < config.DrainPerFrame ? meter : config.DrainPerFrame);
            }
        }
    }

    private static bool IsGuardedBlock(ModuleContext context, int defender, in PushbackGuardConfig config) =>
        IsGuarding(context.State.Inputs[defender].FramesAgo(0), in config)
        && (config.MeterCost == 0 || context.MeterOf(defender) >= config.MeterCost);

    private static bool IsGuarding(InputWord input, in PushbackGuardConfig config)
    {
        int held = 0;
        for (int bit = 0; bit < InputBits; bit++)
        {
            ushort button = (ushort)(1 << bit);
            if ((config.ButtonMask & button) != 0 && input.IsHeld(button))
            {
                held++;
            }
        }

        return held >= config.ButtonCount;
    }

    private static bool IsInBlockstun(FighterAction action) =>
        action is FighterAction.Blockstun or FighterAction.CrouchBlockstun or FighterAction.AirBlockstun;
}
