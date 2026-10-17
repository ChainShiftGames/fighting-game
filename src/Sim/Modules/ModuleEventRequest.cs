using Sim.Math;
using Sim.Rules;

namespace Sim.Modules;

/// <summary>Core request: tell presentation what a module did, as a <c>FrameEventKind.Module</c> event.</summary>
/// <remarks>
/// The event's detail packs the module and its own code (<see cref="DetailOf"/>), so presentation can tell modules
/// apart without the core naming any of them. The event belongs to the frame the request is made in.
/// </remarks>
public struct ModuleEventRequest
{
    /// <summary>Codes each module may use: 0 up to this minus 1.</summary>
    public const int CodesPerModule = 1000;

    /// <summary>The requesting module's own id.</summary>
    public ModuleId Module;

    /// <summary>What happened, in the module's own numbering.</summary>
    public int Code;

    /// <summary>World position in subpixels, e.g. where an effect appears.</summary>
    public IntVector2 Position;

    /// <summary>The event detail for one of a module's codes.</summary>
    public static int DetailOf(ModuleId module, int code) => (module.Value * CodesPerModule) + code;

    /// <summary>The module a module event's detail came from.</summary>
    public static ModuleId ModuleOf(int detail) => new(detail / CodesPerModule);

    /// <summary>The module's own code in a module event's detail.</summary>
    public static int CodeOf(int detail) => detail % CodesPerModule;
}
