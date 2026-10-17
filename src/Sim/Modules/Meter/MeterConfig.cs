namespace Sim.Modules.Meter;

/// <summary>The meter module's settings in its <c>MatchRules</c> slot (developer settings); meter in hundredths of a percent.</summary>
public struct MeterConfig : IModuleConfig
{
    /// <summary>What a super costs (5000 = 50 %); with less, supers can't be started.</summary>
    public int SuperCost;

    /// <summary>Meter the attacker gains each time its attack (or projectile) hits.</summary>
    public int HitGain;

    /// <summary>Meter the attacker gains each time its attack (or projectile) is blocked.</summary>
    public int BlockGain;

    /// <summary>Meter gained on each frame the fighter ends further forward (towards where it faces) than it began.</summary>
    public int ForwardGain;

    /// <summary>Both fighters' meter when the match starts, and at each round start unless carried over.</summary>
    public int RoundStartMeter;

    /// <summary>1: meter carries over into the next round; 0: every round starts at <see cref="RoundStartMeter"/>.</summary>
    public int CarryOverRounds;

    /// <summary>The meter channel this gauge serves (ADR-0023); 0 is the main meter other modules spend.</summary>
    public int Channel;
}
