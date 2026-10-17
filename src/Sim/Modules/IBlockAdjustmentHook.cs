using Sim.Characters;

namespace Sim.Modules;

/// <summary>Optional module question: what a block about to happen does (pushback guard, P3-15).</summary>
/// <remarks>
/// The core asks during hit resolution for every fighter attack and projectile about to be blocked, before the defender
/// takes the chip, blockstun, hitstop and pushback. Modules answer in slot order, each adjusting the previous one's
/// result; a block always stays a block. A negative value is a module bug and throws. A question, not a hook point:
/// the answer must depend only on the contact, the hit, the read-only state (meter included) and the config, and the
/// module must not change its state slice or make requests here (its block hook runs once the block has happened).
/// </remarks>
public interface IBlockAdjustmentHook
{
    void AdjustBlock(ModuleContext context, in FighterContact contact, ref HitData hit);
}
