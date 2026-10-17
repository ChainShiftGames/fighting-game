using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FightingGame.Debug;
using FightingGame.Diagnostics;
using FightingGame.Input;
using FightingGame.Loop;
using FightingGame.Online;
using FightingGame.Platform;
using FightingGame.Presentation.Events;
using FightingGame.Settings;
using FightingGame.UI;
using Godot;
using Netcode.Desync;
using Netcode.Replays;
using Sim.Data.Validation;
using Sim.Input;

namespace FightingGame;

/// <summary>
/// Startup: builds the pieces, starts the match (local, online against one other PC, or a replay), then feeds the
/// loop time each displayed frame and saves the match's replay when it is over or left.
/// </summary>
public partial class Main : Node2D
{
    private const string PlayerSettingsPath = "user://player_settings.json";
    private const string DevBindingsPath = "user://dev_bindings.jsonc";
    private const string ReplaysPath = "user://replays";

    // Developer settings.
    private const int LatencyWindowFrames = 120;
    private const Key ToggleDebugPanelKey = Key.F1;
    private const Key ToggleOverlayKey = Key.F3;
    private const Key StepFrameKey = Key.F6;
    private const int ConnectionLogIntervalSeconds = 30;
    private const string DeveloperKeys =
        "F1 panel | F2 vsync (player setting) | F3 overlay | F4 hitboxes | F5 pause | F6 step one frame | " +
        "F7 sparks | F8 shake | F9 sounds | F10 rumble | F11 save replays | --replay <file> plays one";

    // Player setting, until the options menu.
    private const Key ToggleVsyncKey = Key.F2;

    // Developer setting: Valve's public test app (Spacewar) until the game has its own app ID.
    private const uint SteamAppId = 480;

    private readonly PlayerSettingsStore _settingsStore = new(PlayerSettingsPath);
    private readonly InputLatencyCounter _latency = new(LatencyWindowFrames, Stopwatch.Frequency);
    private readonly RateMeter _logicRate = new(Stopwatch.Frequency);
    private readonly JoypadRoster _joypads = new();

    // Developer settings, listed in the debug panel.
    private readonly DeveloperToggle _hitboxOverlay = new("Hitbox overlay", Key.F4);
    private readonly DeveloperToggle _pause = new("Pause (frame stepping)", Key.F5);
    private readonly EffectToggles _effectToggles = new();

    // Developer setting until there is a replay menu (ADR-0018 §5): every match's replay goes to ReplaysPath.
    private readonly DeveloperToggle _saveReplays = new("Save replays", Key.F11, enabled: true);
    private readonly List<DeveloperToggle> _toggles = [];

    private JoypadWatcher? _joypadWatcher;
    private InputReadout? _inputReadout;
    private PlayerSettings _settings = new();
    private GameDataLocation? _data;
    private LocalMatchStartup? _localStartup;
    private OnlineMatchStartup? _onlineStartup;
    private ReplayMatchStartup? _replayStartup;
    private ReplayFolder? _replays;
    private MatchLauncher? _launcher;
    private StartedMatch? _match;
    private FrameStepping? _stepping;
    private DeveloperOverlay? _overlay;
    private DebugPanel? _debugPanel;
    private MatchStatusLabel? _status;
    private MatchmakingMenu? _matchmakingMenu;
    private IPlatformServices? _platform;
    private string _matchError = string.Empty;
    private bool _stopped;
    private bool _wasWaiting = true;

    public override void _Ready()
    {
        _platform = PlatformStartup.Start(SteamAppId);
        _settings = _settingsStore.Load();
        ApplyVsync();
        _data = GameDataLocation.Choose();
        _stepping = new FrameStepping(_pause);
        _toggles.AddRange([_hitboxOverlay, _pause, .. _effectToggles.All, _saveReplays]);
        _replays = new ReplayFolder(ProjectSettings.GlobalizePath(ReplaysPath));

        _overlay = new DeveloperOverlay { Visible = OS.IsDebugBuild() };
        AddChild(_overlay);

        _debugPanel = new DebugPanel { Visible = false };
        _debugPanel.Build(
            _toggles, DataSettingList.Numbers(), DataSettingList.Choices(), _data.Folder, DeveloperKeys);
        _debugPanel.DataSaved += RestartLocalMatch;
        AddChild(_debugPanel);

        var playerName = new PlayerNameLabel();
        AddChild(playerName);
        playerName.Show(_platform);

        _status = new MatchStatusLabel();
        AddChild(_status);

        _matchmakingMenu = new MatchmakingMenu { Visible = false };
        AddChild(_matchmakingMenu);

        string[] args = OS.GetCmdlineUserArgs();
        CreateStartups(args);
        _launcher = new MatchLauncher(CreateLocalMatch, CreateOnlineMatch, CreateReplayMatch, _platform, args);
        if (_launcher.Matchmaking is { } matchmaking)
        {
            _matchmakingMenu.Show(matchmaking);
        }

        RenderingServer.FramePostDraw += OnFramePostDraw;
    }

