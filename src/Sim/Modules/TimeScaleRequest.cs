namespace Sim.Modules;

/// <summary>Core request: run one fighter at an integer speed ratio for a while (e.g. Roman Cancel slowdown).</summary>
public struct TimeScaleRequest
{
    /// <summary>The fighter advances <see cref="Numerator"/> frames per <see cref="Denominator"/> match frames.</summary>
    public int Numerator;

    public int Denominator;

    /// <summary>Match frames the ratio lasts.</summary>
    public int Frames;
}
