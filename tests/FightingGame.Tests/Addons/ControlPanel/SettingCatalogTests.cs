using System.Collections.Generic;
using System.IO;
using FightingGame.Addons.ControlPanel.Catalog;
using FightingGame.Addons.ControlPanel.Documents;
using Xunit;

namespace FightingGame.Tests.Addons.ControlPanel;

/// <summary>Every setting the panel lists is in the shipped files, with a value its range allows.</summary>
public sealed class SettingCatalogTests
{
    private static readonly PanelDocument _document = new(
        new PanelFolders(TestGameData.DataRoot, Path.Combine(TestGameData.RepositoryRoot, "game")));

    public static TheoryData<string> Pages => new() { "mechanics", "match", "characters", "effects" };

    [Theory]
    [MemberData(nameof(Pages))]
    public void EverySettingIsInItsFileAndInRange(string page)
    {
        foreach (SettingGroup group in GroupsOf(page))
        {
            if (group.Switch is { } toggle)
            {
                _document.Flag(toggle);
            }

            foreach (PanelSetting setting in group.Settings)
            {
                Assert.True(_document.Has(setting), $"{setting.File} has no {setting.Path}");
                if (setting.Kind is SettingKind.Whole or SettingKind.Decimal)
                {
                    double value = _document.Number(setting);
                    Assert.InRange(value, setting.Min, setting.Max);
                }
                else if (setting.Kind == SettingKind.Choice)
                {
                    Assert.Contains(_document.Choice(setting), setting.Choices);
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Pages))]
    public void EveryGroupAndSettingExplainsItself(string page)
    {
        var tooltips = new SettingTooltips(_document);
        foreach (SettingGroup group in GroupsOf(page))
        {
            Assert.False(group.Hint.Length == 0, $"group {group.Title} has no hint");
            foreach (PanelSetting setting in group.Switch is { } toggle ? [toggle, .. group.Settings] : group.Settings)
            {
                Assert.False(tooltips.For(setting).Length == 0, $"{group.Title}: {setting.Label} has no tooltip");
            }
        }
    }

    [Fact]
    public void EveryHitColumnExplainsItself()
    {
        var tooltips = new SettingTooltips(_document);
        HitRow row = HitFeelRows.For(_document, "testman")[0];

        foreach (HitColumn column in HitColumn.All)
        {
            Assert.False(tooltips.For(column.For(row)).Length == 0, $"{column.Label} has no tooltip");
        }
    }

    [Fact]
    public void TooltipsAddTheSchemaDescription()
    {
        var tooltips = new SettingTooltips(_document);
        PanelSetting timer = MatchRuleSettingList.All()[0].Settings[0];
        PanelSetting gravity = CharacterStatSettingList.Gravity("testman");
        var chip = PanelSetting.Whole("Chip", PanelFile.InData("characters/testman/moves/5c.json"), "hits[0].chipDamage", 0, 1);

        Assert.StartsWith(timer.Hint + "\n\n", tooltips.For(timer), System.StringComparison.Ordinal);
        Assert.Contains("5940", tooltips.SchemaDescription(timer), System.StringComparison.Ordinal);
        Assert.Contains("airborne frame", tooltips.SchemaDescription(gravity), System.StringComparison.Ordinal);
        Assert.Equal("Default 0; at most damage.", tooltips.SchemaDescription(chip));
    }

    [Fact]
    public void EveryMechanicsModuleWithADataFileHasAGroup()
    {
        int files = Directory.GetFiles(Path.Combine(TestGameData.DataRoot, "rules", "modules"), "*.json").Length;

        Assert.Equal(files, GroupsOf("mechanics").Count);
    }

    [Fact]
    public void LabelsAndUnitsComeFromKeys()
    {
        Assert.Equal("Roman cancel", SettingNames.FromId("roman_cancel"));
        Assert.Equal("Unblockable attacks", SettingNames.FromKey("unblockableAttacks"));
        Assert.Equal(SettingUnit.Frames, SettingNames.UnitOfKey("penaltyFrames"));
        Assert.Equal(SettingUnit.Permille, SettingNames.UnitOfKey("damageProrationPermille"));
        Assert.Equal(SettingUnit.None, SettingNames.UnitOfKey("hitGain"));
    }

    [Theory]
    [InlineData(SettingUnit.Frames, 5940, "99 s")]
    [InlineData(SettingUnit.Frames, 12, "")]
    [InlineData(SettingUnit.Subpixels, 150_000, "150 px")]
    [InlineData(SettingUnit.SubpixelsPerFrame, 4500, "4.5 px/f")]
    [InlineData(SettingUnit.Permille, 125, "12.5 %")]
    [InlineData(SettingUnit.None, 7, "")]
    public void UnitsReadInHumanTerms(SettingUnit unit, double value, string text) =>
        Assert.Equal(text, UnitReadout.Of(unit, value));

    private static IReadOnlyList<SettingGroup> GroupsOf(string page) => page switch
    {
        "mechanics" => MechanicSettingList.For(path => File.Exists(Path.Combine(TestGameData.DataRoot, path))),
        "match" => MatchRuleSettingList.All(),
        "characters" => CharacterStatSettingList.For("testman"),
        _ => EffectSettingList.All(),
    };
}