    public override void _Process(double delta)
    {
        _platform!.Update();
        if (_launcher!.EndsMatch(_match))
        {
            EndMatch();
        }

        if (_match is null && _launcher.TakeReadyMatch() is { } ready)
        {
            StartMatch(ready);
        }

        if (_match is null)
        {
            _status!.Show(_matchError);
            return;
        }

        long now = Stopwatch.GetTimestamp();
        int framesRun = RunDueFrames(now);
        _logicRate.Count(framesRun, now);
        SampleConnection(now);
        if (_match.Recording is { } recording && _saveReplays.Enabled)
        {
            SaveReplay(recording.TakeFinishedReplay(), recording.Kind);
        }

        // Every displayed frame, so developer toggles show at once, also while paused.
        _match.View.QueueRedraw();

        if (_overlay!.Visible)
        {
            _overlay.Refresh(_logicRate.PerSecond, _settings.Vsync, _latency.AverageMilliseconds, _latency.MaxMilliseconds);
            _overlay.ShowInputs(_inputReadout!.Describe(_match.Loop.LastInputs));
            _overlay.ShowMatch(_match.Description, _match.Loop.CurrentFrame, _match.Connection?.Describe("\n") ?? string.Empty);
        }
    }

    public override void _UnhandledKeyInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }

        Key pressed = key.PhysicalKeycode;
        if (pressed == ToggleVsyncKey)
        {
            _settings = _settings with { Vsync = !_settings.Vsync };
            _settingsStore.Save(_settings);
            ApplyVsync();
        }
        else if (pressed == ToggleOverlayKey)
        {
            _overlay!.Visible = !_overlay.Visible;
        }
        else if (pressed == ToggleDebugPanelKey)
        {
            _debugPanel!.Toggle();
        }
        else if (pressed == StepFrameKey)
        {
            _stepping!.StepOnce();
        }
        else
        {
            FlipTogglesOf(pressed);
        }
    }

    public override void _ExitTree()
    {
        RenderingServer.FramePostDraw -= OnFramePostDraw;
        EndMatch();
        _launcher?.Dispose();
        _joypadWatcher?.Dispose();
        _platform?.Dispose();
    }

    private void FlipTogglesOf(Key pressed)
    {
        foreach (DeveloperToggle toggle in _toggles)
        {
            if (toggle.Key == pressed)
            {
                toggle.Flip();
            }
        }
    }

    // A desync stops the match and stays on screen: it is never hidden or corrected (ARCHITECTURE.md §8).
    private int RunDueFrames(long now)
    {
        if (_stopped)
        {
            return 0;
        }

        try
        {
            int framesRun = _match!.Loop.Advance(now);
            bool waiting = _match.IsOnline && _match.Loop.CurrentFrame == 0;
            if (_wasWaiting && !waiting)
            {
                GD.Print("Match running.");
            }

            _wasWaiting = waiting;
            _status!.Show(waiting ? _match.WaitingMessage : PausedText());
            return framesRun;
        }
        catch (DesyncDetectedException desync)
        {
            _stopped = true;
            GD.PushError($"Desync: {desync.Message}");
            _status!.Show($"DESYNC — match stopped\n{desync.Message}");
            return 0;
        }
    }

    private string PausedText() =>
        !_match!.IsOnline && _stepping!.IsPaused ? $"PAUSED ({_pause.Key}) — {StepFrameKey} steps one frame" : string.Empty;

    private void CreateStartups(string[] args)
    {
        _joypadWatcher = new JoypadWatcher(_joypads);
        ButtonSet? buttons = ButtonSetFile.Load();
        _inputReadout = new InputReadout(buttons, _joypads);
        PlayerBindings[] bindings = new DevBindingsFile(DevBindingsPath).Load(buttons);
        var devices = new GodotDeviceState();
        var loops = new MatchLoopFactory(LoopSettings.Default, Stopwatch.Frequency, _latency);
        _localStartup = new LocalMatchStartup(bindings, devices, _joypads, loops, _stepping!, _hitboxOverlay, _effectToggles);
        _onlineStartup = new OnlineMatchStartup(
            bindings, devices, _joypads, loops, _hitboxOverlay, _effectToggles, args);
        _replayStartup = new ReplayMatchStartup(_joypads, loops, _stepping!, _hitboxOverlay, _effectToggles);
    }

    private StartedMatch? CreateLocalMatch() => TryCreateMatch("local", () => _localStartup!.Create(_data!.Load()));

    private StartedMatch? CreateOnlineMatch(OnlineOpponent opponent) => TryCreateMatch(
        "online", () => _onlineStartup!.Create(_data!.Load(), _data.LoadOnlineSettings(), opponent));

    private StartedMatch? CreateReplayMatch(string path) =>
        TryCreateMatch("replay", () => _replayStartup!.Create(_data!.Load(), path));

    // Invalid or missing data, or a replay this build can't play, stops the match and says why on screen; it is never
    // patched up.
    private StartedMatch? TryCreateMatch(string kind, Func<StartedMatch> create)
    {
        try
        {
            StartedMatch match = create();
            _matchError = string.Empty;
            return match;
        }
        catch (Exception exception) when (
            exception is DataValidationException or IOException or InvalidDataException or ReplayRejectedException)
        {
            GD.PushError(exception.Message);
            _matchError = $"No {kind} match:\n{exception.Message}";
            return null;
        }
    }

    // After the debug panel saved data: a local match starts over with the new data, an online one keeps running.
    private void RestartLocalMatch()
    {
        if (_match is { IsOnline: true } || _launcher!.Matchmaking is not null)
        {
            GD.Print("Saved data applies to the next local match.");
            return;
        }

        EndMatch();
        if (CreateLocalMatch() is { } match)
        {
            StartMatch(match);
        }
    }

    private void StartMatch(StartedMatch match)
    {
        _match = match;
        GD.Print($"Match: {match.Description}");
        AddChild(match.View);
        MoveChild(match.View, 0);
        _matchmakingMenu!.Visible = false;
    }

    // Leaving a match saves what it recorded so far, unless its finished replay was saved already.
    private void EndMatch()
    {
        if (_match is null)
        {
            return;
        }

        GD.Print($"Match ended at frame {_match.Loop.CurrentFrame}.");
        LogConnection();
        if (_match.Recording is { } recording && _saveReplays.Enabled)
        {
            SaveReplay(recording.TakeOnEnd(), recording.Kind);
        }

        _match.Dispose();
        _match = null;
        _stopped = false;
        _wasWaiting = true;
    }

    private void SaveReplay(Replay? replay, string kind)
    {
        if (replay is null)
        {
            return;
        }

        try
        {
            string file = _replays!.Save(replay, DateTime.Now, kind);
            GD.Print($"Replay saved ({replay.Inputs.Count} frames): {file}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushError($"Replay not saved: {exception.Message}");
        }
    }

    // Online, the log gets the connection readings every so often and at the end, for the P0-22 measurements.
    private void SampleConnection(long now)
    {
        if (_match!.Connection is { } connection && connection.Sample(now) &&
            connection.SampleCount % ConnectionLogIntervalSeconds == 0)
        {
            LogConnection();
        }
    }

    private void LogConnection()
    {
        if (_match?.Connection is { } connection)
        {
            GD.Print($"Connection at frame {_match.Loop.CurrentFrame}: {connection.Describe(" | ")}");
        }
    }

    private void OnFramePostDraw() => _latency.FrameDrawn(Stopwatch.GetTimestamp());

    private void ApplyVsync() =>
        DisplayServer.WindowSetVsyncMode(_settings.Vsync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
}
