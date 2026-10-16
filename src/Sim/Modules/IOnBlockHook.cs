namespace Sim.Modules;

/// <summary>Optional module hook: runs once each time an attack is blocked, during hit resolution.</summary>
public interface IOnBlockHook
{
    void OnBlock(ModuleContext context, in FighterContact contact);
}
