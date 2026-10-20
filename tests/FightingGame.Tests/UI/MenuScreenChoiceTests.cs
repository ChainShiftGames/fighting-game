using FightingGame.Online;
using FightingGame.UI;
using Xunit;

namespace FightingGame.Tests.UI;

public sealed class MenuScreenChoiceTests
{
    [Theory]
    [InlineData(LaunchMode.MainMenu, MenuScreen.MainMenu)]
    [InlineData(LaunchMode.Versus, MenuScreen.MainMenu)]
    [InlineData(LaunchMode.Online, MenuScreen.Lobby)]
    public void WithoutAMatchShowsTheMainMenuOrTheLobby(LaunchMode mode, MenuScreen screen)
    {
        Assert.Equal(screen, MenuScreenChoice.For(hasMatch: false, isMatchOver: false, mode, isPaused: false));
    }

    [Theory]
    [InlineData(false, false, MenuScreen.None)]
    [InlineData(false, true, MenuScreen.Pause)]
    [InlineData(true, false, MenuScreen.MatchOver)]
    [InlineData(true, true, MenuScreen.MatchOver)]
    public void DuringAMatchShowsNothingThePauseMenuOrTheMatchOverMenu(bool isOver, bool isPaused, MenuScreen screen)
    {
        Assert.Equal(screen, MenuScreenChoice.For(hasMatch: true, isOver, LaunchMode.Versus, isPaused));
    }
}
