using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FightingGame.Input;
using Sim.Data.Input;
using Sim.Data.Sources;
using Sim.Data.Validation;
using Sim.Input;
using Xunit;

namespace FightingGame.Tests.Input;

/// <summary>Button sets and bindings the tests share.</summary>
internal static class TestButtons
{
    /// <summary>The shipped button set, loaded from the repository's data/ folder.</summary>
    public static ButtonSet Repository { get; } = LoadRepositoryButtons();

    /// <summary>A five-button set, to show nothing in the device layer assumes the shipped buttons.</summary>
    public static ButtonSet FiveButtons { get; } = new(
    [
        new ButtonDefinition("punch", 4),
        new ButtonDefinition("kick", 5),
        new ButtonDefinition("slash", 6),
        new ButtonDefinition("heavy", 7),
        new ButtonDefinition("dust", 12),
    ]);

    public static ushort Mask(this ButtonSet buttons, string id)
    {
        Assert.True(buttons.TryFind(id, out int button), $"no button '{id}'");
        return buttons.MaskOf(button);
    }

    /// <summary>The shipped default bindings, parsed against the shipped buttons.</summary>
    public static PlayerBindings[] Defaults()
    {
        var parser = new BindingsParser(Repository, DevBindingsFile.PlayerCount);
        PlayerBindings[]? bindings = parser.Parse(DefaultBindings.ReadJson());
        Assert.True(bindings is not null, string.Join(Environment.NewLine, parser.Errors));
        return bindings;
    }

    private static ButtonSet LoadRepositoryButtons()
    {
        string root = typeof(TestButtons).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "RepositoryRoot").Value!;
        var errors = new DataErrors();
        ButtonSet? buttons = ButtonSetLoader.Load(new DirectoryDataSource(Path.Combine(root, "data")), errors);
        errors.ThrowIfAny();
        return buttons!;
    }
}
