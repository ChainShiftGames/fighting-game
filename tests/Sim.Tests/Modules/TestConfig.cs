using Sim.Modules;

namespace Sim.Tests.Modules;

/// <summary>Config view the test modules read from their <c>MatchRules</c> slot.</summary>
/// <remarks>Public because tests write its field only as raw config integers, which CS0649 can't see.</remarks>
public struct TestConfig : IModuleConfig
{
    public int Value;
}
