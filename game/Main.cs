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
using FightingGame.Presentation.Feel;
using FightingGame.Presentation.World;
using FightingGame.Settings;
using FightingGame.Training;
using FightingGame.UI;
using Godot;
using Netcode.Desync;
using Netcode.Online;
using Netcode.Replays;
using Netcode.Transport;
using Sim.Data.Validation;
using Sim.Input;

namespace FightingGame;

/// <summary>
/// Startup: builds the pieces and the menus, starts the match picked there or on the command line (versus, training,
/// online against one other PC, or a replay), then feeds the loop time each displayed frame and saves the match's
/// replay when it is over or left.
/// </summary>
public partial class Main : Node2D
{
    private const string PlayerSettingsPath = "user://player_settings.json";
    private const string DevBindingsPath = "user://dev_bindings.jsonc";
    private const string ReplaysPath = "user://replays";
    private const string TrainingSettingsPath = "user://training_settings.json";

    // Developer setting: where online desync dumps go (ADR-0005 §5); the players send these files to the devs.
    private const string DesyncsPath = "user://" + DesyncDumpSettings.DefaultFolderName;

    // Developer settings.
    private const int LatencyWindowFrames = 120;
    private const Key ToggleDebugPanelKey = Key.F1;
    private const Key ToggleOverlayKey = Key.F3;
    private const Key StepFrameKey = Key.F6;
    private const int ConnectionLogIntervalSeconds = 30;

    // Developer setting: how often editor runs look for data saved from outside the game (character editor, P3-43).
    private const double DataCheckSeconds = 0.5;
    private const string DeveloperKeys =
        "F1 panel | F2 vsync (player setting) | F12 options | F3 overlay | F4 hitboxes | F5 pause | F6 step one frame | " +
        "F7 sparks | F8 shake | F9 sounds | F10 rumble | F11 save replays | Esc pause menu | --replay <file> plays one | " +
        "--training starts training (Tab menu) | " + DeviceAssignmentArgs.Usage;

    // Player setting: a shortcut for the options menu's vsync entry.
    private const Key ToggleVsyncKey = Key.F2;

    // Developer setting: Valve's public test app (Spacewar) until the game has its own app ID.
    private const uint SteamAppId = 480;

    private readonly PlayerSettingsStore _settingsStore = new(PlayerSettingsPath);
    private readonly InputLatencyCounter _latency = new(LatencyWindowFrames, Stopwatch.Frequency);
    private readonly RateMeter _logicRate = new(Stopwatch.Frequency);
    private readonly DeviceRoster _devices = new();

    // Developer settings, listed in the debug panel.
    private readonly DeveloperToggle _hitboxOverlay = new("Hitbox overlay", Key.F4);
    private readonly DeveloperToggle _pause = new("Pause (frame stepping)", Key.F5);
    private readonly EffectToggles _effectToggles = new();
    private readonly LookToggles _lookToggles = new();
    private readonly FighterModelSwitch _modelSwitch = new();

    // Developer setting until there is a replay menu (ADR-0018 §5): every match's replay goes to ReplaysPath.
    private readonly DeveloperToggle _saveReplays = new("Save replays", Key.F11, enabled: true);
    private readonly List<DeveloperToggle> _toggles = [];

    // Developer settings: this PC's simulated network conditions, changed from the debug panel mid-match (P2-11).
    private readonly LiveNetworkSimulation _networkSimulation = new();

    private JoypadWatcher? _joypadWatcher;
    private ControllerNotices? _controllerNotices;
    private InputReadout? _inputReadout;
    private PlayerSettings _settings = new();
    private GameDataLocation? _data;
    private DataChangeWatch? _dataWatch;
    private LocalMatchStartup? _localStartup;
    private OnlineMatchStartup? _onlineStartup;
    private ReplayMatchStartup? _replayStartup;
    private TrainingMatchStartup? _trainingStartup;
    private TrainingMenu? _trainingMenu;
    private ReplayFolder? _replays;
    private MatchLauncher? _launcher;
    private StartedMatch? _match;
    private FrameStepping? _stepping;
    private DeveloperOverlay? _overlay;
    private DebugPanel? _debugPanel;
    private MatchStatusLabel? _status;
    private GameMenus? _menus;
    private OptionsMenu? _optionsMenu;
    private PingIndicator? _pingIndicator;
    private NetworkReadout? _networkReadout;
    private IPlatformServices? _platform;
    private string _matchError = string.Empty;
    private bool _wasWaiting = true;
    private ConnectionState _shownConnection = ConnectionState.Connected;

