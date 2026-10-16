namespace Sim.Modules;

/// <summary>Marks a module's config struct, laid over its slot config in <c>MatchRules</c>; int fields only.</summary>
/// <remarks>Int fields only keep the rules hash deterministic; a test checks it.</remarks>
public interface IModuleConfig;
