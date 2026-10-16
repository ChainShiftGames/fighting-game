namespace Sim.Modules;

/// <summary>Who touched whom in a hit, block or throw, as player indices (0 = P1, 1 = P2).</summary>
/// <remarks>Hit resolution (P1-12) adds the details modules need, such as the move.</remarks>
public readonly struct FighterContact
{
    public FighterContact(int attacker, int defender)
    {
        Attacker = attacker;
        Defender = defender;
    }

    public int Attacker { get; }
    public int Defender { get; }
}
