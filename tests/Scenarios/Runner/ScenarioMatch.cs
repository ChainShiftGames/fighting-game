using Sim.Characters;
using Sim.Fighters.States;
using Sim.Input;
using Sim.Math;
using Sim.Modules;
using Sim.Pipeline;
using Sim.Rules;
using Sim.State;

namespace Scenarios.Tests.Runner;

/// <summary>Composes a match: two characters, the button set and the match rules → tick pipeline and start state.</summary>
/// <remarks>
/// Self-contained on purpose (only Sim types, everything through the constructor): it is the stand-in for the
/// production match composition and is meant to move to src/ for the game (P1-15) and SyncTest (P1-19). Gameplay
/// stages plug in here as they are built.
/// </remarks>
public sealed class ScenarioMatch
{
    private const int FacingRight = 1;
    private const int FacingLeft = -1;

    private readonly CharacterData _player1;
    private readonly CharacterData _player2;
    private readonly MatchStartSettings _start;
    private readonly TickPipeline _pipeline;
    private readonly StateMachineStage _stateMachines;

    public ScenarioMatch(
        CharacterData player1, CharacterData player2, ButtonSet buttons, in MatchRules rules, MatchStartSettings start)
    {
        _player1 = player1;
        _player2 = player2;
        _start = start;
        CoreRequests requests = new();
        ModuleLineup modules = new(RegisteredModules.Create(), in rules.Modules, requests);
        // ButtonPressMoveInput is P1-08's stand-in until the motion parser and button buffer (P1-06).
        _stateMachines = new StateMachineStage(new ButtonPressMoveInput(), player1, player2);
        TickStages stages = new()
        {
            Input = new ButtonSetInputStage(buttons),
            StateMachines = _stateMachines,
        };
        _pipeline = new TickPipeline(stages, modules, requests);
    }

    /// <summary>Round 1: fighters placed symmetrically around the centre, facing each other, full health, idle.</summary>
    public MatchState StartState()
    {
        MatchState state = default;
        state.Round.Round = 1;
        Place(ref state.Fighters[0], -_start.StartDistanceFromCentre, FacingRight, _player1.Stats.Health);
        Place(ref state.Fighters[1], _start.StartDistanceFromCentre, FacingLeft, _player2.Stats.Health);
        _stateMachines.EnterIdle(ref state);
        return state;
    }

    /// <summary>Runs one frame; inputs are absolute words, as the logic reads them.</summary>
    public void Tick(ref MatchState state, InputWord player1, InputWord player2) =>
        _pipeline.Tick(ref state, player1, player2);

    private static void Place(ref FighterState fighter, int x, int facing, int health)
    {
        fighter.Position = new IntVector2(x, 0);
        fighter.Facing = facing;
        fighter.Health = health;
    }
}
