namespace Sim.Modules.CharacterGauge;

/// <summary>A character gauge's settings, from its owner's <c>character.json</c> <c>mechanic</c> (developer settings).</summary>
public struct CharacterGaugeConfig : IModuleConfig
{
    /// <summary>The meter channel the gauge serves for its owner (ADR-0023); other modules spend it there.</summary>
    public int Channel;

    /// <summary>A full gauge; gains stop there.</summary>
    public int MaxGauge;

    /// <summary>The gauge at the match start, and at each round start unless carried over.</summary>
    public int RoundStartGauge;

    /// <summary>Gauge gained on every frame of a round being fought.</summary>
    public int GainPerFrame;

    /// <summary>Gauge gained each time the owner's attack (or projectile) hits.</summary>
    public int HitGain;

    /// <summary>Gauge gained each time the owner's attack (or projectile) is blocked.</summary>
    public int BlockGain;

    /// <summary>1: the gauge carries over into the next round; 0: every round starts at <see cref="RoundStartGauge"/>.</summary>
    public int CarryOverRounds;
}
