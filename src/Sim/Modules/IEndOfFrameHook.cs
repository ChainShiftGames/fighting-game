namespace Sim.Modules;

/// <summary>Optional module hook: runs at the end of the frame, after rounds and timer.</summary>
public interface IEndOfFrameHook
{
    void EndOfFrame(ModuleContext context);
}
