using System;
using System.Collections.Generic;
using FightingGame.Addons.ControlPanel.Documents;
using Sim.Data.Rules;
using Xunit;

namespace FightingGame.Tests.Addons.ControlPanel;

public sealed class PanelEditStashTests : IDisposable
{
    private static readonly PanelSetting _timer =
        PanelSetting.Whole("Timer", PanelFile.InData(MatchRulesSchema.File), "roundTimerFrames", 1, 216_000);

    private static readonly PanelSetting _shake = PanelSetting.Decimal(
        "Hit shake", EffectSettingsCheck.File, "screenShake.hitPixels", 0, 40, 0.5);

    private readonly PanelTestFolders _folders = new();

    public void Dispose() => _folders.Dispose();

    [Fact]
    public void EditsComeBackInAFreshDocument()
    {
        var before = new PanelDocument(_folders.Folders);
        before.SetNumber(_timer, 3600);
        before.SetNumber(_shake, 7.5);

        Dictionary<string, string> stash = PanelEditStash.Take(before);
        var after = new PanelDocument(_folders.Folders);
        int restored = PanelEditStash.Restore(after, stash);

        Assert.Equal(2, restored);
        Assert.Equal((3600.0, 7.5), (after.Number(_timer), after.Number(_shake)));
        Assert.True(after.IsChanged(_timer));
    }

    [Fact]
    public void NothingUnsavedStashesNothing()
    {
        Assert.Empty(PanelEditStash.Take(new PanelDocument(_folders.Folders)));
    }

    [Fact]
    public void UnknownKeysAreSkipped()
    {
        var document = new PanelDocument(_folders.Folders);

        int restored = PanelEditStash.Restore(document, new Dictionary<string, string>
        {
            ["Data|rules/missing.json"] = "{}",
            ["NotAnArea|rules/match.json"] = "{}",
            ["no separator"] = "{}",
        });

        Assert.Equal(0, restored);
        Assert.False(document.HasChanges);
    }
}
