using System.Collections.Generic;
using FightingGame.Online;
using FightingGame.UI.Flow;
using FightingGame.UI.Framework;
using Xunit;

namespace FightingGame.Tests.UI.Flow;

public sealed class GameFlowTests
{
    private readonly RecordingMatchStarter _matches = new();

    // The screens that exist today: no side, character or stage select, options or replays yet.
    private readonly HashSet<FlowStep> _screens =
        [FlowStep.Title, FlowStep.MainMenu, FlowStep.QuitPrompt, FlowStep.Online, FlowStep.Results];

    [Fact]
    public void BootsOnTheTitleAndAnyButtonThereOpensTheMainMenu()
    {
        GameFlow flow = Create();
        Assert.Equal(FlowStep.Title, flow.Step);
        Assert.Equal([FlowStep.Title], flow.Path);

        flow.PressOnTitle(MenuDevice.Gamepad(1));

        Assert.Equal(FlowStep.MainMenu, flow.Step);
        Assert.Equal(MenuDevice.Gamepad(1), flow.LeadDevice);
    }

    [Fact]
    public void VersusSkipsTheMissingScreensAndStartsTheDefaultLineup()
    {
        GameFlow flow = AtMainMenu();

        flow.Choose(LaunchMode.Versus);

        Assert.Equal(FlowStep.Match, flow.Step);
        Assert.Empty(flow.Path);
        Assert.Equal([MatchSetup.For(LaunchMode.Versus)], _matches.Started);
    }

    [Fact]
    public void ScreensThatExistComeInRouteOrderBehindTheMainMenu()
    {
        _screens.Add(FlowStep.SideSelect);
        _screens.Add(FlowStep.CharacterSelect);
        GameFlow flow = AtMainMenu();

        flow.Choose(LaunchMode.Training);
        Assert.Equal(FlowStep.SideSelect, flow.Step);
        Assert.Empty(_matches.Started);

        flow.Advance(flow.Setup);
        Assert.Equal(FlowStep.CharacterSelect, flow.Step);
        Assert.Equal([FlowStep.MainMenu, FlowStep.SideSelect, FlowStep.CharacterSelect], flow.Path);
    }

    [Fact]
    public void TheLastScreenBeforeTheMatchStartsItWithWhatWasPicked()
    {
        _screens.Add(FlowStep.CharacterSelect);
        GameFlow flow = AtMainMenu();
        flow.Choose(LaunchMode.Versus);
        MatchSetup picked = flow.Setup with { Player2 = new FighterPick("testman", 1) };

        flow.Advance(picked);

        Assert.Equal(FlowStep.Match, flow.Step);
        Assert.Equal([picked], _matches.Started);
    }

    [Fact]
    public void BackWalksTheRouteBackwardsToTheMainMenuAndThenTheTitle()
    {
        _screens.Add(FlowStep.SideSelect);
        _screens.Add(FlowStep.CharacterSelect);
        GameFlow flow = AtMainMenu();
        flow.Choose(LaunchMode.Versus);
        flow.Advance(flow.Setup);

        flow.Back();
        Assert.Equal(FlowStep.SideSelect, flow.Step);
        flow.Back();
        Assert.Equal(FlowStep.MainMenu, flow.Step);
        flow.Back();
        Assert.Equal(FlowStep.Title, flow.Step);
    }

    [Fact]
    public void AFinishedMatchShowsTheResultsAndARematchKeepsTheSetup()
    {
        GameFlow flow = AtMainMenu();
        flow.Choose(LaunchMode.Training);
        MatchSetup setup = flow.Setup;
        flow.Follow(MatchPhase.Playing);

        flow.Follow(MatchPhase.Over);
        Assert.Equal(FlowStep.Results, flow.Step);
        flow.Rematch();

        Assert.Equal(FlowStep.Match, flow.Step);
        Assert.Equal(1, _matches.Rematches);
        Assert.Same(setup, flow.Setup);

        // The finished match ends and the new one starts in the same frame.
        flow.Follow(MatchPhase.Playing);
        Assert.Equal(FlowStep.Match, flow.Step);
    }

    [Fact]
    public void ResultsOfferCharacterSelectOnlyOnceItExists()
    {
        GameFlow flow = AtResults();
        Assert.False(flow.CanReselect);

        _screens.Add(FlowStep.CharacterSelect);
        Assert.True(flow.CanReselect);
        flow.Reselect();

        Assert.Equal(FlowStep.CharacterSelect, flow.Step);
        Assert.Equal(1, _matches.Stops);
    }

    [Fact]
    public void ResultsToTheMainMenuStopTheMatch()
    {
        GameFlow flow = AtResults();

        flow.ToMainMenu();
        flow.Follow(MatchPhase.None);

        Assert.Equal(FlowStep.MainMenu, flow.Step);
        Assert.Equal(1, _matches.Stops);
    }

