namespace Sim.Rules;

/// <summary>One lineup entry: which module runs in this slot and its config.</summary>
public struct ModuleSlot
{
    public ModuleId Module;
    public ModuleConfig Config;
}
