using Sim.Modules;

namespace Sim.Data.Tests.Modules;

/// <summary>The config of <see cref="LineupTestModule"/>: two values, so their order in the slot is checked.</summary>
public struct LineupTestConfig : IModuleConfig
{
    public int Strength;
    public int Window;
}
