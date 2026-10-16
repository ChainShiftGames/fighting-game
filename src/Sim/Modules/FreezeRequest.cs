namespace Sim.Modules;

/// <summary>Core request: freeze the whole match except the exempted fighters (super flash, Roman Cancel).</summary>
public struct FreezeRequest
{
    public int Frames;

    /// <summary>Bit i set: fighter i keeps running during the freeze.</summary>
    public int ExemptFighters;
}
