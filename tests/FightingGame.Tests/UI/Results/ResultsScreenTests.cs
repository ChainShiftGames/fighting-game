using System.Collections.Generic;
using FightingGame.Online;
using FightingGame.Tests.UI.Flow;
using FightingGame.UI.CharacterSelect;
using FightingGame.UI.Flow;
using FightingGame.UI.Framework;
using FightingGame.UI.Results;
using Xunit;

namespace FightingGame.Tests.UI.Results;

public sealed class ResultsScreenTests
{
    private const long AfterGrace = 1000;
    private static readonly MenuDevice _player1Pad = MenuDevice.Gamepad(0);
    private static readonly MenuDevice _player2Pad = MenuDevice.Gamepad(1);
    private static readonly MatchResult _player1Wins = new(MatchOutcome.Player1Wins, 2, 1);
    private static readonly MatchResult _player2Wins = new(MatchOutcome.Player2Wins, 0, 2);

    private readonly RecordingMatchStarter _matches = new();
    private readonly HashSet<FlowStep> _screens =
        [FlowStep.Title, FlowStep.MainMenu, FlowStep.Replays, FlowStep.CharacterSelect, FlowStep.Results];

    private readonly TestPlayers _players = new();
    private readonly MenuStack _stack = new();
    private readonly MenuNavigator _navigator;
    private readonly GameFlow _flow;
    private readonly ResultsScreen _screen;
    private MatchResult _result = _player1Wins;

    public ResultsScreenTests()
    {
        _navigator = new MenuNavigator(_stack, new MenuGrace(0));
        _flow = new GameFlow(_matches, _screens.Contains);
        _screen = new ResultsScreen(
            _flow,
            new ResultsChoices(),
            new SessionScore(),
            () => _result,
            mode => mode == LaunchMode.Replay ? new AnyDevicePlayer() : _players,
            () => true,
            () => string.Empty);
        _flow.PressOnTitle(MenuDevice.Keyboard);
    }

    [Fact]
    public void BothPressRematchAndTheSameSetupPlaysAgain()
    {
        PlayVersus();

        Press(_player1Pad, MenuAction.Confirm);
        Follow();
        Assert.Equal(FlowStep.Results, _flow.Step);
        Assert.True(_screen.WantsRematch(0));
        Assert.False(_screen.WantsRematch(1));
        Assert.Equal("RESULTS_WAITING_FOR", _screen.Hint.Value);

        Press(_player2Pad, MenuAction.Confirm);
        Follow();

        Assert.Equal(FlowStep.Match, _flow.Step);
        Assert.Equal(1, _matches.Rematches);
    }

    [Fact]
    public void OnePlayerPickingSomethingElseTakesBothThere()
    {
        PlayVersus();
        Press(_player1Pad, MenuAction.Confirm);

        Press(_player2Pad, MenuAction.Down, MenuAction.Down, MenuAction.Confirm);
        Follow();

        Assert.Equal(FlowStep.MainMenu, _flow.Step);
        Assert.Equal(0, _matches.Rematches);
    }

    [Fact]
    public void WithoutAControllerPlayerTwoNeedsNoPress()
    {
        _players.CanPressPlayer2 = false;
        PlayVersus();

        Press(_player1Pad, MenuAction.Confirm);
        Follow();

        Assert.Equal(FlowStep.Match, _flow.Step);
    }

    [Fact]
    public void ShowsTheWinnerRoundsAndSessionScore()
    {
        PlayVersus();

        Assert.Equal("RESULTS_WINS", _screen.Title.Value);
        Assert.Equal("1", _screen.Title.Arguments[0].Value);
        Assert.Equal("RESULTS_ROUNDS", _screen.Body[0].Value);
        Assert.Equal("RESULTS_SESSION", _screen.Body[1].Value);
        Assert.Equal(["1", "0"], Values(_screen.Body[1].Arguments));
    }

    [Fact]
    public void RematchesAndCharacterSelectKeepTheScore()
    {
        PlayVersus();
        BothRematch();
        _result = _player2Wins;
        FinishMatch();
        Assert.Equal((1, 1), Score());

        Press(_player1Pad, MenuAction.Down, MenuAction.Confirm);
        Follow();
        Assert.Equal(FlowStep.CharacterSelect, _flow.Step);
        _flow.Advance(_flow.Setup);
        FinishMatch();

        Assert.Equal((1, 2), Score());
    }

    [Fact]
    public void TheMainMenuResetsTheScore()
    {
        PlayVersus();
        Press(_player1Pad, MenuAction.Down, MenuAction.Down, MenuAction.Confirm);
        Follow();

        Assert.Equal((0, 0), Score());
        Assert.Equal(0, _screen.Score.Matches);
    }

    [Fact]
    public void AnUnfinishedMatchSaysSoAndDoesntCount()
    {
        _result = MatchResult.Unfinished;
        PlayVersus();

        Assert.Equal("MENU_TITLE_MATCH_OVER", _screen.Title.Value);
        Assert.Equal(0, _screen.Score.Matches);
    }

    [Fact]
    public void AReplayIsWatchedAgainWithOnePressAndNotScored()
    {
        _flow.Open(FlowStep.Replays);
        _flow.Watch("a.fgreplay");
        FinishMatch();

        Assert.Equal([ResultsEntry.WatchAgain, ResultsEntry.MainMenu], _screen.Choices.Entries);
        Assert.Equal(-1, _screen.SideOf(0));
        Assert.False(_screen.CountsScore);
        Press(_player2Pad, MenuAction.Confirm);

        Assert.Equal(FlowStep.Match, _flow.Step);
        Assert.Equal(1, _matches.Rematches);
    }

    private static List<string> Values(IReadOnlyList<MenuText> texts)
    {
        var values = new List<string>();
        foreach (MenuText text in texts)
        {
            values.Add(text.Value);
        }

        return values;
    }

    private (int, int) Score() => (_screen.Score.WinsOf(0), _screen.Score.WinsOf(1));

    private void PlayVersus()
    {
        _flow.Choose(LaunchMode.Versus);
        _flow.Advance(_flow.Setup);
        FinishMatch();
    }

    private void BothRematch()
    {
        Press(_player1Pad, MenuAction.Confirm);
        Press(_player2Pad, MenuAction.Confirm);
        Follow();
    }

    private void FinishMatch()
    {
        _flow.Follow(MatchPhase.Over);
        Follow();
    }

    private void Follow()
    {
        _screen.Follow(_flow.Step);
        _stack.Show(_flow.Step == FlowStep.Results ? [_screen] : []);
    }

    private void Press(MenuDevice device, params MenuAction[] actions)
    {
        foreach (MenuAction action in actions)
        {
            _navigator.Handle(new MenuPress(action, device), AfterGrace);
        }
    }

    // Player 1 on the first pad, player 2 on the second.
    private sealed class TestPlayers : IMenuPlayers
    {
        public bool CanPressPlayer2 { get; set; } = true;

        public int PlayerOf(MenuDevice device) => device.Kind == MenuDeviceKind.Gamepad ? device.Index : -1;

        public bool CanPress(int player) => player == 0 || CanPressPlayer2;
    }
}
