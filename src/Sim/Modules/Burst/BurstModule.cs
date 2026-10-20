using System.Collections.Generic;
using Sim.Input;
using Sim.Input.Motions;
using Sim.Rules;
using Sim.State;

namespace Sim.Modules.Burst;

/// <summary>
/// Burst (P3-17): a combo breaker on its own gauge, apart from any meter. Pressing the trigger buttons together with
/// enough gauge, in a situation the config allows (hitstun by default), spends the gauge and cancels the fighter into
/// its burst move, whose data pushes the attacker away (invulnerable, a knockdown hit and a wide pushbox).
/// </summary>
/// <remarks>
/// The gauge fills every frame of a round being fought and each time the fighter is hit, and carries over between
/// rounds. The press counts on its own frame only, also in hitstop: the cancel then waits until the fighter acts
/// again. The burst move's own data input is the same buttons, listed after the character's normals and throws, so
/// from neutral one of those always comes out first and only this module starts a burst.
/// </remarks>
public sealed class BurstModule : IMechanicModule, IAfterInputHook, IOnHitHook
{
    private const int AllButtons = (1 << InputWord.BitCount) - (1 << InputWord.FirstButtonBit);

    // Index in MoveRoles.
    private const int BurstRole = 0;

    public ModuleId Id => new(17);

    public string Name => "burst";

    /// <summary>Each character's <c>burst</c> move: the move a burst cancels into.</summary>
    public IReadOnlyList<string> MoveRoles { get; } = ["burst"];

    public ModuleConfigLayout ConfigLayout { get; } = ModuleConfigLayout.For<BurstConfig>(
        new ModuleConfigField("maxGauge", 1, 1_000_000, "A full burst gauge; gains stop there."),
        new ModuleConfigField("startGauge", 0, 1_000_000, "Burst gauge each fighter has when the match starts."),
        new ModuleConfigField("gainPerFrame", 0, 1_000_000, "Burst gauge gained on every frame of a round being fought."),
        new ModuleConfigField("gainWhenHit", 0, 1_000_000, "Burst gauge a fighter gains each time a hit lands on them."),
        new ModuleConfigField("burstCost", 1, 1_000_000, "Burst gauge one burst needs and spends."),
        new ModuleConfigField("usableInHitstun", 0, 1, "1: can burst while in hitstun (a combo breaker). 0: not."),
        new ModuleConfigField("usableInBlockstun", 0, 1, "1: can burst while blocking. 0: not."),
        new ModuleConfigField("usableInNeutral", 0, 1, "1: can burst from idle, walking or jumping. 0: not."),
        new ModuleConfigField("triggerButtons", 1 << InputWord.FirstButtonBit, AllButtons, "Button bits: A 16, B 32, C 64, D 128, Special 256, Dash 512; add them up (240 = A+B+C+D). Pressing them together on one frame bursts."),
        new ModuleConfigField("cancelPriority", 0, 1_000_000, "Which wins when another mechanic cancels the same fighter on the same frame; higher wins."));

    public void AfterInput(ModuleContext context)
    {
        ref BurstState state = ref context.GetState<BurstState>();
        ref readonly BurstConfig config = ref context.GetConfig<BurstConfig>();
        if (state.IsStarted == 0)
        {
            state.IsStarted = 1;
            for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
            {
                state.Fighters[fighter].Gauge = Gain(0, config.StartGauge, in config);
            }
        }

        bool isFighting = context.State.Round.Phase == RoundPhase.Fighting;
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            ref BurstFighter burst = ref state.Fighters[fighter];
            if (isFighting)
            {
                burst.Gauge = Gain(burst.Gauge, config.GainPerFrame, in config);
            }

            if (WantsBurst(in context.State, fighter, burst.Gauge, in config))
            {
                burst.Gauge -= config.BurstCost;
                burst.BurstsUsed++;
                context.RequestCancel(
                    fighter, new CancelRequest { TargetMove = context.RoleMove(fighter, BurstRole), Priority = config.CancelPriority });
            }
        }
    }

    public void OnHit(ModuleContext context, in FighterContact contact)
    {
        ref BurstFighter burst = ref context.GetState<BurstState>().Fighters[contact.Defender];
        ref readonly BurstConfig config = ref context.GetConfig<BurstConfig>();
        burst.Gauge = Gain(burst.Gauge, config.GainWhenHit, in config);
    }

    private static bool WantsBurst(in MatchState match, int fighter, int gauge, in BurstConfig config) =>
        gauge >= config.BurstCost &&
        BurstSituation.Allows(match.Fighters[fighter].Action, in config) &&
        ButtonPress.IsUnusedPressAt(in match.Inputs[fighter], config.TriggerButtons, 0);

    private static int Gain(int gauge, int amount, in BurstConfig config)
    {
        int gained = gauge + amount;
        return gained < config.MaxGauge ? gained : config.MaxGauge;
    }
}
