namespace Sim.Modules.Install;

/// <summary>An install's settings, from its owner's <c>character.json</c> <c>mechanic</c> (developer settings).</summary>
public struct InstallConfig : IModuleConfig
{
    /// <summary>The install ends on this many frames after its move starts, unless a hit ends it first.</summary>
    public int DurationFrames;
}