    public override void _Ready()
    {
        _platform = PlatformStartup.Start(SteamAppId);
        _settings = _settingsStore.Load();
        ApplyVsync();
        _data = GameDataLocation.Choose();
        if (_data.Folder is string folder)
        {
            // The effect settings file is presentation, outside data/, but the control panel saves it the same way.
            string effects = ProjectSettings.GlobalizePath(EffectSettingsFile.ResPath);
            _dataWatch = new DataChangeWatch(
                () => HashCode.Combine(DataFolderFingerprint.Of(folder), DataFolderFingerprint.OfFile(effects)),
                (long)(Stopwatch.Frequency * DataCheckSeconds),
                Stopwatch.GetTimestamp());
        }
        _stepping = new FrameStepping(_pause);
        _toggles.AddRange([_hitboxOverlay, _pause, .. _effectToggles.All, _saveReplays, .. _lookToggles.All]);
        _replays = new ReplayFolder(ProjectSettings.GlobalizePath(ReplaysPath));

        _overlay = new DeveloperOverlay { Visible = OS.IsDebugBuild() };
        AddChild(_overlay);

        _debugPanel = new DebugPanel { Visible = false };
        (DataFileFlag[] moduleSwitches, DataFileSetting[] moduleValues) = ModuleSettingList.For(_data.Folder);
        _debugPanel.Build(
            _toggles,
            moduleSwitches,
            [.. DataSettingList.Numbers(), .. moduleValues],
            DataSettingList.Choices(),
            _data.Folder,
            DeveloperKeys);
        _debugPanel.DataSaved += RestartLocalMatch;
        var networkSimulator = new NetworkSimulatorControls();
        networkSimulator.Build(_networkSimulation);
        _debugPanel.AddSection("Network simulator (developer settings, act at once)", networkSimulator);
        AddFighterLookSection(_debugPanel);
        _modelSwitch.Switched += variant => _modelSwitch.ShowName(_match?.View.ShowModel(variant) ?? string.Empty);
        _debugPanel.AddSection("Fighter model (developer setting, not saved)", _modelSwitch);
        AddChild(_debugPanel);
        _networkReadout = new NetworkReadout(_networkSimulation);

        _pingIndicator = new PingIndicator();
        AddChild(_pingIndicator);

        var playerName = new PlayerNameLabel();
        AddChild(playerName);
        playerName.Show(_platform);

        _status = new MatchStatusLabel();
        AddChild(_status);

        // Before the options and training menus, so they see Escape first while open.
        _menus = new GameMenus();
        AddChild(_menus);

        _optionsMenu = new OptionsMenu();
        AddChild(_optionsMenu);
        _optionsMenu.Bind(() => _settings, ChangeSettings);

        string[] args = OS.GetCmdlineUserArgs();
        CreateStartups(args);
        _launcher = new MatchLauncher(
            CreateVersusMatch, CreateTrainingMatch, CreateOnlineMatch, CreateReplayMatch, _platform, args);
        _menus.Bind(_launcher, _stepping!);

        RenderingServer.FramePostDraw += OnFramePostDraw;
    }

    public override void _Process(double delta)
    {
        _platform!.Update();
        if (_dataWatch?.HasSettledChange(Stopwatch.GetTimestamp()) == true)
        {
            GD.Print("Data changed on disk.");
            RestartLocalMatch();
        }
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
            _menus!.Refresh(null, _matchError, delta);
            _status!.Show(string.Empty);
            _pingIndicator!.Show(null);
            return;
        }

        long now = Stopwatch.GetTimestamp();
        int framesRun = RunDueFrames(now);
        ShowMatchStatus(delta);
        _logicRate.Count(framesRun, now);
        SampleConnection(now);
        _pingIndicator!.Show(_match.Connection?.PingMilliseconds);
        if (_match.Recording is { } recording && _saveReplays.Enabled)
        {
            SaveReplay(recording.TakeFinishedReplay(), recording.Kind);
        }

        // Every displayed frame, so developer toggles show at once, also while paused.
        _match.View.Refresh();

        if (_overlay!.Visible)
        {
            _overlay.Refresh(_logicRate.PerSecond, _settings.Vsync, _latency.AverageMilliseconds, _latency.MaxMilliseconds);
            _overlay.ShowInputs(_inputReadout!.Describe(_match.Loop.LastInputs));
            _overlay.ShowNetwork(_match.IsOnline ? _networkReadout!.Describe() : string.Empty);
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
            ChangeSettings(_settings with { Vsync = !_settings.Vsync });
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
        _controllerNotices?.Dispose();
        _platform?.Dispose();
    }

