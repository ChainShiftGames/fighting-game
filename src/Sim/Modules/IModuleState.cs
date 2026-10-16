namespace Sim.Modules;

/// <summary>Marks a module's state struct, laid over its slice of <c>MatchState</c>; int fields only.</summary>
/// <remarks>Int fields only keep the slice free of padding, so the checksum stays deterministic; a test checks it.</remarks>
public interface IModuleState;
