using System;
using System.Collections.Generic;
using System.IO;
using FightingGame.Addons.ControlPanel.Documents;
using Sim.Data.Rules;
using Xunit;

namespace FightingGame.Tests.Addons.ControlPanel;

public sealed class PanelSaverTests : IDisposable
{
    private static readonly PanelFile _rules = PanelFile.InData(MatchRulesSchema.File);
    private static readonly PanelSetting _timer = PanelSetting.Whole("Timer", _rules, "roundTimerFrames", 1, 216_000);
    private static readonly PanelSetting _roundsToWin = PanelSetting.Whole("Rounds to win", _rules, "roundsToWin", 1, 10);
    private static readonly PanelSetting _shake = PanelSetting.Decimal(
        "Hit shake", EffectSettingsCheck.File, "screenShake.hitPixels", 0, 40, 0.5);

    private readonly PanelTestFolders _folders = new();
    private readonly PanelDocument _document;
    private readonly PanelSaver _saver;

    public PanelSaverTests()
    {
        _document = new PanelDocument(_folders.Folders);
        _saver = new PanelSaver(_document, [new GameDataCheck(_folders.Folders.DataRoot), new EffectSettingsCheck()]);
    }

    public void Dispose() => _folders.Dispose();

    [Fact]
    public void AnEditStaysInMemoryUntilSaved()
    {
        string onDisk = File.ReadAllText(_folders.Folders.FullPath(_rules));

        _document.SetNumber(_timer, 3600);

        Assert.True(_document.IsChanged(_timer));
        Assert.Equal(3600, _document.Number(_timer));
        Assert.Equal(onDisk, File.ReadAllText(_folders.Folders.FullPath(_rules)));
    }

    [Fact]
    public void SavingWritesOnlyTheChangedValue()
    {
        string before = File.ReadAllText(_folders.Folders.FullPath(_rules));
        double old = _document.Number(_timer);
        _document.SetNumber(_timer, 3600);

        Assert.Empty(_saver.Save());

        string after = File.ReadAllText(_folders.Folders.FullPath(_rules));
        Assert.Equal(before.Replace($"\"roundTimerFrames\": {old}", "\"roundTimerFrames\": 3600"), after);
        Assert.False(_document.HasChanges);
    }

    [Fact]
    public void PuttingAValueBackDropsTheFileFromTheChanges()
    {
        double old = _document.Number(_timer);
        _document.SetNumber(_timer, old + 60);

        _document.SetNumber(_timer, old);

        Assert.False(_document.HasChanges);
    }

    [Fact]
    public void RevertingOneSettingKeepsTheOthersInItsFile()
    {
        _document.SetNumber(_timer, 3600);
        _document.SetNumber(_roundsToWin, 1);

        _document.Revert(_timer);

        Assert.False(_document.IsChanged(_timer));
        Assert.True(_document.IsChanged(_roundsToWin));
    }

    [Fact]
    public void ValuesAreKeptInRangeAndRoundedAsStored()
    {
        _document.SetNumber(_timer, 999_999);
        _document.SetNumber(_shake, 2.123456);

        Assert.Equal(216_000, _document.Number(_timer));
        Assert.Equal(2.123, _document.Number(_shake));
    }

    [Fact]
    public void DataTheGameRejectsIsNotSaved()
    {
        string before = File.ReadAllText(_folders.Folders.FullPath(_rules));
        _document.SetNumber(_roundsToWin, 10);

        IReadOnlyList<string> problems = _saver.Save();

        Assert.Contains(problems, problem => problem.Contains("maxRounds", StringComparison.Ordinal));
        Assert.Equal(before, File.ReadAllText(_folders.Folders.FullPath(_rules)));
        Assert.True(_document.HasChanges);
    }

    [Fact]
    public void AnInvalidEffectFileIsNotSaved()
    {
        var heavy = PanelSetting.Whole("Heavy", EffectSettingsCheck.File, "hitSparks.heavyHitstop", 1, 60);
        var light = PanelSetting.Whole("Light", EffectSettingsCheck.File, "hitSparks.lightHitstop", 0, 60);
        _document.SetNumber(light, _document.Number(heavy));

        Assert.Contains(_saver.Save(), problem => problem.Contains("heavyHitstop", StringComparison.Ordinal));
    }

    [Fact]
    public void AFileChangedOnDiskSinceItWasReadIsNotOverwritten()
    {
        _document.SetNumber(_timer, 3600);
        string path = _folders.Folders.FullPath(_rules);
        File.WriteAllText(path, File.ReadAllText(path).Replace("\"roundsToWin\": 2", "\"roundsToWin\": 3"));

        IReadOnlyList<string> problems = _saver.Save();

        Assert.Single(problems);
        Assert.Contains("changed on disk", problems[0], StringComparison.Ordinal);
        Assert.Contains("\"roundsToWin\": 3", File.ReadAllText(path), StringComparison.Ordinal);
    }

    [Fact]
    public void AnEffectSettingSaves()
    {
        _document.SetNumber(_shake, 6.5);

        Assert.Empty(_saver.Save());
        Assert.Equal(6.5, new PanelDocument(_folders.Folders).Number(_shake));
    }
}
