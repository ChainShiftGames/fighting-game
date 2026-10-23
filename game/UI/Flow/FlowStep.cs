namespace FightingGame.UI.Flow;

/// <summary>One stop on the player's path through the game (<see cref="GameFlow"/>); each is one screen, or the match.</summary>
public enum FlowStep
{
    /// <summary>"Press any button"; the game boots here.</summary>
    Title,

    MainMenu,

    /// <summary>Over the main menu: quit the game, or not.</summary>
    QuitPrompt,

    /// <summary>The settings menu (UI-09).</summary>
    Options,

    /// <summary>Saved replays to watch.</summary>
    Replays,

    /// <summary>Controllers slide to player 1 or 2 (UI-03).</summary>
    SideSelect,

    /// <summary>Characters and colours (UI-05).</summary>
    CharacterSelect,

    /// <summary>Stage select and the versus screen (UI-06).</summary>
    StageSelect,

    /// <summary>The online screens: lobby, host, join, ready (UI-14); also shown over a finished online match.</summary>
    Online,

    /// <summary>A match is being played; the pause menu opens over it.</summary>
    Match,

    /// <summary>Over a finished match on this PC: rematch, character select or main menu (UI-07).</summary>
    Results,
}
