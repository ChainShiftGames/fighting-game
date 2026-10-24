using System.Collections.Generic;
using FightingGame.Settings;
using FightingGame.Tests.UI.Framework;
using FightingGame.UI.Framework;
using FightingGame.UI.Settings;
using Xunit;

namespace FightingGame.Tests.UI.Settings;

public sealed class SettingsScreenTests
{
    private const int Audio = 1;
    private const int Controls = 3;
    private const int Online = 4;
    private const long KeepMilliseconds = 10_000;

    private readonly List<PlayerSettings> _saved = [];
    private readonly CurrentPlayerSettings _settings;
    private readonly SettingsScreen _screen;
    private long _now = 1000;
    private int _closed;

    public SettingsScreenTests()
    {
        _settings = new CurrentPlayerSettings(new PlayerSettings(), _saved.Add, []);
        IReadOnlyList<SettingsTab> tabs = SettingsTabs.Create(WindowSizeChoices.For(1920, 1080, _settings.Value));
        _screen = new SettingsScreen(
            tabs, _settings, new SettingsMenuTiming(KeepMilliseconds), () => _now, isOverMatch: false, () => _closed++);
    }

    [Fact]
    public void TabLeftAndRightWrapAroundTheTabs()
    {
        _screen.Press(new MenuPress(MenuAction.TabLeft, MenuDevice.Keyboard));
        Assert.Equal(6, _screen.TabIndex);

        _screen.Press(new MenuPress(MenuAction.TabRight, MenuDevice.Keyboard));
        _screen.Press(new MenuPress(MenuAction.TabRight, MenuDevice.Keyboard));
        Assert.Equal(Audio, _screen.TabIndex);
    }

    [Fact]
    public void ThePickerAtTheTopAlsoSwitchesTabsSoAPadWithoutShouldersGetsEverywhere()
    {
        Assert.True(_screen.Items[0].Act(MenuAction.Right));

        Assert.Equal(Audio, _screen.TabIndex);
    }

    [Fact]
    public void AChangeAppliesAtOnceAndIsSavedOnLeaving()
    {
        Assert.True(Item("MENU_SETTINGS_VSYNC").Act(MenuAction.Confirm));

        Assert.False(_settings.Value.Vsync);
        Assert.Empty(_saved);

        Assert.False(_screen.Back());

        Assert.Equal([new PlayerSettings { Vsync = false }], _saved);
        Assert.Equal(1, _closed);
    }

    [Fact]
    public void VolumesMoveInStepsWithinTheirRange()
    {
        ShowTab(Audio);

        Item("MENU_SETTINGS_VOLUME_EFFECTS").Act(MenuAction.Left);
        Item("MENU_SETTINGS_VOLUME_EFFECTS").Act(MenuAction.Left);
        Item("MENU_SETTINGS_VOLUME_MASTER").Act(MenuAction.Right);

        Assert.Equal(90, _settings.Value.EffectsVolume);
        Assert.Equal(100, _settings.Value.MasterVolume);
    }

    [Fact]
    public void TheInputDelayGoesFromAutoThroughEachFixedDelayBackToAuto()
    {
        ShowTab(Online);
        var seen = new List<int?>();
        for (int i = 0; i < 6; i++)
        {
            seen.Add(_settings.Value.InputDelayOverrideFrames);
            Item("MENU_SETTINGS_INPUT_DELAY").Act(MenuAction.Right);
        }

        Assert.Equal([null, 0, 1, 2, 3, 4], seen);
        Assert.Null(_settings.Value.InputDelayOverrideFrames);
    }

    [Fact]
    public void AWindowModeChangeAsksToKeepItAndRevertsWhenTimeRunsOut()
    {
        Item("MENU_SETTINGS_SCREEN_MODE").Act(MenuAction.Right);

        Assert.Equal(ScreenMode.Borderless, _settings.Value.ScreenMode);
        IMenuScreen question = Assert.IsType<KeepSettingScreen>(_screen.Over);
        Assert.Equal("Going back to the previous setting in 10 s.", Format(question.Body[0]));

        _now += KeepMilliseconds - 1;
        _screen.Follow();
        Assert.Equal("Going back to the previous setting in 1 s.", Format(question.Body[0]));
        Assert.Equal(ScreenMode.Borderless, _settings.Value.ScreenMode);

        _now++;
        _screen.Follow();

        Assert.Equal(ScreenMode.Windowed, _settings.Value.ScreenMode);
        Assert.Null(_screen.Over);
    }

