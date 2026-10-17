namespace Sim.Modules.Burst;

/// <summary>The burst module's settings in its <c>MatchRules</c> slot (developer settings).</summary>
/// <remarks>All gauge values share one unit, so the cost and the gains read as shares of <see cref="MaxGauge"/>.</remarks>
public struct BurstConfig : IModuleConfig
{
    /// <summary>A full gauge; gains stop there.</summary>
    public int MaxGauge;

    /// <summary>Gauge each fighter has on the match's first frame (at most <see cref="MaxGauge"/>).</summary>
    public int StartGauge;

    /// <summary>Gauge each fighter gains on every frame of a round being fought.</summary>
    public int GainPerFrame;

    /// <summary>Gauge a fighter gains each time a hit lands on it.</summary>
    public int GainWhenHit;

    /// <summary>Gauge one burst needs and spends.</summary>
    public int BurstCost;

    /// <summary>1: usable in hitstun (standing, crouching, air); 0: not.</summary>
    public int UsableInHitstun;

    /// <summary>1: usable in blockstun (standing, crouching, air); 0: not.</summary>
    public int UsableInBlockstun;

    /// <summary>1: usable from a free action (idle, crouch, walk, jump); 0: not.</summary>
    public int UsableInNeutral;

    /// <summary>
    /// The input word's button bits that trigger a burst when they become held together on one frame (bit 4 is the
    /// first button in <c>data/input/buttons.json</c>). The characters' burst moves use the same buttons.
    /// </summary>
    public int TriggerButtons;

    /// <summary>The burst's cancel priority against other modules' cancels for the same fighter and frame.</summary>
    public int CancelPriority;
}
