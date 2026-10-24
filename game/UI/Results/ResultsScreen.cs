using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FightingGame.Online;
using FightingGame.UI.CharacterSelect;
using FightingGame.UI.Flow;
using FightingGame.UI.Framework;
using FightingGame.UI.Online;
using Netcode.Handshake;
using Platform.Steam.Lobbies;

namespace FightingGame.UI.Results;

/// <summary>
/// Over a finished match (or a replay): who won, the rounds and the session score, then rematch, character select or
/// the main menu, each player with their own cursor (<see cref="ResultsChoices"/>). In versus the rematch waits for
/// both players on this PC; online for the other PC, through the lobby (ADR-0042), and it isn't offered when the
/// pre-match checks refused the match. Replays offer to watch again. Drawn by <see cref="ResultsView"/>.
/// </summary>
/// <remarks>It only reads the finished match; the session score is kept here, not in any game state.</remarks>
public sealed class ResultsScreen : IMenuScreen
{
    private readonly GameFlow _flow;
    private readonly ResultsChoices _choices;
    private readonly SessionScore _score;
    private readonly Func<MatchResult> _result;
    private readonly Func<LaunchMode, IMenuPlayers> _playersFor;
    private readonly Func<bool> _canRematch;
    private readonly Func<string> _note;
    private readonly LobbyFlow? _lobby;
    private readonly Func<HandshakeRefusal> _refusal;
    private IMenuPlayers _players = new AnyDevicePlayer();
    private bool _isOpen;

    /// <param name="result">The match just finished, read when the screen opens.</param>
    /// <param name="playersFor">Whose devices are whose for a mode, read each time the screen opens.</param>
    /// <param name="canRematch">Whether the launcher can play the match again.</param>
    /// <param name="note">What the finished match says, e.g. a desync; may be empty.</param>
    /// <param name="lobby">The online lobby; null without online play.</param>
    /// <param name="refusal">Why the pre-match checks refused the finished online match, if they did.</param>
    public ResultsScreen(
        GameFlow flow,
        ResultsChoices choices,
        SessionScore score,
        Func<MatchResult> result,
        Func<LaunchMode, IMenuPlayers> playersFor,
        Func<bool> canRematch,
        Func<string> note,
        LobbyFlow? lobby = null,
        Func<HandshakeRefusal>? refusal = null)
    {
        _flow = flow;
        _choices = choices;
        _score = score;
        _result = result;
        _playersFor = playersFor;
        _canRematch = canRematch;
        _note = note;
        _lobby = lobby;
        _refusal = refusal ?? (() => HandshakeRefusal.None);
    }

    public MatchResult Result { get; private set; } = MatchResult.Unfinished;

    public ResultsChoices Choices => _choices;

    public SessionScore Score => _score;

    /// <summary>Whether the session score is counted and shown: not for replays, where nobody on this PC played.</summary>
    public bool CountsScore => _flow.Setup.Mode != LaunchMode.Replay;

    /// <summary>Played in the lobby: the rematch waits for the other PC.</summary>
    public bool IsOnline => _flow.Setup.Mode == LaunchMode.Online && _lobby is not null;

    public MenuText Title => Result.Outcome switch
    {
        MatchOutcome.Player1Wins or MatchOutcome.Player2Wins =>
            MenuText.Key("RESULTS_WINS", PlayerNumber(Result.WinnerSide)),
        MatchOutcome.Draw => MenuText.Key("RESULTS_DRAW"),
        _ => MenuText.Key("MENU_TITLE_MATCH_OVER"),
    };

    public IReadOnlyList<MenuText> Body
    {
        get
        {
            var lines = new List<MenuText>();
            if (Result.IsFinished)
            {
                lines.Add(MenuText.Key("RESULTS_ROUNDS", Count(Result.Player1Rounds), Count(Result.Player2Rounds)));
            }

            if (CountsScore && _score.Matches > 0)
            {
                lines.Add(MenuText.Key("RESULTS_SESSION", Count(_score.WinsOf(0)), Count(_score.WinsOf(1))));
            }

            if (IsOnline)
            {
                lines.AddRange(OnlineResultsText.Describe(_lobby!, _refusal()));
            }

            lines.Add(MenuText.Plain(_note()));
            return lines;
        }
    }

    /// <summary>What to do now, under the entries: who the rematch waits for.</summary>
    public MenuText Hint
    {
        get
        {
            if (IsOnline)
            {
                return IsWaitingOnline ? MenuText.Key("ONLINE_SELECT_WAITING") : MenuText.Empty;
            }

            if (_choices.Choosers < 2 || !_choices.Entries.Contains(ResultsEntry.Rematch))
            {
                return MenuText.Empty;
            }

            for (int side = 0; side < _choices.Choosers; side++)
            {
                if (_choices.IsIn(side))
                {
                    return MenuText.Key("RESULTS_WAITING_FOR", PlayerNumber(1 - side));
                }
            }

            return _players.CanPress(0) && _players.CanPress(1) ? MenuText.Key("RESULTS_HINT_BOTH") : MenuText.Empty;
        }
    }

