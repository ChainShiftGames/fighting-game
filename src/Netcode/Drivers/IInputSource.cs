using Sim.Input;

namespace Netcode.Drivers;

/// <summary>Plays one player that no one on this PC controls: a script, seeded random inputs or a bot.</summary>
public interface IInputSource
{
    /// <summary>The input word for the next logic frame; called exactly once per frame that runs.</summary>
    InputWord Next();
}
