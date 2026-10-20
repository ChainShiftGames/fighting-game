using Sim.Combat;
using Sim.Fighters.States;
using Sim.Input;
using Sim.Rules;
using Sim.State;

namespace Sim.Modules.InstantBlock;

/// <summary>
/// Instant block (P3-16): blocking with a fresh press of back, just before the attack connects, shortens the
/// blockstun and tells presentation (a module event), so a well-timed block is rewarded. Off, every block is equal.
/// </summary>
/// <remarks>
/// Runs on the core's block hook, for strikes and projectiles, ground and air blocks alike. It reads the defender's
/// input history: the run of frames back has been held up to the block (<see cref="InstantBlockConfig.WindowFrames"/>)
/// and, before that run, the lockout frames that must be free of back so mashing doesn't count. Back is away from the
/// attacker now, as the core's own block check reads it. The reward goes through core requests (stun override and
/// module event), applied at the end of the frame. Stateless: it only reads the state and its config.
/// </remarks>
public sealed class InstantBlockModule : IMechanicModule, IOnBlockHook
{
    /// <summary>The code of the module event an instant block emits (<see cref="ModuleEventRequest.Code"/>).</summary>
    public const int InstantBlockCode = 0;

    /// <summary>Window plus lockout must fit the input history the module reads.</summary>
    public const int MaxHistoryFrames = InputHistory.Capacity / 2;

    // Only keeps data sane: no blockstun in the game comes near it.
    private const int MaxBlockstunReduction = 60;

    public ModuleId Id => new(16);

    public string Name => "instant_block";

    public ModuleConfigLayout ConfigLayout { get; } = ModuleConfigLayout.For<InstantBlockConfig>(
        new ModuleConfigField("windowFrames", 1, MaxHistoryFrames, "Back must be pressed at most this many frames before the hit (1 = on the frame it connects) for an instant block."),
        new ModuleConfigField("lockoutFrames", 0, MaxHistoryFrames, "Anti-mash: no instant block if back was also held this many frames before the press. 0 = no lockout."),
        new ModuleConfigField("blockstunReduction", 0, MaxBlockstunReduction, "Blockstun frames an instant block takes off."));

    public void OnBlock(ModuleContext context, in FighterContact contact)
    {
        ref readonly MatchState state = ref context.State;
        ref readonly FighterState defender = ref state.Fighters[contact.Defender];
        if (!IsInBlockstun(defender.Action))
        {
            // Chip damage knocked it out: there is no blockstun to reward.
            return;
        }

        ref readonly InstantBlockConfig config = ref context.GetConfig<InstantBlockConfig>();
        int toward = Blocking.TowardAttacker(in defender, in state.Fighters[contact.Attacker]);
        if (!IsInstant(in state.Inputs[contact.Defender], toward, in config))
        {
            return;
        }

        int stun = defender.StunFramesLeft - config.BlockstunReduction;
        context.RequestStunOverride(contact.Defender, new StunOverrideRequest { Frames = stun > 0 ? stun : 0 });
        context.RequestEvent(
            contact.Defender,
            new ModuleEventRequest { Module = Id, Code = InstantBlockCode, Position = defender.Position });
    }

    private static bool IsInstant(in InputHistory history, int toward, in InstantBlockConfig config)
    {
        int held = 0;
        while (held <= config.WindowFrames && IsBack(history.FramesAgo(held), toward))
        {
            held++;
        }

        if (held > config.WindowFrames)
        {
            return false;
        }

        for (int framesAgo = held; framesAgo < held + config.LockoutFrames; framesAgo++)
        {
            if (IsBack(history.FramesAgo(framesAgo), toward))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsBack(InputWord input, int toward) => NumpadDirection.IsBack(NumpadDirection.Of(input, toward));

    private static bool IsInBlockstun(FighterAction action) =>
        action is FighterAction.Blockstun or FighterAction.CrouchBlockstun or FighterAction.AirBlockstun;
}
