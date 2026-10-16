using Sim.Characters;
using Sim.Collision;
using Sim.Fighters.Movement;
using Sim.Fighters.States;
using Sim.Input;
using Sim.Input.Motions;
using Sim.Math;
using Sim.Modules;
using Sim.Pipeline;
using Sim.Rules;
using Sim.State;

namespace Sim.Composition;

/// <summary>One match composed from data: two characters, the input data and the match rules → pipeline and start state.</summary>
/// <remarks>
/// The one place the gameplay stages are put together, for every match type (game, scenarios, SyncTest, tools).
/// A new gameplay stage plugs in here. Built once at match start; <see cref="Tick"/> allocates nothing.
/// </remarks>
public sealed class MatchSimulation
{
    private const int FacingRight = 1;
    private const int FacingLeft = -1;

    private readonly TickPipeline _pipeline;
    private readonly StateMachineStage _stateMachines;

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
        _stateMachines = new StateMachineStage(
            new MotionMoveInput(parser, movement, player1),
            new MotionMoveInput(parser, movement, player2),
            player1,
            player2);
        TickStages stages = new()
        {
            Input = new ButtonSetInputStage(buttons),
            StateMachines = _stateMachines,
            Movement = new MovementStage(player1, player2),
            Collision = new CollisionStage(player1, player2, StageWalls.From(in rules)),
        };
        _pipeline = new TickPipeline(stages, modules, requests);
    }

    public CharacterData Player1 { get; }

    public CharacterData Player2 { get; }

    public MatchRules Rules { get; }

    /// <summary>Round 1: fighters placed symmetrically around the centre, facing each other, full health, idle.</summary>
    public MatchState StartState()
    {
        MatchState state = default;
        state.Round.Round = 1;
        Place(ref state.Fighters[0], -Rules.RoundStartDistance, FacingRight, Player1.Stats.Health);
        Place(ref state.Fighters[1], Rules.RoundStartDistance, FacingLeft, Player2.Stats.Health);
        _stateMachines.EnterIdle(ref state);
        return state;
    }

    /// <summary>Runs one frame with both players' raw input words.</summary>
    public void Tick(ref MatchState state, InputWord player1, InputWord player2) =>
        _pipeline.Tick(ref state, player1, player2);

    private static void Place(ref FighterState fighter, int x, int facing, int health)
    {
        fighter.Position = new IntVector2(x, 0);
        fighter.Facing = facing;
        fighter.Health = health;
    }
}
