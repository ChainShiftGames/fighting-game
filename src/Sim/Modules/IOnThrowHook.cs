namespace Sim.Modules;

/// <summary>Optional module hook: runs once each time a throw lands (untouched by a tech), in hit resolution.</summary>
public interface IOnThrowHook
{
    void OnThrow(ModuleContext context, in FighterContact contact);
}
