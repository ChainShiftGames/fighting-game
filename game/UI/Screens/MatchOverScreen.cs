using System;
using System.Collections.Generic;
using FightingGame.Online;
using FightingGame.UI.Flow;
using FightingGame.UI.Framework;
using FightingGame.UI.Online;
using Netcode.Handshake;
using Platform.Steam.Lobbies;

namespace FightingGame.UI.Screens;

/// <summary>
/// Over a finished match on this PC (or a replay): play it again, back to character select (once it exists), or to the
/// main menu. Online the rematch waits for both players and starts without the lobby; it isn't offered when the
/// pre-match checks refused the match.
/// </summary>
public sealed class MatchOverScreen : IMenuScreen
{
    private readonly GameFlow _flow;
    private readonly MatchLauncher _launcher;
    private readonly Func<string> _note;
    private readonly Func<HandshakeRefusal> _refusal;

    /// <param name="note">What the finished match says, e.g. a desync; may be empty.</param>
    /// <param name="refusal">Why the pre-match checks refused the finished online match, if they did.</param>
    public MatchOverScreen(GameFlow flow, MatchLauncher launcher, Func<string> note, Func<HandshakeRefusal> refusal)
    {
        _flow = flow;
        _launcher = launcher;
        _note = note;
        _refusal = refusal;
    }

    public MenuText Title { get; } = MenuText.Key("MENU_TITLE_MATCH_OVER");

    public IReadOnlyList<MenuText> Body
    {
        get
        {
            List<MenuText> lines = Lobby is { } lobby ? OnlineResultsText.Describe(lobby, _refusal()) : [];
            lines.Add(MenuText.Plain(_note()));
            return lines;
        }
    }

    public IReadOnlyList<MenuItem> Items
    {
        get
        {
            var items = new List<MenuItem>();
            if (Lobby is not null)
            {
                AddOnline(items);
            }
            else if (_launcher.CanRematch)
            {
                string again = _launcher.Mode == LaunchMode.Replay ? "MENU_WATCH_AGAIN" : "MENU_REMATCH";
                items.Add(new ButtonItem(MenuText.Key(again), _flow.Rematch));
            }

            if (_flow.CanReselect)
            {
                items.Add(new ButtonItem(MenuText.Key("MENU_CHARACTER_SELECT"), _flow.Reselect));
            }

            items.Add(new ButtonItem(MenuText.Key("MENU_MAIN_MENU"), _flow.ToMainMenu));
            return items;
        }
    }

    public bool IsOverMatch => true;

    // The lobby of the online match just played; null for matches on this PC.
    private LobbyFlow? Lobby => _launcher.Mode == LaunchMode.Online ? _launcher.Matchmaking?.Lobby : null;

    // Its buttons say where to go; back doesn't guess.
    public bool Back() => false;

    public void Press(MenuPress press)
    {
    }

    // Rematch (once; then it waits for the other player), or back to the online screens out of the lobby.
    private void AddOnline(List<MenuItem> items)
    {
        if (OnlineResultsText.CanRematch(_refusal()))
        {
            bool waiting = _launcher.IsWaitingForRematch;
            MenuText label = MenuText.Key(waiting ? "LOBBY_REMATCH_WAITING" : "MENU_REMATCH");
            items.Add(new ButtonItem(label, _flow.Rematch, !waiting));
        }

        items.Add(new ButtonItem(MenuText.Key("LOBBY_LEAVE"), _flow.LeaveMatch));
    }
}
