using Sim.Modules;

namespace Sim.Tests.Modules;

/// <summary>A config with two values, for checking config layouts.</summary>
public struct TwoValueConfig : IModuleConfig
{
    public int First;
    public int Second;
}
