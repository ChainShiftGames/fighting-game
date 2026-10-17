using Sim.Modules;

namespace Sim.Data.Tests.Modules;

/// <summary>The config of the meter gauges in <see cref="MoveRoleCheckTests"/>: just the channel they serve.</summary>
public struct ChannelTestConfig : IModuleConfig
{
    public int Channel;
}
