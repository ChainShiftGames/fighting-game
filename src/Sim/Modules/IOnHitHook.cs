namespace Sim.Modules;

/// <summary>Optional module hook: runs once each time a hit lands, during hit resolution.</summary>
public interface IOnHitHook
{
    void OnHit(ModuleContext context, in FighterContact contact);
}
