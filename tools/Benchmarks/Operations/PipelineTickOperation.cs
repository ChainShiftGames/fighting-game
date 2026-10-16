using Sim.Modules;
using Sim.Pipeline;
using Sim.Rules;
using Sim.State;

namespace Benchmarks.Operations;

/// <summary>One frame through the real tick pipeline (P1-03); gameplay stages are filled in by later tasks.</summary>
internal struct PipelineTickOperation : IBenchmarkOperation
{
    private readonly TickPipeline _pipeline;
    private readonly InputSequence _inputs;
    private MatchState _state;

    public PipelineTickOperation(InputSequence inputs)
    {
        MatchRules rules = default;
        var requests = new CoreRequests();
        var modules = new ModuleLineup(RegisteredModules.Create(), in rules.Modules, requests);
        _pipeline = new TickPipeline(new TickStages(), modules, requests);
        _inputs = inputs;
        _state = default;
    }

    public readonly string Name => "Tick (pipeline)";

    public void Run()
    {
        _pipeline.Tick(ref _state, _inputs.Player1At(_state.Frame), _inputs.Player2At(_state.Frame));
    }
}
