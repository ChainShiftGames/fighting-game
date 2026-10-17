using System;
using System.Runtime.InteropServices;
using Sim.Characters;
using Sim.Combat;
using Sim.Entities;
using Sim.Modules;
using Sim.Pipeline;
using Sim.Rules;
using Sim.State;
using Sim.Tests.Modules;

namespace Sim.Tests.Entities;

/// <summary>
/// The entity stages in their tick steps, with the fighters' hit stages around them and a counting module enabled,
/// for two <see cref="ProjectileCharacter"/>s.
/// </summary>
internal sealed class EntityStages
{
    public const int ModuleSlot = 0;

    private readonly EntityMovementStage _movement;
    private readonly MoveSpawnStage _moveSpawns;
    private readonly ITickStage _detection;
    private readonly ITickStage _resolution;
    private readonly SpawnRequestStage _spawnRequests;

    /// <param name="counterHit">The match rules' default counter hit bonus; none by default.</param>
    public EntityStages(
        int entitiesPerPlayer = 4, TradeRule tradeRule = TradeRule.Trade, CounterBonus counterHit = default)
    {
        CharacterData character = ProjectileCharacter.Create();
        MatchRules rules = default;
        rules.EntitiesPerPlayer = entitiesPerPlayer;
        rules.CounterHit = counterHit;
        rules.Modules[ModuleSlot].Module = new ModuleId(1);
        var lineup = new ModuleLineup(new ModuleCatalog([new CountingModule(1)]), in rules.Modules, Requests);
        var owners = new OwnerCharacters(character, character);
        var contacts = new HitContacts();
        var entityContacts = new EntityContacts();
        var counterHits = new CounterHits(in rules);
        var guarding = new Guarding(lineup);
        Spawner = new ProjectileSpawner(owners, in rules);
        _movement = new EntityMovementStage(owners);
        _moveSpawns = new MoveSpawnStage(owners, Spawner);
        _detection = new StageSequence(
            new HitDetectionStage(character, character, contacts),
            new EntityHitDetectionStage(owners, entityContacts, contacts));
        _resolution = new StageSequence(
            new HitResolutionStage(
                character, character, contacts, lineup, tradeRule, counterHits, new ShortcutDamage(in rules), guarding,
                new Pushback(rules.PushbackFrames)),
            new EntityHitResolutionStage(
                owners,
                entityContacts,
                contacts,
                lineup,
                tradeRule,
                counterHits,
                guarding,
                new Pushback(rules.PushbackFrames),
                new ShortcutDamage(in rules)));
        _spawnRequests = new SpawnRequestStage(owners, Spawner, Requests);
    }

    public CoreRequests Requests { get; } = new();

    public ProjectileSpawner Spawner { get; }

    /// <summary>What the counting module saw: hook calls and the last contact.</summary>
    public static CountingState Hooks(ref MatchState state)
    {
        Span<int> slice = state.Modules[ModuleSlot];
        return MemoryMarshal.Read<CountingState>(MemoryMarshal.AsBytes(slice));
    }

    public static int Count(in MatchState state)
    {
        int count = 0;
        for (int slot = 0; slot < EntityPool.Capacity; slot++)
        {
            count += state.Entities.Slots[slot].IsActive;
        }

        return count;
    }

    /// <summary>One frame of the entity work, in tick order; the frame counter advances like the pipeline's.</summary>
    public void Tick(ref MatchState state) => Tick(ref state, FrameActivity.Everyone);

    public void Tick(ref MatchState state, in FrameActivity activity)
    {
        Move(ref state, in activity);
        Hit(ref state);
        _spawnRequests.Run(ref state, in activity);
        state.Frame++;
    }

    public void Move(ref MatchState state, in FrameActivity activity)
    {
        _movement.Run(ref state, in activity);
        _moveSpawns.Run(ref state, in activity);
    }

    public void Hit(ref MatchState state)
    {
        _detection.Run(ref state, FrameActivity.Everyone);
        _resolution.Run(ref state, FrameActivity.Everyone);
    }
}
