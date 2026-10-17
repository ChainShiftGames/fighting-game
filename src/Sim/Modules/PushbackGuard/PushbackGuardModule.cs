using Sim.Characters;
using Sim.Fighters.States;
using Sim.Input;
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
/// request), so pressing the buttons never starts a throw or a normal instead. On the core's block hook, for strikes
/// and projectiles alike: with the guard buttons held and the meter paid, the
/// defender's pushback slide is scaled (core request, <see cref="PushbackScaleRequest"/>), and presentation is told
/// (a module event). In the corner the longer slide pushes the attacker back instead, the core's corner rule. At the end
/// of each frame a fighter guarding in blockstun pays the drain. Meter goes through the core
/// (<see cref="ModuleContext.TrySpendMeter"/>), so with a cost it needs a meter module. Stateless.
/// </remarks>
public sealed class PushbackGuardModule : IMechanicModule, IBeforeStateMachinesHook, IOnBlockHook, IEndOfFrameHook
{
    /// <summary>The code of the module event a guarded block emits (<see cref="ModuleEventRequest.Code"/>).</summary>
    public const int GuardCode = 0;

    private const MoveCategory Attacks = MoveCategory.Normal | MoveCategory.Special | MoveCategory.Super | MoveCategory.Throw;
    private const int MaxButtonCount = 4;
    private const int InputBits = 16;

    // Task number of P3-15, so module tasks running in parallel don't pick the same id.
    public ModuleId Id => new(15);

    public string Name => "pushback_guard";

    public ModuleConfigLayout ConfigLayout { get; } = ModuleConfigLayout.For<PushbackGuardConfig>(
        new ModuleConfigField("buttonMask", 1, ushort.MaxValue),
        new ModuleConfigField("buttonCount", 1, MaxButtonCount),
        new ModuleConfigField("pushbackPermille", 0, PushbackScaleRequest.MaxPermille),
        new ModuleConfigField("meterCost", 0, IMeterGauge.FullMeter),
        new ModuleConfigField("drainPerFrame", 0, IMeterGauge.FullMeter));

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

    public void OnBlock(ModuleContext context, in FighterContact contact)
    {
        ref readonly PushbackGuardConfig config = ref context.GetConfig<PushbackGuardConfig>();
        int defender = contact.Defender;
        if (!IsGuarding(context.State.Inputs[defender].FramesAgo(0), in config))
        {
            return;
        }

        if (config.MeterCost > 0 && !context.TrySpendMeter(defender, config.MeterCost))
        {
            return;
        }

        context.RequestPushbackScale(defender, new PushbackScaleRequest { Permille = config.PushbackPermille });
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
