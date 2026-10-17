using System.IO;
using System.Linq;
using FightingGame.Debug;
using Xunit;

namespace FightingGame.Tests.Debug;

/// <summary>The panel's module settings read cleanly from the real <c>data/rules/modules/</c> files (P3-34).</summary>
public sealed class ModuleSettingListTests
{
    [Fact]
    public void ListsASwitchForEveryModuleWithADataFile()
    {
        (DataFileFlag[] switches, _) = ModuleSettingList.For(TestGameData.DataRoot);
        string[] files = Directory.GetFiles(Path.Combine(TestGameData.DataRoot, "rules", "modules"), "*.json");

        Assert.Equal(files.Length, switches.Length);
        Assert.Contains(switches, flag => flag.Name == "Module meter");
        Assert.DoesNotContain(switches, flag => flag.Name.Contains("contract_probe", System.StringComparison.Ordinal));
        foreach (DataFileFlag flag in switches)
        {
            flag.Read(TestGameData.DataRoot);
        }
    }

    [Fact]
    public void EveryConfigValueReadsWithinItsRange()
    {
        (_, DataFileSetting[] values) = ModuleSettingList.For(TestGameData.DataRoot);

        Assert.Contains(values, value => value.Name == "meter: superCost");
        foreach (DataFileSetting value in values)
        {
            Assert.InRange(value.Read(TestGameData.DataRoot), value.Min, value.Max);
        }
    }

    [Fact]
    public void SwitchingAModuleRewritesOnlyItsEnabledFlag()
    {
        string root = Directory.CreateTempSubdirectory().FullName;
        string modules = Directory.CreateDirectory(Path.Combine(root, "rules", "modules")).FullName;
        string source = Path.Combine(TestGameData.DataRoot, "rules", "modules", "meter.json");
        string copy = Path.Combine(modules, "meter.json");
        File.Copy(source, copy);
        DataFileFlag meter = ModuleSettingList.For(root).Switches.Single();

        meter.Write(root, !meter.Read(root));

        Assert.NotEqual(File.ReadAllText(source), File.ReadAllText(copy));
        meter.Write(root, !meter.Read(root));
        Assert.Equal(File.ReadAllText(source), File.ReadAllText(copy));
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void TheEmbeddedDataOffersNoModuleSettings()
    {
        (DataFileFlag[] switches, DataFileSetting[] values) = ModuleSettingList.For(null);

        Assert.Empty(switches);
        Assert.Empty(values);
    }
}
