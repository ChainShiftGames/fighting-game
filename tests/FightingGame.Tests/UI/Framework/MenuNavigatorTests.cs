using System.Collections.Generic;
using FightingGame.UI.Framework;
using Xunit;

namespace FightingGame.Tests.UI.Framework;

public sealed class MenuNavigatorTests
{
    private const long GraceMilliseconds = 600;
    private const long AfterGrace = 1000;

    private readonly MenuStack _stack = new();
    private readonly MenuGrace _grace = new(GraceMilliseconds);
    private readonly List<int> _chosen = [];
    private readonly MenuNavigator _navigator;

    public MenuNavigatorTests()
    {
        _navigator = new MenuNavigator(_stack, _grace);
    }

    [Fact]
    public void WithoutAScreenPressesAreLeftForTheMatch()
    {
        Assert.False(_navigator.Handle(Press(MenuAction.Start), AfterGrace));
    }

    [Fact]
    public void ConfirmChoosesTheFocusedButton()
    {
        _stack.Push(TestScreen.Buttons(3, _chosen));

        _navigator.Handle(Press(MenuAction.Down), AfterGrace);
        _navigator.Handle(Press(MenuAction.Down), AfterGrace);
        _navigator.Handle(Press(MenuAction.Confirm), AfterGrace);

        Assert.Equal([2], _chosen);
    }

    [Fact]
    public void PressesDuringTheGraceTimeAreSwallowed()
    {
        _stack.Push(TestScreen.Buttons(2, _chosen));
        _grace.Restart(100);

        Assert.True(_navigator.Handle(Press(MenuAction.Down), 100 + GraceMilliseconds - 1));
        Assert.True(_navigator.Handle(Press(MenuAction.Confirm), 100 + GraceMilliseconds - 1));
        Assert.Empty(_chosen);

        _navigator.Handle(Press(MenuAction.Confirm), 100 + GraceMilliseconds);
        Assert.Equal([0], _chosen);
    }

    [Fact]
    public void BackGoesToTheScreenUnderWithItsFocus()
    {
        TestScreen main = TestScreen.Buttons(3, _chosen);
        _stack.Push(main);
        _navigator.Handle(Press(MenuAction.Down), AfterGrace);
        _stack.Push(TestScreen.Buttons(1, _chosen));

        _navigator.Handle(Press(MenuAction.Back), AfterGrace);
        _navigator.Handle(Press(MenuAction.Confirm), AfterGrace);

        Assert.Same(main, _stack.Top);
        Assert.Equal([1], _chosen);
    }

    [Fact]
    public void BackWinsOverStartOnTheSameKey()
    {
        TestScreen screen = TestScreen.Buttons(1, _chosen);
        _stack.Push(screen);

        _navigator.Handle(Press(MenuAction.Back | MenuAction.Start), AfterGrace);

        Assert.Equal(1, screen.BackCount);
        Assert.Empty(screen.Presses);
    }

    [Fact]
    public void DisabledItemsDoNothingAndUnusedPressesGoToTheScreen()
    {
        var screen = new TestScreen(new ButtonItem(MenuText.Plain("Off"), () => _chosen.Add(0), Enabled: false));
        _stack.Push(screen);

        _navigator.Handle(Press(MenuAction.Confirm), AfterGrace);
        _navigator.Handle(Press(MenuAction.TabRight), AfterGrace);

        Assert.Empty(_chosen);
        Assert.Equal([MenuAction.Confirm, MenuAction.TabRight], [screen.Presses[0].Actions, screen.Presses[1].Actions]);
    }

    [Fact]
    public void OptionsToggleAndSlidersChangeWithLeftAndRight()
    {
        MenuText label = MenuText.Plain("Setting");
        int option = -1;
        bool? toggle = null;
        int slider = -1;
        _stack.Push(new TestScreen(
            new OptionItem(label, [MenuText.Plain("A"), MenuText.Plain("B"), MenuText.Plain("C")], 0, i => option = i),
            new ToggleItem(label, IsOn: false, on => toggle = on),
            new SliderItem(label, Value: 10, Min: 0, Max: 10, Step: 5, value => slider = value)));

        _navigator.Handle(Press(MenuAction.Left), AfterGrace);
        Assert.Equal(2, option);

        _navigator.Handle(Press(MenuAction.Down), AfterGrace);
        _navigator.Handle(Press(MenuAction.Right), AfterGrace);
        Assert.True(toggle);

        _navigator.Handle(Press(MenuAction.Down), AfterGrace);
        _navigator.Handle(Press(MenuAction.Right), AfterGrace);
        Assert.Equal(-1, slider);
        _navigator.Handle(Press(MenuAction.Left), AfterGrace);
        Assert.Equal(5, slider);
    }

    [Fact]
    public void ClickingFocusesAndChoosesButNotDuringTheGraceTime()
    {
        _stack.Push(TestScreen.Buttons(3, _chosen));
        _grace.Restart(0);

        _navigator.Click(2, 10);
        Assert.Empty(_chosen);

        _navigator.Click(1, AfterGrace);
        Assert.Equal([1], _chosen);
        Assert.Equal(1, _stack.TopFocus!.Index);
    }

    [Fact]
    public void PointingMovesTheFocus()
    {
        _stack.Push(TestScreen.Buttons(3, _chosen));

        _navigator.Point(2);

        Assert.Equal(2, _stack.TopFocus!.Index);
    }

    private static MenuPress Press(MenuAction actions) => new(actions, MenuDevice.Keyboard);
}
