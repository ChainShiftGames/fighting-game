namespace Sim.Data.Characters.Compiled;

/// <summary>How a hit must be blocked: mid (stand or crouch), low (crouch), overhead (stand), or not at all.</summary>
public enum Guard
{
    Mid,
    Low,
    Overhead,
    Unblockable,
}
