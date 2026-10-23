using System.Collections.Generic;
using FightingGame.Input;
using FightingGame.Loop;
using FightingGame.Online;
using FightingGame.UI.Framework;
using FightingGame.UI.Screens;
using Godot;

namespace FightingGame.UI;

/// <summary>
/// The menus around matches: keeps the <see cref="MenuStack"/> on the screen that fits what the game is doing
/// (<see cref="MenuScreenChoice"/>), passes menu presses to it and draws it (<see cref="MenuView"/>).
/// </summary>
/// <remarks>
/// Escape or a joypad's Start opens the pause menu during a match; B or Escape goes back a menu where there is one. On
/// this PC it holds the match; online it can't (the other player's game keeps running), so there it only offers to
/// leave.
/// </remarks>
public partial class GameMenus : Node
{
    private readonly MenuStack _stack = new();
    private readonly MenuInput _input = new();
    private readonly MenuView _view = new();
    private readonly MenuGrace _grace = new(MenuTiming.Default.GraceMilliseconds);
    private MenuNavigator? _navigator;
    private MatchLauncher? _launcher;
    private FrameStepping? _stepping;
    private LobbyScreen? _lobby;
    private IMenuScreen[] _mainMenuPath = [];
    private IMenuScreen[] _lobbyPath = [];
    private IMenuScreen[] _onlineMatchOverPath = [];
    private IMenuScreen[] _matchOverPath = [];
    private IMenuScreen[] _pausePath = [];
    private StartedMatch? _match;
    private string _note = string.Empty;
    private bool _isPaused;

    /// <summary>Whether a menu is on screen, so the host puts its match messages in it instead of over it.</summary>
    public bool IsOpen => !_stack.IsEmpty;

    public override void _Ready()
    {
        AddChild(_input);
        AddChild(_view);
    }

    /// <param name="stepping">Held while the pause menu is open over a match on this PC.</param>
    /// <param name="joypads">The joypads menus read (the roster's no-double-input rule).</param>
    public void Bind(MatchLauncher launcher, FrameStepping stepping, JoypadRoster joypads, IDeviceState devices)
    {
        _launcher = launcher;
        _stepping = stepping;
        _navigator = new MenuNavigator(_stack, _grace);
        _input.Bind(new MenuActionReader(MenuBindings.Default, MenuTiming.Default), devices, joypads, OnPress);
        var transition = new MenuTransition(MenuTiming.Default.TransitionMilliseconds);
        _view.Bind(new MenuTextFormatter(new GodotMenuTranslator()), transition);
        _view.Pointed += index => _navigator.Point(index);
        _view.Clicked += index => _navigator.Click(index, Now());

        var mainMenu = new MainMenuScreen(launcher, () => GetTree().Quit(), () => _note);
        _mainMenuPath = [mainMenu];
        _matchOverPath = [new MatchOverScreen(launcher, () => _note)];
        _pausePath = [new PauseScreen(launcher, () => _isPaused = false, () => _note)];
        if (launcher.Matchmaking is { } matchmaking)
        {
            _lobby = new LobbyScreen(launcher, matchmaking.Lobby, () => _note, line => GD.Print(line));
            _lobbyPath = [mainMenu, _lobby];
            _onlineMatchOverPath = [_lobby];
        }
    }

    /// <summary>Shows the menu that fits now, or none; call once per displayed frame.</summary>
    /// <param name="match">The match being played or just finished; null when there is none.</param>
    /// <param name="note">What the match (or a failed start) has to say, e.g. a desync; may be empty.</param>
    public void Refresh(StartedMatch? match, string note, double delta)
    {
        if (_launcher is null)
        {
            return;
        }

        _match = match;
        _note = note;
        if (match is null || match.IsOver)
        {
            _isPaused = false;
        }

        MenuScreen screen = MenuScreenChoice.For(match is not null, match?.IsOver ?? false, _launcher.Mode, _isPaused);

        // A and B are also attack buttons: after the pause menu closes, the match waits until they are let go, so the
        // press that resumed it doesn't attack.
        bool isHeldAfterMenu = _stepping!.IsHeld && screen == MenuScreen.None &&
            _input.IsHeld(MenuAction.Confirm | MenuAction.Back);
        _stepping.IsHeld = _launcher.IsLocal && (screen == MenuScreen.Pause || isHeldAfterMenu);
        _lobby?.Follow(screen == MenuScreen.Lobby, delta);
        if (_lobby is not null)
        {
            _lobby.IsOverMatch = screen == MenuScreen.MatchOver;
        }

        long now = Now();
        bool wasOpen = IsOpen;
        _stack.Show(PathTo(screen));
        if (!wasOpen && IsOpen)
        {
            _grace.Restart(now);
        }

        _view.Show(_stack.Top, _stack.TopFocus, now);
    }

    private static long Now() => (long)Time.GetTicksMsec();

    private IReadOnlyList<IMenuScreen> PathTo(MenuScreen screen) => screen switch
    {
        MenuScreen.MainMenu => _mainMenuPath,
        MenuScreen.Lobby => _lobbyPath,
        MenuScreen.MatchOver when _launcher!.Mode == LaunchMode.Online => _onlineMatchOverPath,
        MenuScreen.MatchOver => _matchOverPath,
        MenuScreen.Pause => _pausePath,
        _ => [],
    };

    // Open menus take every press; during a match only Start, which pauses it. A pad's Start closes the pause menu
    // again even during the grace time: it was opened on purpose, not mashed into. (Escape is also back, so it goes
    // through the menu like any back.)
    private bool OnPress(MenuPress press)
    {
        bool isStartOnly = press.Actions == MenuAction.Start;
        if (_isPaused && isStartOnly)
        {
            _isPaused = false;
            return true;
        }

        if (_navigator!.Handle(press, Now()))
        {
            return true;
        }

        if (!press.Has(MenuAction.Start) || _match is null || _match.IsOver)
        {
            return false;
        }

        _isPaused = true;
        return true;
    }
}
