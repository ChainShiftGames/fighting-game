using System;
using System.Collections.Generic;
using FightingGame.Online;
using FightingGame.UI.Flow;
using FightingGame.UI.Framework;

namespace FightingGame.UI.MainMenu;

/// <summary>
/// The main menu: versus, training, online, replays, options, quit, each with a line saying what it is. Entries whose
/// screen doesn't exist yet are dimmed. The cursor starts on the mode played last.
/// </summary>
public sealed class MainMenuScreen : IMenuScreen
{
    private readonly GameFlow _flow;
    private readonly string _platformName;
    private readonly Func<string> _note;

    /// <param name="platformName">The platform's name for the player, e.g. why online play isn't there ("Offline").</param>
    /// <param name="note">What a match that failed to start has to say; may be empty.</param>
    public MainMenuScreen(GameFlow flow, string platformName, Func<string> note)
    {
        _flow = flow;
        _platformName = platformName;
        _note = note;
    }

    public MenuText Title { get; } = MenuText.Key("MENU_TITLE_MAIN");

    public IReadOnlyList<MenuText> Body => [MenuText.Plain(_note())];

    public IReadOnlyList<MenuItem> Items
    {
        get
        {
            bool hasOnline = _flow.Has(FlowStep.Online);
            MenuText online = hasOnline
                ? MenuText.Key("MENU_ONLINE")
                : MenuText.Key("MENU_ONLINE_NEEDS_PLATFORM", _platformName);
            return
            [
                Entry("MENU_VERSUS", "MENU_VERSUS_INFO", () => _flow.Choose(LaunchMode.Versus)),
                Entry("MENU_TRAINING", "MENU_TRAINING_INFO", () => _flow.Choose(LaunchMode.Training)),
                new ButtonItem(online, () => _flow.Choose(LaunchMode.Online), hasOnline)
                {
                    Description = MenuText.Key(hasOnline ? "MENU_ONLINE_INFO" : "MENU_ONLINE_NEEDS_PLATFORM_INFO"),
                },
                Opens(FlowStep.Replays, "MENU_REPLAYS", "MENU_REPLAYS_INFO", "MENU_NOT_YET"),
                Opens(FlowStep.Options, "MENU_OPTIONS", "MENU_OPTIONS_INFO", "MENU_NOT_YET"),
                Entry("MENU_QUIT", "MENU_QUIT_INFO", () => _flow.Open(FlowStep.QuitPrompt)),
            ];
        }
    }

    // The items' order: the modes come first.
    public int FirstFocus => _flow.Setup.Mode switch
    {
        LaunchMode.Training => 1,
        LaunchMode.Online => 2,
        _ => 0,
    };

    public bool IsOverMatch => false;

    public bool Back()
    {
        _flow.Back();
        return false;
    }

    public void Press(MenuPress press)
    {
    }

    private static ButtonItem Entry(string label, string description, Action choose) =>
        new(MenuText.Key(label), choose) { Description = MenuText.Key(description) };

    private ButtonItem Opens(FlowStep step, string label, string description, string missing)
    {
        bool has = _flow.Has(step);
        return new ButtonItem(MenuText.Key(label), () => _flow.Open(step), has)
        {
            Description = MenuText.Key(has ? description : missing),
        };
    }
}
