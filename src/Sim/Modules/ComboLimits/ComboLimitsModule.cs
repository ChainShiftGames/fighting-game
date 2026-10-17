using Sim.Characters;
using Sim.Fighters.States;
using Sim.Rules;
using Sim.State;

namespace Sim.Modules.ComboLimits;

/// <summary>
/// Combo limits (P3-12): the longer a combo runs, the less each hit does. Damage proration and hitstun decay scale
/// each hit by the defender's earlier hits in the combo, gravity scaling makes a juggled defender fall faster, and
/// the OTG rule caps hits on a knocked-down defender. All tunable in <see cref="ComboLimitsConfig"/>. Off (not in
/// the lineup), every hit does its full data and anything that touches a knocked-down hurtbox hits.
/// </summary>
/// <remarks>
/// A combo is the hits a fighter takes from being hit (or thrown) until it can act again: it lasts while the fighter
/// is in hitstun, air hitstun, knockdown or being thrown, as the state machines leave it each frame. Blocks don't
/// count. Scaling reaches every hit that goes through hit resolution, fighter attacks and projectiles alike
/// (<see cref="IHitAdjustmentHook"/>); throws count as combo hits but their own damage isn't scaled. Gravity goes
/// through the core's gravity request, so it applies from the frame after a hit and resets the frame after the
/// combo ends. Both fighters are tracked the same way, each in its own part of the state slice.
/// </remarks>
public sealed class ComboLimitsModule
    : IMechanicModule, IAfterStateMachinesHook, IHitAdjustmentHook, IOnHitHook, IOnThrowHook
{
    public ModuleId Id => new(12);

    public string Name => "combo_limits";

    public ModuleConfigLayout ConfigLayout { get; } = ModuleConfigLayout.For<ComboLimitsConfig>(
        new ModuleConfigField("damageProrationPermille", 0, 1000),
        new ModuleConfigField("minimumDamagePermille", 0, 1000),
        new ModuleConfigField("hitstunDecayFrames", 0, 999),
        new ModuleConfigField("maximumHitstunDecayFrames", 0, 999),
        new ModuleConfigField("minimumHitstunFrames", 0, 999),
        new ModuleConfigField("gravityScalingPermille", 0, 10000),
        new ModuleConfigField("maximumGravityPermille", 0, 10000),
        new ModuleConfigField("otgHits", 0, 99));

    /// <summary>Ends the combo of a fighter that can act again, and notes who is knocked down for the OTG rule.</summary>
    public void AfterStateMachines(ModuleContext context)
    {
        ref ComboLimitsState state = ref context.GetState<ComboLimitsState>();
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            FighterAction action = context.State.Fighters[fighter].Action;
            ref DefenderCombo combo = ref state[fighter];
            if (!IsInCombo(action) && combo.Hits > 0)
            {
                combo = default;
                context.RequestGravityScale(fighter, new GravityScaleRequest { ExtraPermille = 0 });
            }

            combo.IsDown = action == FighterAction.Knockdown ? 1 : 0;
        }
    }

    public bool AdjustHit(ModuleContext context, in FighterContact contact, ref HitData hit)
    {
        ref readonly ComboLimitsConfig config = ref context.GetConfig<ComboLimitsConfig>();
        DefenderCombo combo = context.GetState<ComboLimitsState>()[contact.Defender];
        if (combo.IsDown != 0 && combo.OtgHits >= config.OtgHits)
        {
            return false;
        }

        hit = hit with
        {
            Damage = ComboScaling.Damage(hit.Damage, combo.Hits, in config),
            Hitstun = ComboScaling.Hitstun(hit.Hitstun, combo.Hits, in config),
        };
        return true;
    }

    public void OnHit(ModuleContext context, in FighterContact contact)
    {
        ref DefenderCombo combo = ref context.GetState<ComboLimitsState>()[contact.Defender];
        if (combo.IsDown != 0)
        {
            combo.OtgHits++;
        }

        Count(context, ref combo, contact.Defender);
    }

    public void OnThrow(ModuleContext context, in FighterContact contact) =>
        Count(context, ref context.GetState<ComboLimitsState>()[contact.Defender], contact.Defender);

    // One more hit in the combo: the defender falls faster, and is down now if the hit knocked it down.
    private static void Count(ModuleContext context, ref DefenderCombo combo, int defender)
    {
        combo.Hits++;
        combo.IsDown = context.State.Fighters[defender].Action == FighterAction.Knockdown ? 1 : 0;
        ref readonly ComboLimitsConfig config = ref context.GetConfig<ComboLimitsConfig>();
        int extraGravity = ComboScaling.ExtraGravityPermille(combo.Hits, in config);
        context.RequestGravityScale(defender, new GravityScaleRequest { ExtraPermille = extraGravity });
    }

    private static bool IsInCombo(FighterAction action) => action is FighterAction.Hitstun
        or FighterAction.CrouchHitstun or FighterAction.AirHitstun or FighterAction.Knockdown or FighterAction.Thrown;
}
