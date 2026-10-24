using System.Collections.Generic;
using FightingGame.Input;
using FightingGame.Input.Profiles;
using FightingGame.Settings;
using FightingGame.Tests.Input;
using FightingGame.Tests.UI.Framework;
using FightingGame.UI.Framework;
using FightingGame.UI.Settings;
using FightingGame.UI.Settings.Controls;
using Godot;
using Xunit;

namespace FightingGame.Tests.UI.Settings.Controls;

public sealed class ControlsMenuTests
{
    private readonly TestControls _controls = new();

    private ControlsMenu Menu => _controls.Menu;

    [Fact]
    public void TheTabListsEveryConnectedDeviceAndFollowsHotPlug()
    {
        var entries = new ControlsTabEntries(Menu, _controls.Roster, isOverMatch: false);
        Assert.Equal(3, entries.Count);

        _controls.Roster.Update([TestJoypads.Pad(0)]);

        Assert.Equal(["Keyboard", "DualSense"], Labels(entries));
    }

    [Fact]
    public void ChoosingADeviceOpensItsControls()
    {
        Item(new DeviceControlsEntry(Menu, _controls.Pad, isOverMatch: true)).Act(MenuAction.Confirm);

        Assert.IsType<DeviceControlsScreen>(Assert.Single(Menu.Path));
        Assert.Equal(_controls.Pad, Menu.Device);
        Assert.True(Menu.IsOverMatch);
    }

    [Fact]
    public void TheButtonsListShowsTheKeysThatShareAnInput()
    {
        Menu.Open(_controls.Keyboard, isOverMatch: false);
        Menu.Path[0].Items[1].Act(MenuAction.Confirm);
        IMenuScreen buttons = Menu.Path[^1];

        Assert.Equal("BUTTONS", Format(buttons.Title));
        Assert.Equal(["A: U", "B: I", "C: J, P", "D: K", "Special: O, P", "Dash: L"], [.. Labels(buttons.Items)]);
        Assert.Equal(["P is also on Special.", "P is also on C."], [.. Format(buttons.Body)[3..]]);
    }

    [Fact]
    public void TheKeyboardHasNoStickOptions()
    {
        Menu.Open(_controls.Keyboard, isOverMatch: false);
        int keyboardItems = Menu.Path[0].Items.Count;

        Menu.Open(_controls.Pad, isOverMatch: false);

        Assert.Equal(keyboardItems + 1, Menu.Path[0].Items.Count);
    }

    [Fact]
    public void ThePressedControlReplacesTheInputsControls()
    {
        Capture(_controls.Pad, "a", replace: true);

        Assert.True(Menu.Offer(_controls.Pad.Id, BoundControl.OfButton(JoyButton.Back)));

        Assert.Equal([BoundControl.OfButton(JoyButton.Back)], Menu.Profile.ControlsOf(BindingLayer.Game, "a"));
        Assert.Null(Menu.Capture);
        Assert.IsType<InputBindingScreen>(Menu.Path[^1]);
    }

    [Fact]
    public void OnlyTheEditedDeviceIsCaptured()
    {
        Capture(_controls.Pad, "a", replace: true);

        Assert.False(Menu.Offer(_controls.Hitbox.Id, BoundControl.OfButton(JoyButton.Back)));
        Assert.False(Menu.Offer(InputDeviceId.Keyboard, BoundControl.OfKey(Key.Q)));

        Assert.NotNull(Menu.Capture);
    }

    [Fact]
    public void ATriggerCantBecomeAMenuButton()
    {
        Capture(_controls.Pad, MenuInputs.Confirm, replace: true, BindingLayer.Menu);

        Assert.False(Menu.Offer(_controls.Pad.Id, BoundControl.OfTrigger(JoyAxis.TriggerRight)));
        Assert.True(Menu.Offer(_controls.Pad.Id, BoundControl.OfButton(JoyButton.X)));

        Assert.Equal([BoundControl.OfButton(JoyButton.X)], Menu.Profile.ControlsOf(BindingLayer.Menu, MenuInputs.Confirm));
    }

    [Fact]
    public void ADuplicateAsksAndCanMoveTheControl()
    {
        Capture(_controls.Pad, "a", replace: true);

        // The pad's A is d.
        Menu.Offer(_controls.Pad.Id, BoundControl.OfButton(JoyButton.A));
        var question = Assert.IsType<DuplicateBindingScreen>(Menu.Path[^1]);
        Assert.Equal("A is already on D. Use it for A?", Format(question.Body[0]));
        question.Items[0].Act(MenuAction.Confirm);

        Assert.Equal([BoundControl.OfButton(JoyButton.A)], Menu.Profile.ControlsOf(BindingLayer.Game, "a"));
        Assert.Empty(Menu.Profile.ControlsOf(BindingLayer.Game, "d"));
        Assert.IsType<InputBindingScreen>(Menu.Path[^1]);
    }

