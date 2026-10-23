using System;
using System.Collections.Generic;
using FightingGame.UI.Framework;
using Xunit;

namespace FightingGame.Tests.UI.Framework;

public sealed class MenuStackTests
{
    private readonly List<int> _chosen = [];

    [Fact]
    public void PushShowsTheNewScreenAndPopReturnsToTheOneUnder()
    {
        var stack = new MenuStack();
        TestScreen main = TestScreen.Buttons(3, _chosen);
        TestScreen options = TestScreen.Buttons(2, _chosen);

        stack.Push(main);
        stack.Push(options);
        Assert.Same(options, stack.Top);

        stack.Pop();
        Assert.Same(main, stack.Top);
        Assert.Equal(1, stack.Count);
    }

    [Fact]
    public void BackPopsWhenTheScreenAgrees()
    {
        var stack = new MenuStack();
        TestScreen main = TestScreen.Buttons(1, _chosen);
        TestScreen options = TestScreen.Buttons(1, _chosen);
        stack.Push(main);
        stack.Push(options);

        options.AllowsBack = false;
        stack.Back();
        Assert.Same(options, stack.Top);

        options.AllowsBack = true;
        stack.Back();
        Assert.Same(main, stack.Top);
        Assert.Equal(2, options.BackCount);
    }

    [Fact]
    public void TheBottomScreenDecidesBackButIsNeverPopped()
    {
        var stack = new MenuStack();
        TestScreen main = TestScreen.Buttons(1, _chosen);
        stack.Push(main);

        stack.Back();

        Assert.Equal(1, main.BackCount);
        Assert.Same(main, stack.Top);
    }

    [Fact]
    public void GoingBackRestoresTheFocusedItem()
    {
        var stack = new MenuStack();
        TestScreen main = TestScreen.Buttons(4, _chosen);
        stack.Push(main);
        stack.TopFocus!.Move(main.Items, 1);
        stack.TopFocus.Move(main.Items, 1);

        stack.Push(TestScreen.Buttons(2, _chosen));
        Assert.Equal(-1, stack.TopFocus.Index);
        stack.Back();

        Assert.Equal(2, stack.TopFocus!.Index);
    }

    [Fact]
    public void ReplaceSwapsTheTopScreenSoBackSkipsIt()
    {
        var stack = new MenuStack();
        TestScreen main = TestScreen.Buttons(1, _chosen);
        TestScreen first = TestScreen.Buttons(1, _chosen);
        TestScreen second = TestScreen.Buttons(1, _chosen);
        stack.Push(main);
        stack.Push(first);

        stack.Replace(second);
        stack.Back();

        Assert.Same(main, stack.Top);
    }

    [Fact]
    public void ShowKeepsScreensAlreadyInPlaceWithTheirFocus()
    {
        var stack = new MenuStack();
        TestScreen main = TestScreen.Buttons(4, _chosen);
        TestScreen lobby = TestScreen.Buttons(2, _chosen);
        stack.Show([main]);
        stack.TopFocus!.Move(main.Items, 1);
        stack.TopFocus.Move(main.Items, 1);

        stack.Show([main, lobby]);
        Assert.Same(lobby, stack.Top);
        stack.Show([main, lobby]);
        Assert.Equal(2, stack.Count);

        stack.Show([main]);
        Assert.Equal(2, stack.TopFocus!.Index);

        stack.Show([]);
        Assert.True(stack.IsEmpty);
        Assert.Null(stack.TopFocus);
    }

    [Fact]
    public void PopOnAnEmptyStackFailsLoudly()
    {
        Assert.Throws<InvalidOperationException>(() => new MenuStack().Pop());
    }
}
