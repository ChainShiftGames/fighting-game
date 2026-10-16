namespace Sim.Modules;

/// <summary>Optional module hook: runs once each time a throw connects, during hit resolution.</summary>
public interface IOnThrowHook
{
    void OnThrow(ModuleContext context, in FighterContact contact);
}
