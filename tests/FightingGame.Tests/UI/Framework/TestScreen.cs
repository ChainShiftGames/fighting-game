using System.Collections.Generic;
using FightingGame.UI.Framework;

namespace FightingGame.Tests.UI.Framework;

/// <summary>A menu screen whose items and back answer the test sets, recording what reached it.</summary>
internal sealed class TestScreen : IMenuScreen
{
    public TestScreen(params MenuItem[] items)
    {
        Items = [.. items];
    }

    public MenuText Title { get; } = MenuText.Plain("Test");

    public IReadOnlyList<MenuText> Body { get; } = [];

    public List<MenuItem> Items { get; set; }

    IReadOnlyList<MenuItem> IMenuScreen.Items => Items;

    public int FirstFocus { get; set; }

    public bool IsOverMatch => false;

    /// <summary>What <see cref="Back"/> answers: whether the stack may pop this screen.</summary>
    public bool AllowsBack { get; set; } = true;

    public int BackCount { get; private set; }

    public List<MenuPress> Presses { get; } = [];

    public bool Back()
    {
        BackCount++;
        return AllowsBack;
    }

    public void Press(MenuPress press) => Presses.Add(press);

    /// <summary>A screen of buttons labelled with their numbers, each adding its number to <paramref name="chosen"/>.</summary>
    public static TestScreen Buttons(int count, List<int> chosen)
    {
        var items = new MenuItem[count];
        for (int i = 0; i < count; i++)
        {
            int number = i;
            string label = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
            items[i] = new ButtonItem(MenuText.Plain(label), () => chosen.Add(number));
        }

        return new TestScreen(items);
    }
}