    // Drawn by ResultsView, so the menu view has no list to draw.
    public IReadOnlyList<MenuItem> Items { get; } = [];

    public bool IsOverMatch => true;

    /// <summary>Each player's cursor is their own, so every press comes with its device.</summary>
    public bool TakesEveryPress => true;

    private bool IsWaitingOnline => _lobby is { Stage: LobbyStage.Rematch, IsLocalReady: true };

    // The online player on this PC plays P1 when they host the lobby (ADR-0042 §4).
    private int LocalSide => _lobby is { IsHost: false } ? 1 : 0;

    /// <summary>Opens with the flow and starts the rematch once everyone wants it; call once per displayed frame.</summary>
    public void Follow(FlowStep step)
    {
        if (SessionScore.EndsAt(step))
        {
            _score.Reset();
        }

        if (step != FlowStep.Results)
        {
            _isOpen = false;
            return;
        }

        if (!_isOpen)
        {
            _isOpen = true;
            Open();
        }

        if (!IsOnline && _choices.IsRematchAgreed(_players.CanPress))
        {
            _flow.Rematch();
            Left();
        }
    }

    /// <summary>The side a chooser's cursor is drawn on: P1 or P2, or -1 for one cursor that is nobody's (replays).</summary>
    public int SideOf(int chooser) => IsOnline ? LocalSide : _flow.Setup.Mode == LaunchMode.Versus ? chooser : -1;

    /// <summary>Whether a chooser has a device to press with, so their cursor is drawn.</summary>
    public bool CanChoose(int chooser) => _players.CanPress(chooser);

    /// <summary>Whether the player on <paramref name="side"/> wants the rematch: their tick.</summary>
    public bool WantsRematch(int side)
    {
        if (!IsOnline)
        {
            return _flow.Setup.Mode == LaunchMode.Versus && _choices.IsIn(side);
        }

        if (_lobby!.Stage != LobbyStage.Rematch)
        {
            return false;
        }

        return side == LocalSide ? _lobby.IsLocalReady : _lobby.IsOpponentReady;
    }

    public MenuText LabelOf(ResultsEntry entry) => entry switch
    {
        ResultsEntry.Rematch when IsWaitingOnline => MenuText.Key("LOBBY_REMATCH_WAITING"),
        ResultsEntry.Rematch => MenuText.Key("MENU_REMATCH"),
        ResultsEntry.WatchAgain => MenuText.Key("MENU_WATCH_AGAIN"),
        ResultsEntry.CharacterSelect => MenuText.Key("MENU_CHARACTER_SELECT"),
        ResultsEntry.LeaveLobby => MenuText.Key("LOBBY_LEAVE"),
        _ => MenuText.Key("MENU_MAIN_MENU"),
    };

    public void Press(MenuPress press)
    {
        int chooser = _players.PlayerOf(press.Device);
        if (_choices.Press(chooser, press.Actions) is { } chosen)
        {
            Choose(chosen);
            Left();
        }
    }

    // Its entries say where to go; every press reaches Press (TakesEveryPress), back never comes here.
    public bool Back() => false;

    private static string PlayerNumber(int side) => (side + 1).ToString(CultureInfo.InvariantCulture);

    private static string Count(int value) => value.ToString(CultureInfo.InvariantCulture);

    private void Open()
    {
        LaunchMode mode = _flow.Setup.Mode;
        _players = _playersFor(mode);
        Result = _result();
        if (CountsScore)
        {
            _score.Record(Result);
        }

        List<ResultsEntry> entries = ResultsEntries.For(mode, _canRematch(), _flow.CanReselect, IsOnline, _refusal());
        _choices.Begin(entries, mode == LaunchMode.Versus ? 2 : 1);
    }

    // Once the flow moved on, the next results (a rematch's) open afresh even if no frame showed another step.
    private void Left() => _isOpen = _flow.Step == FlowStep.Results;

    private void Choose(ResultsEntry entry)
    {
        switch (entry)
        {
            case ResultsEntry.Rematch or ResultsEntry.WatchAgain:
                _flow.Rematch();
                break;
            case ResultsEntry.CharacterSelect:
                _flow.Reselect();
                break;
            case ResultsEntry.LeaveLobby:
                _flow.LeaveMatch();
                break;
            default:
                _flow.ToMainMenu();
                break;
        }
    }
}