    [Fact]
    public void ADuplicateCanStayOnBoth()
    {
        Capture(_controls.Pad, "a", replace: true);
        Menu.Offer(_controls.Pad.Id, BoundControl.OfButton(JoyButton.A));

        Menu.Path[^1].Items[1].Act(MenuAction.Confirm);

        Assert.Equal(["d"], Menu.Profile.InputsBoundTo(BindingLayer.Game, BoundControl.OfButton(JoyButton.A), except: "a"));
        Assert.Contains("A is also on D.", Format(Menu.Path[^1].Body[1]), System.StringComparison.Ordinal);
    }

    [Fact]
    public void CancellingADuplicateChangesNothing()
    {
        BindingProfile before = _controls.Profiles.For(_controls.Pad.Id);
        Capture(_controls.Pad, "a", replace: true);
        Menu.Offer(_controls.Pad.Id, BoundControl.OfButton(JoyButton.A));

        Assert.False(Menu.Path[^1].Back());

        Assert.Same(before, Menu.Profile);
    }

    [Fact]
    public void TheCaptureGivesUpWhenTimeRunsOut()
    {
        Capture(_controls.Keyboard, "a", replace: true);
        Assert.Equal("Cancels in 6 s.", Format(Menu.Path[^1].Body[1]));

        _controls.Now += TestControls.CaptureMilliseconds;
        Menu.Follow();

        Assert.Null(Menu.Capture);
        Assert.IsType<InputBindingScreen>(Menu.Path[^1]);
    }

    [Fact]
    public void ClosingTheLastScreenSaves()
    {
        Menu.Open(_controls.Keyboard, isOverMatch: false);
        Menu.Push(new BindingListScreen(Menu, BindingLayer.Game, Menu.Buttons, "MENU_CONTROLS_BUTTONS_TITLE"));

        Menu.Path[^1].Back();
        Assert.Equal(0, _controls.Saves);

        Menu.Path[^1].Back();
        Assert.False(Menu.IsOpen);
        Assert.Equal(1, _controls.Saves);
    }

    [Fact]
    public void ClosingAroundACaptureEndsIt()
    {
        Capture(_controls.Keyboard, "a", replace: true);

        Menu.CloseAll();

        Assert.Null(Menu.Capture);
        Assert.False(Menu.Offer(InputDeviceId.Keyboard, BoundControl.OfKey(Key.Q)));
    }

    [Fact]
    public void APresetReplacesTheLayout()
    {
        Menu.Open(_controls.Hitbox, isOverMatch: false);
        var presets = new PresetScreen(Menu);
        Menu.Push(presets);

        presets.Items[1].Act(MenuAction.Confirm);

        Assert.Same(_controls.Profiles.PresetsFor(InputDeviceKind.Joypad)[1].Profile, Menu.Profile);
        Assert.False(Menu.Profile.Stick.LeftStick);
    }

    [Fact]
    public void ResetAsksThenGoesBackToTheDefaultAndSaves()
    {
        Menu.Open(_controls.Pad, isOverMatch: false);
        Menu.Change(Menu.Profile.WithStick(new StickOptions(false, 0.2f, 0.2f)));
        ItemLabelled(Menu.Path[0], "Reset to default").Act(MenuAction.Confirm);
        var question = Assert.IsType<ResetTabScreen>(Menu.Path[^1]);

        question.Items[0].Act(MenuAction.Confirm);

        Assert.Same(_controls.Profiles.DefaultOf(InputDeviceKind.Joypad), Menu.Profile);
        Assert.Single(Menu.Path);
        Assert.Equal(1, _controls.Saves);
    }

    [Fact]
    public void StickOptionsChangeTheLayoutInPercent()
    {
        Menu.Open(_controls.Pad, isOverMatch: false);
        var stick = new StickOptionsScreen(Menu);

        stick.Items[1].Act(MenuAction.Right);
        stick.Items[0].Act(MenuAction.Confirm);

        Assert.Equal(new StickOptions(false, 0.55f, 0.5f), Menu.Profile.Stick);
    }

