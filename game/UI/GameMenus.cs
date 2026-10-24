using System;
using System.Collections.Generic;
using FightingGame.Input;
using FightingGame.Loop;
using FightingGame.Online;
using FightingGame.Settings;
using FightingGame.UI.CharacterSelect;
using FightingGame.UI.Flow;
using FightingGame.UI.Framework;
using FightingGame.UI.MainMenu;
using FightingGame.UI.Online;
using FightingGame.UI.Results;
using FightingGame.UI.Screens;
using FightingGame.UI.Settings;
using FightingGame.UI.SideSelect;
using FightingGame.UI.Title;
using Godot;
using Netcode.Handshake;
using Platform.Steam.Lobbies;
using Sim.Data;

namespace FightingGame.UI;

/// <summary>
/// The menus around matches: keeps the <see cref="MenuStack"/> on the screens the <see cref="GameFlow"/> is at,
/// passes menu presses to them and draws them (<see cref="MenuView"/>). The pause menu opens over a match.
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
    private readonly CharacterSelectView _characterSelectView = new();
    private readonly SideSelectView _sideSelectView = new();
    private readonly ResultsView _resultsView = new();
    private readonly MenuGrace _grace = new(MenuTiming.Default.GraceMilliseconds);
    private readonly FlowScreens _screens = new();
    private MenuNavigator? _navigator;
    private GameFlow? _flow;
    private MatchLauncher? _launcher;
    private FrameStepping? _stepping;
    private LobbyScreen? _lobby;
    private SideSelectScreen? _sideSelect;
    private CharacterSelectScreen? _characterSelect;
    private ResultsScreen? _results;
    private ReplayBrowserScreen? _replays;
    private SettingsScreen? _options;
    private SettingsScreen? _pauseOptions;
    private IMenuScreen[] _pausePath = [];
    private IMenuScreen[] _pauseOptionsPath = [];
    private StartedMatch? _match;
    private string _note = string.Empty;
    private bool _isPaused;
    private bool _isPauseOptionsOpen;

    /// <summary>Whether a menu is on screen, so the host puts its match messages in it instead of over it.</summary>
    public bool IsOpen => !_stack.IsEmpty;

    public override void _Ready()
    {
        AddChild(_input);
        AddChild(_view);
        AddChild(_characterSelectView);
        AddChild(_sideSelectView);
        AddChild(_resultsView);
    }

    /// <param name="stepping">Held while the pause menu is open over a match on this PC.</param>
    /// <param name="joypads">The joypads menus read (the roster's no-double-input rule).</param>
    /// <param name="players">Whose devices are whose: side select chooses it, character select moves each player's own cursor by it.</param>
    /// <param name="loadData">The game data, for the characters to pick from.</param>
    /// <param name="replays">The saved replays, for the replay browser.</param>
    /// <param name="settings">The player settings, for the options menu.</param>
    public void Bind(
        MatchLauncher launcher,
        FrameStepping stepping,
        JoypadRoster joypads,
        IDeviceState devices,
        PlayerDevicesChoice players,
        Func<GameData> loadData,
        ReplayFolder replays,
        CurrentPlayerSettings settings)
    {
        _launcher = launcher;
        _stepping = stepping;
        _navigator = new MenuNavigator(_stack, _grace);
        _input.Bind(new MenuActionReader(MenuBindings.Default, MenuTiming.Default), devices, joypads, OnPress);
        var transition = new MenuTransition(MenuTiming.Default.TransitionMilliseconds);
        var text = new MenuTextFormatter(new GodotMenuTranslator());
        _view.Bind(text, transition);
        _characterSelectView.Bind(text, transition);
        _sideSelectView.Bind(text, transition, SideSelectSettings.Default);
        _resultsView.Bind(text, transition);
        _view.Pointed += index => _navigator.Point(index);
        _view.Clicked += index => _navigator.Click(index, Now());

        GameFlow flow = _flow = new GameFlow(launcher, _screens.Has);
        _screens.Add(FlowStep.Title, new TitleScreen(flow, () => _note));
        _screens.Add(FlowStep.MainMenu, new MainMenuScreen(flow, launcher.PlatformName, () => _note));
        _screens.Add(FlowStep.QuitPrompt, new QuitScreen(flow, () => GetTree().Quit()));
        LobbyFlow? lobby = launcher.Matchmaking?.Lobby;
        _results = CreateResults(flow, launcher, players, lobby);
        _screens.Add(FlowStep.Results, _results);
        _sideSelect = new SideSelectScreen(flow, new DeviceSides(SideSelectSettings.Default), players);
        _screens.Add(FlowStep.SideSelect, _sideSelect);
        _characterSelect = CreateCharacterSelect(flow, players, loadData, lobby);
        _screens.Add(FlowStep.CharacterSelect, _characterSelect);
        _replays = new ReplayBrowserScreen(flow, new FolderReplayLibrary(replays, loadData));
        _screens.Add(FlowStep.Replays, _replays);
        _options = CreateOptions(settings, isOverMatch: false, flow.Back);
        _screens.Add(FlowStep.Options, _options);
        if (lobby is not null)
        {
            _lobby = new LobbyScreen(flow, launcher, lobby, launcher.PlayerName, () => _note, line => GD.Print(line));
            _screens.Add(FlowStep.Online, _lobby);
        }

        var pause = new PauseScreen(flow, launcher, () => _isPaused = false, () => _isPauseOptionsOpen = true, () => _note);
        _pauseOptions = CreateOptions(settings, isOverMatch: true, () => _isPauseOptionsOpen = false);
        _pausePath = [pause];
        _pauseOptionsPath = [pause, _pauseOptions];
    }

    /// <summary>Shows the menu that fits now, or none; call once per displayed frame.</summary>
    /// <param name="match">The match being played or just finished; null when there is none.</param>
    /// <param name="note">What the match (or a failed start) has to say, e.g. a desync; may be empty.</param>
    public void Refresh(StartedMatch? match, string note, double delta)
    {
        if (_flow is null)
        {
            return;
        }

        _match = match;
        _note = note;
        _flow.Follow(match is null ? MatchPhase.None : match.IsOver ? MatchPhase.Over : MatchPhase.Playing);
        _sideSelect!.Follow(_flow.Step == FlowStep.SideSelect, Now());
        _characterSelect!.Follow(_flow.Step == FlowStep.CharacterSelect, Now());
        _results!.Follow(_flow.Step);
        bool isPlaying = _flow.Step == FlowStep.Match;
        if (!isPlaying)
        {
            _isPaused = false;
        }

        if (!_isPaused && _isPauseOptionsOpen)
        {
            // The pause menu closed around the options (Start, or the match ended): they still save.
            _isPauseOptionsOpen = false;
            _pauseOptions!.Leave();
        }

        _replays!.Follow(_flow.Step == FlowStep.Replays);
        IReadOnlyList<IMenuScreen> pausePath = _isPauseOptionsOpen ? _pauseOptionsPath : _pausePath;
        IReadOnlyList<IMenuScreen> path = isPlaying ? (_isPaused ? pausePath : []) : _screens.For(_flow.Path);
        if (_replays.Over is { } chosen && _flow.Step == FlowStep.Replays)
        {
            // The chosen replay (or the delete question) opens over the list, so back finds the list as it was.
            path = [.. path, chosen];
        }

        // The options menu's questions ("Keep this?", "Reset?") open over it the same way.
        if (path.Count > 0 && path[^1] is SettingsScreen options)
        {
            options.Follow();
            if (options.Over is { } question)
            {
                path = [.. path, question];
            }
        }

        // A and B are also attack buttons: after the pause menu closes, the match waits until they are let go, so the
        // press that resumed it doesn't attack.
        bool isHeldAfterMenu = _stepping!.IsHeld && path.Count == 0 &&
            _input.IsHeld(MenuAction.Confirm | MenuAction.Back);
        _stepping.IsHeld = _launcher!.IsLocal && (_isPaused || isHeldAfterMenu);
        _lobby?.Follow(_flow.Step == FlowStep.Online, delta);

        long now = Now();
        bool wasOpen = IsOpen;
        _stack.Show(path);
        if (!wasOpen && IsOpen)
        {
            _grace.Restart(now);
        }

        // Side select, character select and results draw themselves; every other screen is a list the menu view draws.
        bool isCharacterSelect = ReferenceEquals(_stack.Top, _characterSelect);
        bool isSideSelect = ReferenceEquals(_stack.Top, _sideSelect);
        bool isResults = ReferenceEquals(_stack.Top, _results);
        _view.Show(isCharacterSelect || isSideSelect || isResults ? null : _stack.Top, _stack.TopFocus, now);
        _characterSelectView.Show(isCharacterSelect ? _characterSelect : null, now);
        _sideSelectView.Show(isSideSelect ? _sideSelect : null, now);
        _resultsView.Show(isResults ? _results : null, now);
    }

    // In versus each player chooses with their own devices; online the one player on this PC; after a replay anyone.
    private ResultsScreen CreateResults(
        GameFlow flow, MatchLauncher launcher, PlayerDevicesChoice players, LobbyFlow? lobby) =>
        new(
            flow,
            new ResultsChoices(),
            new SessionScore(),
            () => _match is { } match ? MatchResult.From(match.Round) : MatchResult.Unfinished,
            mode => mode switch
            {
                LaunchMode.Versus => new AssignedMenuPlayers(players.Versus(), players.Roster),
                LaunchMode.Online => new AssignedMenuPlayers(players.Online(), players.Roster),
                _ => new AnyDevicePlayer(),
            },
            () => launcher.CanRematch,
            () => _note,
            lobby,
            () => _match?.Refusal ?? HandshakeRefusal.None);

    // Online only this PC's player picks, on the side the lobby gives them (ADR-0042).
    private static CharacterSelectScreen CreateCharacterSelect(
        GameFlow flow, PlayerDevicesChoice players, Func<GameData> loadData, LobbyFlow? lobby)
    {
        var selection = new CharacterSelection(CharacterSelectSettings.Default, Random.Shared.Next);
        LobbyPicking? online = lobby is null ? null : new LobbyPicking(lobby);
        return new CharacterSelectScreen(
            flow,
            selection,
            new PickMemory(),
            mode => mode switch
            {
                LaunchMode.Online when online is not null => new OnlineMenuPlayers(
                    new AssignedMenuPlayers(players.Online(), players.Roster), online.LocalSide),
                LaunchMode.Training => new AssignedMenuPlayers(players.Training(), players.Roster),
                _ => new AssignedMenuPlayers(players.Versus(), players.Roster),
            },
            () => SelectableCharacters.From(loadData()),
            mode => mode == LaunchMode.Online ? online : null);
    }

    private static long Now() => (long)Time.GetTicksMsec();

    // The window sizes offered are those that fit the screen the game is on.
    private static SettingsScreen CreateOptions(CurrentPlayerSettings settings, bool isOverMatch, Action close)
    {
        Vector2I screen = DisplayServer.ScreenGetUsableRect(DisplayServer.WindowGetCurrentScreen()).Size;
        IReadOnlyList<SettingsTab> tabs = SettingsTabs.Create(WindowSizeChoices.For(screen.X, screen.Y, settings.Value));
        return new SettingsScreen(tabs, settings, SettingsMenuTiming.Default, Now, isOverMatch, close);
    }

    // Open menus take every press; during a match only Start, which pauses it. A pad's Start closes the pause menu
    // again even during the grace time: it was opened on purpose, not mashed into. (Escape is also back, so it goes
    // through the menu like any back.)
    private bool OnPress(MenuPress press)
    {
        if (_flow!.Step == FlowStep.Title && !_stack.IsEmpty)
        {
            // Any button, back included, with the device that pressed it (the navigator would make back a Back()).
            if (_grace.HasPassed(Now()))
            {
                _stack.Top!.Press(press);
            }

            return true;
        }

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

        if (!press.Has(MenuAction.Start) || _flow.Step != FlowStep.Match || _match is null || _match.IsOver)
        {
            return false;
        }

        _isPaused = true;
        return true;
    }
}
