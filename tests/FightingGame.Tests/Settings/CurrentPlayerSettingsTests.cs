using System.Collections.Generic;
using FightingGame.Settings;
using Xunit;

namespace FightingGame.Tests.Settings;

public sealed class CurrentPlayerSettingsTests
{
    private readonly List<PlayerSettings> _applied = [];
    private readonly List<PlayerSettings> _saved = [];

    [Fact]
    public void TheLoadedSettingsApplyAtOnce()
    {
        var loaded = new PlayerSettings { Vsync = false };

        CurrentPlayerSettings settings = Create(loaded);

        Assert.Equal(loaded, settings.Value);
        Assert.Equal([loaded], _applied);
        Assert.Empty(_saved);
    }

    [Fact]
    public void ChangesApplyAtOnceButAreOnlyWrittenOnSave()
    {
        CurrentPlayerSettings settings = Create(new PlayerSettings());
        var quieter = new PlayerSettings { MasterVolume = 40 };

        settings.Change(quieter);

        Assert.Equal(quieter, _applied[^1]);
        Assert.Empty(_saved);

        settings.Save();
        settings.Save();

        Assert.Equal([quieter], _saved);
    }

    [Fact]
    public void SavingWithoutAChangeWritesNothing()
    {
        CurrentPlayerSettings settings = Create(new PlayerSettings());

        settings.Change(new PlayerSettings());
        settings.Save();

        Assert.Empty(_saved);
    }

    private CurrentPlayerSettings Create(PlayerSettings loaded) =>
        new(loaded, _saved.Add, [new RecordingApplier(_applied)]);

    private sealed class RecordingApplier(List<PlayerSettings> applied) : IPlayerSettingApplier
    {
        public void Apply(PlayerSettings settings) => applied.Add(settings);
    }
}
