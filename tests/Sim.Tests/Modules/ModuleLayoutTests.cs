using System;
using System.Runtime.CompilerServices;
using Sim.Modules;
using Sim.Rules;
using Sim.State;
using Sim.Tests.Rules;
using Xunit;

namespace Sim.Tests.Modules;

/// <summary>Every module's state and config struct fits its slice and is ints only (no padding).</summary>
/// <remarks>Found by reflection, so each new module is covered without naming it here (removal test).</remarks>
public sealed class ModuleLayoutTests
{
    [Fact]
    public void ModuleStatesFitTheirSliceWithoutPadding()
    {
        AssertEveryLayout(typeof(IModuleState), typeof(ModuleStateSlice));
    }

    [Fact]
    public void ModuleConfigsFitTheirSlotWithoutPadding()
    {
        AssertEveryLayout(typeof(IModuleConfig), typeof(ModuleConfig));
    }

    private static void AssertEveryLayout(Type marker, Type storage)
    {
        foreach (Type type in typeof(IMechanicModule).Assembly.GetTypes())
        {
            if (type == marker || !marker.IsAssignableFrom(type))
            {
                continue;
            }

            IntFieldLayout.Walk(type);
            Assert.True(SizeOf(type) <= SizeOf(storage), $"{type} is larger than its {storage.Name}.");
        }
    }

    private static int SizeOf(Type type) =>
        (int)typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf))!.MakeGenericMethod(type).Invoke(null, null)!;
}
