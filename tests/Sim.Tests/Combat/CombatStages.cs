using System;
using System.Runtime.InteropServices;
using Sim.Characters;
using Sim.Combat;
using Sim.Modules;
using Sim.Pipeline;
using Sim.Rules;
using Sim.State;
using Sim.Tests.Modules;

namespace Sim.Tests.Combat;

/// <summary>Hit detection then hit resolution for two <see cref="CombatCharacter"/>s, with a counting module enabled.</summary>
internal sealed class CombatStages
{
    public const int ModuleSlot = 0;

    private readonly HitDetectionStage _detection;
    private readonly HitResolutionStage _resolution;

    /// <param name="counterHit">The match rules' default counter hit bonus; none by default.</param>
    /// <param name="punishCounter">The match rules' default punish counter bonus; none by default.</param>
    public CombatStages(
        TradeRule tradeRule = TradeRule.Trade, CounterBonus counterHit = default, CounterBonus punishCounter = default)
    {
        CharacterData character = CombatCharacter.Create();
        Contacts = new HitContacts();
        MatchRules rules = default;
        rules.CounterHit = counterHit;
        rules.PunishCounter = punishCounter;
        rules.Modules[ModuleSlot].Module = new ModuleId(1);
        var lineup = new ModuleLineup(new ModuleCatalog([new CountingModule(1)]), in rules.Modules, new CoreRequests());
        _detection = new HitDetectionStage(character, character, Contacts);
        _resolution = new HitResolutionStage(
            character, character, Contacts, lineup, tradeRule, new CounterHits(in rules), new Guarding(lineup));
    }

    public HitContacts Contacts { get; }

    /// <summary>What the counting module saw: hook calls and the last contact.</summary>
    public static CountingState Hooks(ref MatchState state)
    {
        Span<int> slice = state.Modules[ModuleSlot];
        return MemoryMarshal.Read<CountingState>(MemoryMarshal.AsBytes(slice));
    }

    public void Run(ref MatchState state)
    {
        _detection.Run(ref state, FrameActivity.Everyone);
        _resolution.Run(ref state, FrameActivity.Everyone);
    }

    public void Detect(ref MatchState state) => _detection.Run(ref state, FrameActivity.Everyone);
}
