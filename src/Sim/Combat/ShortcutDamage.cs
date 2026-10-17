using Sim.Characters;
using Sim.Math;
using Sim.Rules;
using Sim.State;

namespace Sim.Combat;

/// <summary>
/// The special button shortcut's damage trade-off (ADR-0020): hits of a move the fighter started from its shortcut
/// deal <see cref="MatchRules.ShortcutDamagePercent"/> of their damage and chip.
/// </summary>
/// <remarks>Applied after the counter hit bonus, so the bonus is scaled too.</remarks>
public sealed class ShortcutDamage
{
    private const int Percent = 100;

    private readonly int _percent;

    public ShortcutDamage(in MatchRules rules)
    {
        // Rules built in code (tests, tools) leave it at 0: no trade-off.
        _percent = rules.ShortcutDamagePercent == 0 ? Percent : rules.ShortcutDamagePercent;
    }

    public HitData Apply(in HitData hit, in FighterState attacker) => attacker.IsShortcutMove == 0 || _percent == Percent
        ? hit
        : hit with
        {
            Damage = IntMath.MulDiv(hit.Damage, _percent, Percent),
            ChipDamage = IntMath.MulDiv(hit.ChipDamage, _percent, Percent),
        };
}