    [Fact]
    public void LeavingALocalMatchGoesToTheMainMenu()
    {
        GameFlow flow = AtMainMenu();
        flow.Choose(LaunchMode.Versus);
        flow.Follow(MatchPhase.Playing);

        flow.LeaveMatch();

        Assert.Equal(FlowStep.MainMenu, flow.Step);
        Assert.Equal(1, _matches.Stops);
    }

    [Fact]
    public void OnlineGoesToTheOnlineScreensAndTheLobbyStartsTheMatch()
    {
        GameFlow flow = AtMainMenu();

        flow.Choose(LaunchMode.Online);
        Assert.Equal(FlowStep.Online, flow.Step);
        Assert.Equal([FlowStep.MainMenu, FlowStep.Online], flow.Path);
        Assert.Equal(LaunchMode.Online, _matches.Mode);
        Assert.Empty(_matches.Started);

        flow.Follow(MatchPhase.Playing);
        Assert.Equal(FlowStep.Match, flow.Step);
    }

    [Fact]
    public void OnlineGoesFromTheLobbyToCharacterSelectAndTheLobbyStartsTheMatch()
    {
        _screens.Add(FlowStep.CharacterSelect);
        GameFlow flow = AtMainMenu();
        flow.Choose(LaunchMode.Online);

        // Both ready in the lobby.
        flow.Advance(flow.Setup);
        Assert.Equal(FlowStep.CharacterSelect, flow.Step);
        Assert.Equal([FlowStep.MainMenu, FlowStep.Online, FlowStep.CharacterSelect], flow.Path);

        // Both picked: the lobby starts it, the flow doesn't.
        flow.Follow(MatchPhase.Playing);
        Assert.Equal(FlowStep.Match, flow.Step);
        Assert.Empty(_matches.Started);
    }

    [Fact]
    public void BackFromOnlineCharacterSelectGoesToTheOnlineScreens()
    {
        _screens.Add(FlowStep.CharacterSelect);
        GameFlow flow = AtMainMenu();
        flow.Choose(LaunchMode.Online);
        flow.Advance(flow.Setup);

        flow.Back();

        Assert.Equal(FlowStep.Online, flow.Step);
    }

    [Fact]
    public void AFinishedOnlineMatchShowsTheResultsWhichWaitForTheRematch()
    {
        _screens.Add(FlowStep.CharacterSelect);
        GameFlow flow = InOnlineMatch();

        flow.Follow(MatchPhase.Over);
        Assert.Equal(FlowStep.Results, flow.Step);
        Assert.False(flow.CanReselect);

        flow.Rematch();
        Assert.Equal(1, _matches.Rematches);
        Assert.Equal(FlowStep.Results, flow.Step);

        // Both want it: the lobby starts the rematch.
        flow.Follow(MatchPhase.Playing);
        Assert.Equal(FlowStep.Match, flow.Step);
    }

    [Fact]
    public void LeavingTheResultsOfAnOnlineMatchGoesBackToTheOnlineScreens()
    {
        GameFlow flow = InOnlineMatch();
        flow.Follow(MatchPhase.Over);

        flow.LeaveMatch();

        Assert.Equal(FlowStep.Online, flow.Step);
        Assert.Equal(LaunchMode.Online, _matches.Mode);
    }

    [Fact]
    public void LeavingTheResultsOfALocalMatchDoesNothing()
    {
        GameFlow flow = AtResults();

        flow.LeaveMatch();

        Assert.Equal(FlowStep.Results, flow.Step);
    }

    [Fact]
    public void LeavingAnOnlineMatchGoesBackToTheOnlineScreens()
    {
        GameFlow flow = InOnlineMatch();

        flow.LeaveMatch();
        flow.Follow(MatchPhase.None);

        Assert.Equal(FlowStep.Online, flow.Step);
        Assert.Equal(LaunchMode.Online, _matches.Mode);
        Assert.Equal(0, _matches.Stops);
    }

    [Fact]
    public void AnOnlineMatchEndingOnItsOwnGoesBackToTheOnlineScreens()
    {
        GameFlow flow = InOnlineMatch();

        // The opponent left.
        flow.Follow(MatchPhase.None);

        Assert.Equal(FlowStep.Online, flow.Step);
    }

    [Fact]
    public void AReplayChosenInTheBrowserPlaysAndItsResultsOfferToWatchItAgain()
    {
        _screens.Add(FlowStep.Replays);
        GameFlow flow = AtMainMenu();
        flow.Watch("a.fgreplay");
        Assert.Empty(_matches.Watched);

        flow.Open(FlowStep.Replays);
        flow.Watch("a.fgreplay");

        Assert.Equal(["a.fgreplay"], _matches.Watched);
        Assert.Equal(FlowStep.Match, flow.Step);
        Assert.Equal(LaunchMode.Replay, flow.Setup.Mode);
        flow.Follow(MatchPhase.Playing);
        flow.Follow(MatchPhase.Over);
        Assert.Equal(FlowStep.Results, flow.Step);
    }

