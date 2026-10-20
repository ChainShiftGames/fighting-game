using Sim.Math;
using Sim.Rules;
using Sim.State;

namespace Sim.Modules.CharacterGauge;

/// <summary>
/// A character's own resource gauge (ADR-0023): a character mechanic that keeps one gauge for its owner on a meter
/// channel of its config, filled over time and by the owner's hits and blocks. It spends nothing itself; other
/// mechanics (installs, ammo moves) read and spend it with <see cref="ModuleContext.MeterOf"/> /
/// <see cref="ModuleContext.TrySpendMeter"/> on that channel.
/// </summary>
/// <remarks>
/// Switched on by a character's data, not by the module lineup: <c>"mechanic": { "name": "character_gauge", … }</c>.
/// The core only routes the owner's channel here, so a question about the other fighter never arrives.
/// </remarks>
public sealed class CharacterGaugeModule : IMechanicModule, IAfterInputHook, IOnHitHook, IOnBlockHook, IMeterGauge
{
    // Number of ADR-0023, which added character mechanics.
    public ModuleId Id => new(23);

    public string Name => "character_gauge";

    public bool IsCharacterMechanic => true;

    public ModuleConfigLayout ConfigLayout { get; } = ModuleConfigLayout.For<CharacterGaugeConfig>(
        new ModuleConfigField("channel", 0, IMeterGauge.ChannelCount - 1, "Which meter channel this character's gauge is (ADR-0023)."),
        new ModuleConfigField("maxGauge", 1, 1_000_000, "A full gauge; gains stop there."),
        new ModuleConfigField("roundStartGauge", 0, 1_000_000, "Gauge at the start of each round."),
        new ModuleConfigField("gainPerFrame", 0, 1_000_000, "Gauge gained on every frame of a round being fought."),
        new ModuleConfigField("hitGain", 0, 1_000_000, "Gauge gained each time the owner's attack hits."),
        new ModuleConfigField("blockGain", 0, 1_000_000, "Gauge gained each time the owner's attack is blocked."),
        new ModuleConfigField("carryOverRounds", 0, 1, "1: the gauge carries over into the next round. 0: it starts fresh."));

    public void AfterInput(ModuleContext context)
    {
        ref CharacterGaugeState gauge = ref context.GetState<CharacterGaugeState>();
        ref readonly CharacterGaugeConfig config = ref context.GetConfig<CharacterGaugeConfig>();
        int round = context.State.Round.Round;
        if (round != gauge.Round)
        {
            // Round 1 is the match start: nothing to carry over yet.
            bool isFirstSetup = gauge.Round == 0;
            gauge.Round = round;
            if (isFirstSetup || config.CarryOverRounds == 0)
            {
                gauge.Gauge = IntMath.Clamp(config.RoundStartGauge, 0, config.MaxGauge);
            }
        }

        if (context.State.Round.Phase == RoundPhase.Fighting)
        {
            Add(ref gauge, config.GainPerFrame, in config);
        }
    }

    public void OnHit(ModuleContext context, in FighterContact contact) =>
        GainForAttack(context, in contact, context.GetConfig<CharacterGaugeConfig>().HitGain);

    public void OnBlock(ModuleContext context, in FighterContact contact) =>
        GainForAttack(context, in contact, context.GetConfig<CharacterGaugeConfig>().BlockGain);

    public int ChannelOf(ref readonly ModuleConfig config) =>
        ModuleConfigView.As<CharacterGaugeConfig>(in config).Channel;

    public int MeterOf(ModuleContext context, int fighter) => context.GetState<CharacterGaugeState>().Gauge;

    public bool TrySpend(ModuleContext context, int fighter, int amount)
    {
        ref CharacterGaugeState gauge = ref context.GetState<CharacterGaugeState>();
        if (gauge.Gauge < amount)
        {
            return false;
        }

        gauge.Gauge -= amount;
        return true;
    }

    private static void GainForAttack(ModuleContext context, in FighterContact contact, int amount)
    {
        if (contact.Attacker == context.Owner)
        {
            Add(ref context.GetState<CharacterGaugeState>(), amount, in context.GetConfig<CharacterGaugeConfig>());
        }
    }

    private static void Add(ref CharacterGaugeState gauge, int amount, in CharacterGaugeConfig config) =>
        gauge.Gauge = IntMath.Clamp(gauge.Gauge + amount, 0, config.MaxGauge);
}
