namespace Sim.Modules.AirBlock;

/// <summary>The air block module's settings in its <c>MatchRules</c> slot (developer settings).</summary>
public struct AirBlockConfig : IModuleConfig
{
    /// <summary>
    /// The attack kinds that can't be blocked in the air, as <see cref="AirBlockAttacks"/> bits. 0 (the default when
    /// the module is enabled without config): everything blockable can be air-blocked.
    /// </summary>
    public int UnblockableAttacks;
}
