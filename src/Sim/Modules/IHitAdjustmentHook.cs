using Sim.Characters;

namespace Sim.Modules;

/// <summary>Optional module question: how much a hit about to land does, or whether it whiffs (combo limits, P3-12).</summary>
/// <remarks>
/// The core asks during hit resolution for every fighter attack and projectile about to hit (not blocks or throws),
/// after the counter hit bonus and before the defender takes it. Modules answer in slot order, each adjusting the
/// previous one's result; returning false makes the hit whiff: it doesn't connect, as if it had missed. A negative
/// damage, hitstun or hitstop is a module bug and throws. A question, not a hook point: the answer must depend only
/// on the contact, the hit, the read-only state and the config, and the module must not change its state slice or
/// make requests here (its hit hook runs once the hit has landed).
/// </remarks>
public interface IHitAdjustmentHook
{
    bool AdjustHit(ModuleContext context, in FighterContact contact, ref HitData hit);
}