    private void ChangeSettings(PlayerSettings settings)
    {
        _settings = settings;
        _settingsStore.Save(settings);
        ApplyVsync();
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

    // A desync stops the match for good and stays on screen: it is never hidden or corrected (ARCHITECTURE.md §8).
    private int RunDueFrames(long now)
    {
        if (_match!.Desync.Length > 0)
        {
            return 0;
        }

        try
        {
            int framesRun = _match.Loop.Advance(now);
            bool waiting = _match.IsOnline && _match.Loop.CurrentFrame == 0;
            if (_wasWaiting && !waiting)
            {
                GD.Print("Match running.");
            }

            _wasWaiting = waiting;
            return framesRun;
        }
        catch (DesyncDetectedException desync)
        {
            GD.PushError($"Desync: {desync.Message}");
            _match.StopForDesync(desync.Message);
            return 0;
        }
    }

    // Over a finished or paused match a menu is up: its text carries the message then, so the two never overlap.
    private void ShowMatchStatus(double delta)
    {
        ConnectionStatus connection = _match!.ConnectionStatus;
        if (connection.State != _shownConnection)
        {
            GD.Print($"Connection {_shownConnection} → {connection.State} at frame {_match.Loop.CurrentFrame}.");
            _shownConnection = connection.State;
        }

        string waiting = _match.IsOnline && _match.Loop.CurrentFrame == 0 ? _match.WaitingMessage : string.Empty;
        string status = MatchStatusText.Choose(_match.Desync, connection, waiting, PausedText());
        _menus!.Refresh(_match, status, delta);
        _status!.Show(_menus.IsOpen ? string.Empty : status);
    }

    private string PausedText() =>
        !_match!.IsOnline && _stepping!.IsPaused ? $"PAUSED ({_pause.Key}) — {StepFrameKey} steps one frame" : string.Empty;

    private void CreateStartups(string[] args)
    {
        _joypadWatcher = new JoypadWatcher(_devices);
        var toasts = new ToastNotice();
        AddChild(toasts);
        _controllerNotices = new ControllerNotices(_devices, toasts.Show);
        ButtonSet? buttons = ButtonSetFile.Load();
        _inputReadout = new InputReadout(buttons, _devices.Joypads);
        PlayerBindings[] bindings = new DevBindingsFile(DevBindingsPath).Load(buttons);
        var choice = new PlayerDevicesChoice(_devices, args);
        var reader = new InputDeviceReader(bindings, new GodotDeviceState(), _devices);
        var loops = new MatchLoopFactory(LoopSettings.Default, Stopwatch.Frequency, _latency);
        _localStartup = new LocalMatchStartup(choice, reader, loops, _stepping!, _hitboxOverlay, _effectToggles);
        _onlineStartup = new OnlineMatchStartup(
            choice,
            reader,
            loops,
            _hitboxOverlay,
            _effectToggles,
            _networkSimulation,
            ProjectSettings.GlobalizePath(DesyncsPath),
            args);
        _replayStartup = new ReplayMatchStartup(_devices, loops, _stepping!, _hitboxOverlay, _effectToggles);
        var trainingStore = new TrainingSettingsStore(TrainingSettingsPath);
        _trainingMenu = new TrainingMenu();
        AddChild(_trainingMenu);
        _trainingStartup = new TrainingMatchStartup(
            choice,
            reader,
            loops,
            _stepping!,
            _hitboxOverlay,
            _effectToggles,
            new CurrentTrainingSettings(trainingStore.Load(), trainingStore.Save),
            _trainingMenu);
    }

    private StartedMatch? CreateVersusMatch() => TryCreateMatch("local", () => _localStartup!.Create(_data!.Load()));

    private StartedMatch? CreateTrainingMatch() =>
        TryCreateMatch("training", () => _trainingStartup!.Create(_data!.Load()));

    private StartedMatch? CreateOnlineMatch(OnlineOpponent opponent)
    {
        _networkReadout!.Watch(opponent.Transport.Dropped);
        return TryCreateMatch(
            "online", () => _onlineStartup!.Create(_data!.Load(), _data.LoadOnlineSettings(), _settings, opponent));
    }

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

    // Only where the art files are plain files on disk (editor and dev runs), not in an exported build.
    private void AddFighterLookSection(DebugPanel panel)
    {
        string root = ProjectSettings.GlobalizePath(FighterArtFile.CharactersFolder);
        DataFileSetting[] settings = ArtFileSettingList.LookSettings(root);
        if (settings.Length == 0)
        {
            return;
        }

        var look = new FighterLookControls();
        look.Build(settings, root);
        look.Saved += RestartLocalMatch;
        panel.AddSection("Fighter look (art files, developer settings)", look);
    }

    // After data was saved (debug panel, character editor): a match on this PC starts over with the new data, an
    // online one keeps running.
    private void RestartLocalMatch()
    {
        // The data as it is now is what the next match loads, so the same save never restarts it twice.
        _dataWatch?.Accept(Stopwatch.GetTimestamp());
        if (!_launcher!.Restart())
        {
            GD.Print("Saved data applies to the next match.");
        }
    }

    private void StartMatch(StartedMatch match)
    {
        _match = match;
        GD.Print($"Match: {match.Description}");
        match.View.FollowLook(_lookToggles);
        _modelSwitch.ShowName(match.View.ShowModel(_modelSwitch.Variant));
        AddChild(match.View);
        MoveChild(match.View, 0);
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
        _trainingMenu?.Use([]);
        _wasWaiting = true;
        _shownConnection = ConnectionState.Connected;
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