    [Fact]
    public void KeepingTheChangeStopsTheCountdown()
    {
        Item("MENU_SETTINGS_SCREEN_MODE").Act(MenuAction.Right);

        _screen.Over!.Items[0].Act(MenuAction.Confirm);
        _now += KeepMilliseconds * 2;
        _screen.Follow();

        Assert.Equal(ScreenMode.Borderless, _settings.Value.ScreenMode);
        Assert.Null(_screen.Over);
    }

    [Fact]
    public void BackOnTheQuestionRevertsAndLeavingWithItOpenRevertsBeforeSaving()
    {
        Item("MENU_SETTINGS_WINDOW_SIZE").Act(MenuAction.Right);
        Assert.False(_screen.Over!.Back());
        Assert.Equal(new PlayerSettings(), _settings.Value);

        Item("MENU_SETTINGS_SCREEN_MODE").Act(MenuAction.Left);
        _screen.Back();

        Assert.Equal(new PlayerSettings(), _settings.Value);
        Assert.Empty(_saved);
    }

    [Fact]
    public void TheWindowSizeCanOnlyChangeWhileWindowed()
    {
        Assert.True(Item("MENU_SETTINGS_WINDOW_SIZE").Enabled);

        _settings.Change(new PlayerSettings { ScreenMode = ScreenMode.Fullscreen });

        MenuItem size = Item("MENU_SETTINGS_WINDOW_SIZE");
        Assert.False(size.Enabled);
        Assert.Equal("MENU_SETTINGS_WINDOW_SIZE_WINDOWED_ONLY", size.Description.Value);
    }

    [Fact]
    public void ResetAsksFirstAndOnlyResetsItsOwnTab()
    {
        _settings.Change(new PlayerSettings { Vsync = false, MusicVolume = 10, VoiceVolume = 20 });
        ShowTab(Audio);

        Item("MENU_SETTINGS_RESET").Act(MenuAction.Confirm);
        ResetTabScreen question = Assert.IsType<ResetTabScreen>(_screen.Over);
        Assert.Equal(1, question.FirstFocus);
        Assert.Equal("Reset every Audio setting to its default?", Format(question.Body[0]));

        question.Items[1].Act(MenuAction.Confirm);
        Assert.Null(_screen.Over);
        Assert.Equal(10, _settings.Value.MusicVolume);

        Item("MENU_SETTINGS_RESET").Act(MenuAction.Confirm);
        _screen.Over!.Items[0].Act(MenuAction.Confirm);

        Assert.Equal(new PlayerSettings { Vsync = false }, _settings.Value);
        Assert.Null(_screen.Over);
    }

    [Fact]
    public void ResettingARiskyVideoSettingAsksToKeepIt()
    {
        _settings.Change(new PlayerSettings { ScreenMode = ScreenMode.Fullscreen });

        Item("MENU_SETTINGS_RESET").Act(MenuAction.Confirm);
        _screen.Over!.Items[0].Act(MenuAction.Confirm);

        Assert.Equal(ScreenMode.Windowed, _settings.Value.ScreenMode);
        Assert.IsType<KeepSettingScreen>(_screen.Over);
    }

    [Fact]
    public void TabsStillToComeOnlySayWhatWillBeThere()
    {
        ShowTab(Controls);

        Assert.Single(_screen.Items);
        Assert.Equal("Button remapping is coming.", Format(_screen.Body[^1]));
    }

    [Fact]
    public void EveryTabsTextIsInTheTranslationTable()
    {
        for (int tab = 0; tab < 7; tab++)
        {
            ShowTab(tab);
            foreach (MenuItem item in _screen.Items)
            {
                AssertTranslated(item.Label);
                AssertTranslated(item.Description);
                if (item is OptionItem option)
                {
                    foreach (MenuText choice in option.Choices)
                    {
                        AssertTranslated(choice);
                    }
                }
            }

            foreach (MenuText line in _screen.Body)
            {
                AssertTranslated(line);
            }
        }
    }

    private static string Format(MenuText text) => MenuTextTable.Formatter.Format(text);

    private static void AssertTranslated(MenuText text)
    {
        if (text.IsKey)
        {
            Assert.True(MenuTextTable.English.Has(text.Value), text.Value);
            foreach (MenuText argument in text.Arguments)
            {
                AssertTranslated(argument);
            }
        }
    }

    private void ShowTab(int tab)
    {
        while (_screen.TabIndex != tab)
        {
            _screen.Press(new MenuPress(MenuAction.TabRight, MenuDevice.Keyboard));
        }
    }

    private MenuItem Item(string labelKey)
    {
        foreach (MenuItem item in _screen.Items)
        {
            if (item.Label.Value == labelKey)
            {
                return item;
            }
        }

        throw new KeyNotFoundException(labelKey);
    }
}