    // The test area shows the raw word and the logic's: SOCD stays the fixed rule, never an option.
    [Fact]
    public void TheTestAreaShowsWhatTheDevicePressesWithItsLayout()
    {
        Menu.Open(_controls.Keyboard, isOverMatch: false);
        _controls.Devices.Hold(Key.A, Key.D, Key.U);

        MenuText[] lines = Menu.TestLines();

        Assert.Equal("Test – press anything: L R | a", Format(lines[0]));
        Assert.StartsWith("The game sees: - | a", Format(lines[1]), System.StringComparison.Ordinal);
    }

    [Fact]
    public void ResettingTheControlsTabResetsEveryDevice()
    {
        var entry = new DeviceControlsEntry(Menu, _controls.Pad, isOverMatch: false);
        _controls.Profiles.Set(_controls.Pad, _controls.Profiles.PresetsFor(InputDeviceKind.Joypad)[1].Profile);
        var settings = new PlayerSettings();

        Assert.Same(settings, entry.Reset(settings, new PlayerSettings()));

        Assert.Same(_controls.Profiles.DefaultOf(InputDeviceKind.Joypad), _controls.Profiles.For(_controls.Pad.Id));
    }

    [Fact]
    public void EveryControlsScreensTextIsInTheTranslationTable()
    {
        foreach (InputDeviceInfo device in _controls.Roster.Devices)
        {
            Menu.Open(device, isOverMatch: false);
            AssertTranslated(Menu.Path[0]);
            foreach (MenuItem item in Menu.Path[0].Items)
            {
                if (item is ButtonItem { Label.Value: not "MENU_CONTROLS_RESET" })
                {
                    item.Act(MenuAction.Confirm);
                    AssertTranslated(Menu.Path[^1]);
                    Menu.Path[^1].Back();
                }
            }

            AssertTranslated(new InputBindingScreen(Menu, BindingLayer.Game, "c"));
            AssertTranslated(new StickOptionsScreen(Menu));
            AssertTranslated(new PresetScreen(Menu));
            Menu.StartCapture(BindingLayer.Game, "special", replace: true);
            AssertTranslated(Menu.Path[^1]);
            Menu.Offer(device.Id, device.Id.IsJoypad ? BoundControl.OfButton(JoyButton.X) : BoundControl.OfKey(Key.U));
            AssertTranslated(Menu.Path[^1]);
        }

        foreach (ControlPreset preset in _controls.Profiles.PresetsFor(InputDeviceKind.Keyboard))
        {
            Assert.True(MenuTextTable.English.Has(preset.NameKey), preset.NameKey);
        }

        Assert.True(MenuTextTable.English.Has("MENU_CONTROLS_PRESET_OLD_P2_KEYBOARD"));
        Assert.True(MenuTextTable.English.Has("MENU_CONTROLS_PRESET_OLD_P2_PAD"));
    }

    private static string Format(MenuText text) => MenuTextTable.Formatter.Format(text);

    private static MenuItem Item(ISettingEntry entry) => entry.Item(new PlayerSettings(), _ => { });

    private static List<string> Labels(IReadOnlyList<ISettingEntry> entries)
    {
        var labels = new List<string>();
        foreach (ISettingEntry entry in entries)
        {
            labels.Add(Format(Item(entry).Label));
        }

        return labels;
    }

    private static List<string> Labels(IReadOnlyList<MenuItem> items)
    {
        var labels = new List<string>();
        foreach (MenuItem item in items)
        {
            labels.Add(Format(item.Label));
        }

        return labels;
    }

    private static List<string> Format(IReadOnlyList<MenuText> lines)
    {
        var texts = new List<string>();
        foreach (MenuText line in lines)
        {
            texts.Add(Format(line));
        }

        return texts;
    }

    private static MenuItem ItemLabelled(IMenuScreen screen, string label) =>
        Assert.Single(screen.Items, item => Format(item.Label) == label);

    private static void AssertTranslated(IMenuScreen screen)
    {
        AssertTranslated(screen.Title);
        foreach (MenuText line in screen.Body)
        {
            AssertTranslated(line);
        }

        foreach (MenuItem item in screen.Items)
        {
            AssertTranslated(item.Label);
            AssertTranslated(item.Description);
        }
    }

    private static void AssertTranslated(MenuText text)
    {
        if (!text.IsKey)
        {
            return;
        }

        Assert.True(MenuTextTable.English.Has(text.Value), text.Value);
        foreach (MenuText argument in text.Arguments)
        {
            AssertTranslated(argument);
        }
    }

    private void Capture(InputDeviceInfo device, string input, bool replace, BindingLayer layer = BindingLayer.Game)
    {
        Menu.Open(device, isOverMatch: false);
        Menu.Push(new InputBindingScreen(Menu, layer, input));
        Menu.StartCapture(layer, input, replace);
    }
}
