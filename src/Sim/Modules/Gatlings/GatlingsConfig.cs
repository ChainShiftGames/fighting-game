namespace Sim.Modules.Gatlings;

/// <summary>The gatlings module's settings in its <c>MatchRules</c> slot (developer settings).</summary>
public struct GatlingsConfig : IModuleConfig
{
    /// <summary>
    /// The cancel kinds the match doesn't allow, as <see cref="GatlingCancels"/> bits, even where character data
    /// opens a window for them. 0 (the default when the module is enabled without config): every data cancel works.
    /// </summary>
    public int ClosedCancels;
}
