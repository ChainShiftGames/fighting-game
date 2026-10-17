using Sim.Camera;
using Sim.Characters;
using Sim.Collision;
using Sim.Combat;
using Sim.Entities;
using Sim.Fighters.Movement;
using Sim.Fighters.States;
using Sim.Input;
using Sim.Input.Motions;
using Sim.Match;
using Sim.Modules;
using Sim.Pipeline;
using Sim.Rules;
using Sim.State;
using Sim.Throws;

namespace Sim.Composition;

/// <summary>One match composed from data: two characters, the input data and the match rules → pipeline and start state.</summary>
/// <remarks>
/// The one place the gameplay stages are put together, for every match type (game, scenarios, SyncTest, tools).
/// A new gameplay stage plugs in here. Built once at match start; <see cref="Tick"/> allocates nothing.
/// </remarks>
public sealed class MatchSimulation
{
    private const int FirstRound = 1;

    private readonly TickPipeline _pipeline;
    private readonly RoundStart _roundStart;

    /// <param name="player1">P1's character.</param>
    /// <param name="player2">P2's character.</param>
    /// <param name="buttons">The button set (ADR-0010).</param>
    /// <param name="motions">The motion set, in priority order.</param>
    /// <param name="movement">Which motions dash, backdash and high jump.</param>
    /// <param name="rules">The match rules, with the values from data applied.</param>
    public MatchSimulation(
        CharacterData player1,
        CharacterData player2,
        ButtonSet buttons,
        MotionSet motions,
        MovementInputs movement,
        in MatchRules rules)
    {
        Player1 = player1;
        Player2 = player2;
        Rules = rules;
        CoreRequests requests = new();
        ModuleLineup modules = new(RegisteredModules.Create(), in rules.Modules, requests);
        MotionParser parser = new(motions, in rules);
        _roundStart = new RoundStart(player1, player2, in rules);
        StateMachineStage stateMachines = new(
            new MotionMoveInput(parser, movement, player1),
            new MotionMoveInput(parser, movement, player2),
            player1,
            player2,
            modules,
            rules.ShortcutCooldownFrames,
            rules.AirRecovery != 0);
        HitContacts contacts = new();
        EntityContacts entityContacts = new();
        OwnerCharacters owners = new(player1, player2);
        ProjectileSpawner spawner = new(owners, in rules);
        CounterHits counterHits = new(in rules);
        ThrowContacts throwContacts = new();
        Guarding guarding = new(modules);
        Pushback pushback = new(rules.PushbackFrames);

        // Entities (P1-21) run after the fighters in each step. Module spawn requests apply after the end-of-frame
        // hooks, so they lead the round and timer step (SpawnRequestStage). Throws (P1-23) resolve before strikes, so
        // a throw can beat a strike on the same frame, and running holds come first of all.
        TickStages stages = new()
        {
            Input = new RoundInputLock(new ButtonSetInputStage(buttons)),
            StateMachines = stateMachines,
            Movement = new StageSequence(
                new MovementStage(player1, player2),
                new EntityMovementStage(owners),
                new MoveSpawnStage(owners, spawner),
                new ThrowProtectionStage(player1, player2, in rules)),
            Collision = new StageSequence(
                new PushbackStage(player1, player2, StageWalls.From(in rules)),
                new CollisionStage(player1, player2, StageWalls.From(in rules)),
                new CameraStage(player1, player2, CameraFraming.From(in rules))),
            HitDetection = new StageSequence(
                new HitDetectionStage(player1, player2, contacts),
                new ThrowDetectionStage(player1, player2, throwContacts),
                new EntityHitDetectionStage(owners, entityContacts, contacts)),
            HitResolution = new StageSequence(
                new ThrowHoldStage(
                    player1,
                    player2,
                    new ThrowTechInput(player1, rules.ButtonBuffer),
                    new ThrowTechInput(player2, rules.ButtonBuffer),
                    modules),
                new ThrowResolutionStage(player1, player2, throwContacts, contacts, in rules),
                new HitResolutionStage(
                    player1,
                    player2,
                    contacts,
                    modules,
                    rules.TradeRule,
                    counterHits,
                    new ShortcutDamage(in rules),
                    guarding,
                    pushback),
                new EntityHitResolutionStage(
                    owners,
                    entityContacts,
                    contacts,
                    modules,
                    rules.TradeRule,
                    counterHits,
                    guarding,
                    pushback,
                    new ShortcutDamage(in rules))),
            RoundTimer = new StageSequence(
                new SpawnRequestStage(owners, spawner, requests),
                new RoundStage(_roundStart, new RoundJudge(player1, player2), new MatchScore(in rules), in rules)),
        };
        _pipeline = new TickPipeline(stages, modules, requests);
    }

    public CharacterData Player1 { get; }

    public CharacterData Player2 { get; }

    public MatchRules Rules { get; }

    /// <summary>Round 1 (<see cref="RoundStart"/>): fighters around the centre facing each other, full health, idle.</summary>
    public MatchState StartState()
    {
        MatchState state = default;
        _roundStart.Begin(ref state, FirstRound);
        return state;
    }

    /// <summary>Runs one frame with both players' raw input words.</summary>
    public void Tick(ref MatchState state, InputWord player1, InputWord player2) =>
        _pipeline.Tick(ref state, player1, player2);
}
