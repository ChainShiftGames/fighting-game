namespace Sim.Modules;

/// <summary>Core request: set and clear some of one fighter's flags (ADR-0023), e.g. an install starting or running out.</summary>
/// <remarks>
/// Masks come from <see cref="ModuleContext.FlagMask"/>. Clears apply before sets; requests from several modules
/// apply in slot order.
/// </remarks>
public struct FlagsRequest
{
    public int Set;
    public int Clear;
}
