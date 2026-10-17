using System;
using System.Runtime.InteropServices;
using Sim.Rules;

namespace Sim.Modules;

/// <summary>Views a slot's raw <see cref="ModuleConfig"/> as the module's own config struct.</summary>
public static class ModuleConfigView
{
    /// <summary>The config as <typeparamref name="TConfig"/>; throws if the struct doesn't fit.</summary>
    public static ref readonly TConfig As<TConfig>(ref readonly ModuleConfig config)
        where TConfig : unmanaged, IModuleConfig
    {
        ReadOnlySpan<int> values = config;
        return ref MemoryMarshal.AsRef<TConfig>(MemoryMarshal.AsBytes(values));
    }
}