    [Fact]
    public void BackFromTheOnlineScreensLeavesTheLobby()
    {
        GameFlow flow = AtMainMenu();
        flow.Choose(LaunchMode.Online);

        flow.Back();

        Assert.Equal(FlowStep.MainMenu, flow.Step);
        Assert.Equal(1, _matches.Stops);
    }

    [Fact]
    public void OnlineDoesNothingWithoutThePlatform()
    {
        _matches.HasOnline = false;
        GameFlow flow = AtMainMenu();

        flow.Choose(LaunchMode.Online);

        Assert.Equal(FlowStep.MainMenu, flow.Step);
        Assert.Equal(LaunchMode.Versus, flow.Setup.Mode);
    }

    [Theory]
    [InlineData(FlowStep.Title)]
    [InlineData(FlowStep.MainMenu)]
    [InlineData(FlowStep.Results)]
    public void AnInviteAcceptedInSteamGoesToTheOnlineScreensFromAnywhere(FlowStep from)
    {
        GameFlow flow = from switch
        {
            FlowStep.Title => Create(),
            FlowStep.MainMenu => AtMainMenu(),
            _ => AtResults(),
        };

        _matches.AcceptInvite();
        flow.Follow(MatchPhase.None);

        Assert.Equal(FlowStep.Online, flow.Step);
        Assert.Equal(LaunchMode.Online, flow.Setup.Mode);
    }

    [Fact]
    public void AnInviteDuringALocalMatchGoesToTheOnlineScreens()
    {
        GameFlow flow = AtMainMenu();
        flow.Choose(LaunchMode.Versus);
        flow.Follow(MatchPhase.Playing);

        _matches.AcceptInvite();
        flow.Follow(MatchPhase.None);

        Assert.Equal(FlowStep.Online, flow.Step);
    }

    [Theory]
    [InlineData(LaunchMode.Training)]
    [InlineData(LaunchMode.Replay)]
    [InlineData(LaunchMode.DirectOnline)]
    public void ACommandLineLaunchSkipsTheMenusAndEndsAtTheMainMenu(LaunchMode launched)
    {
        var matches = new RecordingMatchStarter(launched);
        var flow = new GameFlow(matches, _screens.Contains);

        flow.Follow(MatchPhase.Playing);
        Assert.Equal(FlowStep.Match, flow.Step);
        Assert.Equal(launched, flow.Setup.Mode);

        flow.Follow(MatchPhase.Over);
        Assert.Equal(FlowStep.Results, flow.Step);
        flow.ToMainMenu();
        Assert.Equal(FlowStep.MainMenu, flow.Step);
    }

    [Fact]
    public void AMatchThatFailsToStartGoesBackToTheMainMenu()
    {
        GameFlow flow = AtMainMenu();
        flow.Choose(LaunchMode.Versus);

        flow.Follow(MatchPhase.None);

        Assert.Equal(FlowStep.MainMenu, flow.Step);
        Assert.Equal(LaunchMode.Versus, flow.Setup.Mode);
    }

    [Fact]
    public void QuitAsksOverTheMainMenuAndBackCancels()
    {
        GameFlow flow = AtMainMenu();

        flow.Open(FlowStep.QuitPrompt);
        Assert.Equal([FlowStep.MainMenu, FlowStep.QuitPrompt], flow.Path);

        flow.Back();
        Assert.Equal(FlowStep.MainMenu, flow.Step);
    }

    [Fact]
    public void ScreensWithoutAScreenYetDoNotOpen()
    {
        GameFlow flow = AtMainMenu();

        flow.Open(FlowStep.Options);
        flow.Open(FlowStep.Replays);

        Assert.Equal(FlowStep.MainMenu, flow.Step);
    }

    [Fact]
    public void BackDuringAMatchOrOnTheResultsDoesNothing()
    {
        GameFlow flow = AtResults();

        flow.Back();

        Assert.Equal(FlowStep.Results, flow.Step);
    }

    private GameFlow Create() => new(_matches, _screens.Contains);

    private GameFlow AtMainMenu()
    {
        GameFlow flow = Create();
        flow.PressOnTitle(MenuDevice.Keyboard);
        return flow;
    }

    private GameFlow AtResults()
    {
        GameFlow flow = AtMainMenu();
        flow.Choose(LaunchMode.Versus);
        flow.Follow(MatchPhase.Playing);
        flow.Follow(MatchPhase.Over);
        return flow;
    }

    private GameFlow InOnlineMatch()
    {
        GameFlow flow = AtMainMenu();
        flow.Choose(LaunchMode.Online);
        flow.Follow(MatchPhase.Playing);
        return flow;
    }
}
