using Sim.Fighters.States;
using Sim.Input;
using Sim.Math;
using Sim.Modules;
using Sim.Pipeline;
using Sim.Rules;
using Sim.State;

namespace Scenarios.Tests.Runner;

/// <summary>Builds a match from a scenario's setup: match rules, the tick pipeline and the start state.</summary>
/// <remarks>
/// Stand-in for the production match composition (data + <see cref="MatchRules"/> → pipeline and start state),
/// which doesn't exist yet. Gameplay stages plug in here as they are built, and round start (P1-13) will replace
/// the start positions; the format and the expectation kinds don't change for that.
/// </remarks>
public sealed class ScenarioMatch
{
    /// <summary>Test start: each fighter 150 px from the centre, facing the other; a setup line can change it.</summary>
    public static readonly int StartDistanceFromCentre = Subpixels.FromPixels(150);

    private const int FacingRight = 1;
    private const int FacingLeft = -1;

    private readonly TickPipeline _pipeline;
    private readonly StateMachineStage _stateMachines;

    public ScenarioMatch(ScenarioSetup setup)
    {
        Setup = setup;
        MatchRules rules = BuildRules(setup);
        CoreRequests requests = new();
        ModuleLineup modules = new(RegisteredModules.Create(), in rules.Modules, requests);
        // ButtonPressMoveInput is P1-08's stand-in until the motion parser and button buffer (P1-06).
        _stateMachines = new StateMachineStage(new ButtonPressMoveInput(), setup.Character(0), setup.Character(1));
        TickStages stages = new()
        {
            Input = new ButtonSetInputStage(setup.Buttons),
            StateMachines = _stateMachines,
        };
        _pipeline = new TickPipeline(stages, modules, requests);
    }

    public ScenarioSetup Setup { get; }

    public MatchState StartState()
    {
        MatchState state = default;
        state.Round.Round = 1;
        PlaceFighter(ref state.Fighters[0], -StartDistanceFromCentre, FacingRight, Setup.Character(0).Stats.Health);
        PlaceFighter(ref state.Fighters[1], StartDistanceFromCentre, FacingLeft, Setup.Character(1).Stats.Health);
        _stateMachines.EnterIdle(ref state);
        foreach (SetupValue value in Setup.Values)
        {
            value.Kind.Set(ref state, value.Player, value.Value);
        }

        return state;
    }

    /// <summary>Runs one frame; inputs are absolute words, as the logic reads them.</summary>
    public void Tick(ref MatchState state, InputWord player1, InputWord player2) =>
        _pipeline.Tick(ref state, player1, player2);

    private static MatchRules BuildRules(ScenarioSetup setup)
    {
        MatchRules rules = default;
        for (int slot = 0; slot < setup.Modules.Count; slot++)
        {
            EnabledModule module = setup.Modules[slot];
            rules.Modules[slot].Module = module.Id;
            for (int i = 0; i < module.Config.Length; i++)
            {
                rules.Modules[slot].Config[i] = module.Config[i];
            }
        }

        return rules;
    }

    private static void PlaceFighter(ref FighterState fighter, int x, int facing, int health)
    {
        fighter.Position = new IntVector2(x, 0);
        fighter.Facing = facing;
        fighter.Health = health;
    }
}
