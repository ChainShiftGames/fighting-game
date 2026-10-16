namespace Sim.State;

/// <summary>A global freeze (super flash, Roman Cancel freeze): only exempted fighters keep running.</summary>
public struct FreezeState
{
    /// <summary>Frames the freeze still lasts, counting the current one; 0 when nothing is frozen.</summary>
    public int FramesLeft;

    /// <summary>Bit i set: fighter i keeps running during the freeze.</summary>
    public int ExemptFighters;
}
