using Sim.Characters;

namespace Sim.Modules.Gatlings;

/// <summary>The cancel kinds <see cref="GatlingsConfig.ClosedCancels"/> can rule out, one bit each.</summary>
/// <remarks>Integers rather than a flags enum, because module configs hold only ints.</remarks>
public static class GatlingCancels
{
    /// <summary>Into a normal: the chain routes (gatlings) of the character's data.</summary>
    public const int Chains = 1 << 0;

    /// <summary>Into a special move.</summary>
    public const int Specials = 1 << 1;

    /// <summary>Into a super.</summary>
    public const int Supers = 1 << 2;

    /// <summary>Into a throw.</summary>
    public const int Throws = 1 << 3;

    /// <summary>Into a jump: a prejump, or a double jump in the air.</summary>
    public const int Jumps = 1 << 4;

    /// <summary>Every kind: the largest mask the config accepts.</summary>
    public const int All = Chains | Specials | Supers | Throws | Jumps;

    /// <summary>The kind bit of the cancel a fighter wants to take.</summary>
    public static int Of(in CancelCheck check)
    {
        if (check.IsJump)
        {
            return Jumps;
        }

        return check.IntoCategory switch
        {
            MoveCategory.Normal => Chains,
            MoveCategory.Special => Specials,
            MoveCategory.Super => Supers,
            MoveCategory.Throw => Throws,
            _ => 0,
        };
    }
}
