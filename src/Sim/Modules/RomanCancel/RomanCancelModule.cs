using Sim.Characters;
using Sim.Fighters.States;
using Sim.Input;
using Sim.Rules;
using Sim.State;

namespace Sim.Modules.RomanCancel;

/// <summary>
/// Roman Cancel v1 (P3-18), Guilty Gear style: pressing the Roman Cancel buttons together spends meter and cancels
/// whatever the fighter is doing into neutral, so it can act at once, while a short freeze and a slowdown on the
/// opponent leave it plus. Which situations allow it (red, purple, blue, yellow) is a developer setting. Off, there is
/// no way to cancel a move's recovery.
/// </summary>
/// <remarks>
/// Runs after input, so its requests apply before this frame's state machines: a cancel into neutral
/// (<see cref="CancelRequest.Neutral"/>), the end of its own hitstop, a global freeze and the opponent's time scale,
/// then a module event whose
/// code is the variant. The freeze also keeps the input from starting a move: by the time the fighter runs again, the
/// presses are older than the button buffer. Not while hit, knocked down, thrown, frozen or with a cancel already
/// pending. Meter goes through the core (<see cref="ModuleContext.TrySpendMeter"/>), so with a cost it needs a meter
/// module. Stateless: it only reads the state and its config.
/// </remarks>
public sealed class RomanCancelModule : IMechanicModule, IAfterInputHook
{
    private const int MaxButtonCount = 4;
    private const int MaxPressWindow = 10;
    private const int MaxFreezeFrames = 60;
    private const int MaxSlowdownFrames = 180;
    private const int MaxSlowdownRatio = 10;
    private const int InputBits = 16;

    // Task number of P3-18, so module tasks running in parallel don't pick the same id.
    public ModuleId Id => new(18);

    public string Name => "roman_cancel";

    public ModuleConfigLayout ConfigLayout { get; } = ModuleConfigLayout.For<RomanCancelConfig>(
        new ModuleConfigField("buttonMask", 1, ushort.MaxValue),
        new ModuleConfigField("buttonCount", 1, MaxButtonCount),
        new ModuleConfigField("pressWindow", 1, MaxPressWindow),
        new ModuleConfigField("meterCost", 0, IMeterGauge.FullMeter),
        new ModuleConfigField("variants", 0, RomanCancelVariants.All),
        new ModuleConfigField("freezeFrames", 1, MaxFreezeFrames),
        new ModuleConfigField("slowdownFrames", 0, MaxSlowdownFrames),
        new ModuleConfigField("slowdownNumerator", 1, MaxSlowdownRatio),
        new ModuleConfigField("slowdownDenominator", 1, MaxSlowdownRatio));

    public void AfterInput(ModuleContext context)
    {
        ref readonly MatchState state = ref context.State;
        if (state.Freeze.FramesLeft > 0)
        {
            return;
        }

        ref readonly RomanCancelConfig config = ref context.GetConfig<RomanCancelConfig>();
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            ref readonly FighterState self = ref state.Fighters[fighter];
            int variant = VariantOf(in self);
            if (variant == 0 || (variant & config.Variants) == 0 || self.Cancel.IsPending != 0 || self.Health <= 0
                || !IsInput(in state.Inputs[fighter], in config))
            {
                continue;
            }

            if (config.MeterCost > 0 && !context.TrySpendMeter(fighter, config.MeterCost))
            {
                continue;
            }

            Cancel(context, fighter, variant, in self, in config);
        }
    }

    private void Cancel(ModuleContext context, int fighter, int variant, in FighterState self, in RomanCancelConfig config)
    {
        context.RequestCancel(fighter, new CancelRequest { TargetMove = CancelRequest.Neutral });
        // Out of its own hitstop at once: on contact the defender stays stuck, which is what leaves a red RC plus.
        context.RequestHitstopOverride(fighter, new HitstopOverrideRequest { Frames = 0 });
        context.RequestFreeze(new FreezeRequest { Frames = config.FreezeFrames });
        if (variant != RomanCancelVariants.Yellow && config.SlowdownFrames > 0
            && config.SlowdownNumerator < config.SlowdownDenominator)
        {
            context.RequestTimeScale(
                MatchState.PlayerCount - 1 - fighter,
                new TimeScaleRequest
                {
                    Numerator = config.SlowdownNumerator,
                    Denominator = config.SlowdownDenominator,
                    // Counted from the end of the freeze, so a longer freeze doesn't eat the slowdown.
                    Frames = config.FreezeFrames + config.SlowdownFrames,
                });
        }

        context.RequestEvent(
            fighter,
            new ModuleEventRequest { Module = Id, Code = BitIndex(variant), Position = self.Position });
    }

    // Which variant the fighter's situation is; 0 when it can't Roman Cancel (hit, knocked down, thrown).
    private static int VariantOf(in FighterState fighter) => fighter.Action switch
    {
        FighterAction.Attack => (fighter.MoveOutcome & (CancelOn.Hit | CancelOn.Block)) != 0
            ? RomanCancelVariants.Red
            : RomanCancelVariants.Purple,
        FighterAction.Blockstun or FighterAction.CrouchBlockstun or FighterAction.AirBlockstun => RomanCancelVariants.Yellow,
        FighterAction.Hitstun or FighterAction.CrouchHitstun or FighterAction.AirHitstun or FighterAction.Knockdown
            or FighterAction.WakeUp or FighterAction.Thrown or FighterAction.ThrowTech => 0,
        _ => RomanCancelVariants.Blue,
    };

    // At least ButtonCount of the buttons are held, one of them pressed this frame and all within the press window.
    private static bool IsInput(in InputHistory history, in RomanCancelConfig config)
    {
        int together = 0;
        bool pressedNow = false;
        for (int bit = 0; bit < InputBits; bit++)
        {
            ushort button = (ushort)(1 << bit);
            if ((config.ButtonMask & button) == 0 || !history.FramesAgo(0).IsHeld(button))
            {
                continue;
            }

            int heldFor = HeldFor(in history, button, config.PressWindow);
            pressedNow |= heldFor == 1;
            if (heldFor <= config.PressWindow)
            {
                together++;
            }
        }

        return pressedNow && together >= config.ButtonCount;
    }

    // Frames the button has been held, counting this one; more than the window means "too long ago".
    private static int HeldFor(in InputHistory history, ushort button, int window)
    {
        int frames = 1;
        while (frames <= window && history.FramesAgo(frames).IsHeld(button))
        {
            frames++;
        }

        return frames;
    }

    private static int BitIndex(int variant)
    {
        int index = 0;
        while ((variant >> index) != 1)
        {
            index++;
        }

        return index;
    }
}
