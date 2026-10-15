using System.Diagnostics;
using FightingGame.Debug;
using FightingGame.Diagnostics;
using FightingGame.Loop;
using FightingGame.Online;
using FightingGame.Platform;
using FightingGame.Presentation;
using FightingGame.Settings;
using FightingGame.UI;
using Godot;
using Netcode.Desync;
using Sim.Toy;

namespace FightingGame;

/// <summary>Startup for the Spike A toy match: builds the pieces, then feeds the loop time each displayed frame.</summary>
public partial class Main : Node2D
{
    private const string PlayerSettingsPath = "user://player_settings.json";

    // Player settings until the options menu exists.
    private const float StickDeadzone = 0.5f;

    // Developer settings.
    private const int LatencyWindowFrames = 120;
    private const Key ToggleVsyncKey = Key.F2;
    private const Key ToggleOverlayKey = Key.F3;

    // Developer setting: Valve's public test app (Spacewar) until the game has its own app ID.
    private const uint SteamAppId = 480;

    private readonly PlayerSettingsStore _settingsStore = new(PlayerSettingsPath);
    private readonly InputLatencyCounter _latency = new(LatencyWindowFrames, Stopwatch.Frequency);
    private readonly RateMeter _logicRate = new(Stopwatch.Frequency);
    private PlayerSettings _settings = new();
    private ToyMatch? _match;
    private MatchLoop<ToyState>? _loop;
    private ToyMatchView? _view;
    private DeveloperOverlay? _overlay;
    private MatchStatusLabel? _status;
    private IPlatformServices? _platform;
    private bool _stopped;
    private bool _wasWaiting = true;

    public override void _Ready()
    {
        _platform = PlatformStartup.Start(SteamAppId);
        _settings = _settingsStore.Load();
        ApplyVsync();

        ToyRules rules = ToyRules.Default;
        _match = ToyMatchStartup.Create(rules, OS.GetCmdlineUserArgs(), StickDeadzone);
        GD.Print($"Match: {_match.Description}");
        _loop = new MatchLoop<ToyState>(
            new FixedStepClock(LoopSettings.Default, Stopwatch.Frequency), _match.Driver, _match.LocalPlayers, _latency);

        _view = new ToyMatchView();
        AddChild(_view);
        _view.Show(_match.Driver, rules);

        _overlay = new DeveloperOverlay { Visible = OS.IsDebugBuild() };
        AddChild(_overlay);

        var playerName = new PlayerNameLabel();
        AddChild(playerName);
        playerName.Show(_platform);

        _status = new MatchStatusLabel();
        AddChild(_status);

        RenderingServer.FramePostDraw += OnFramePostDraw;
    }

    public override void _Process(double delta)
    {
        _platform!.Update();
        long now = Stopwatch.GetTimestamp();
        int framesRun = RunDueFrames(now);
        _logicRate.Count(framesRun, now);
        if (framesRun > 0)
        {
            _view!.QueueRedraw();
        }

        if (_overlay!.Visible)
        {
            _overlay.Refresh(_logicRate.PerSecond, _settings.Vsync, _latency.AverageMilliseconds, _latency.MaxMilliseconds);
            _overlay.ShowMatch(_match!.Description, _match.Driver.CurrentFrame);
        }
    }

    public override void _UnhandledKeyInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }

        if (key.PhysicalKeycode == ToggleVsyncKey)
        {
            _settings = _settings with { Vsync = !_settings.Vsync };
            _settingsStore.Save(_settings);
            ApplyVsync();
        }
        else if (key.PhysicalKeycode == ToggleOverlayKey)
        {
            _overlay!.Visible = !_overlay.Visible;
        }
    }

    public override void _ExitTree()
    {
        RenderingServer.FramePostDraw -= OnFramePostDraw;
        GD.Print($"Match ended at frame {_match?.Driver.CurrentFrame}.");
        _match?.Driver.Dispose();
        _platform?.Dispose();
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
            int framesRun = _loop!.Advance(now);
            bool waiting = _match!.IsOnline && _match.Driver.CurrentFrame == 0;
            if (_wasWaiting && !waiting)
            {
                GD.Print("Match running.");
            }

            _wasWaiting = waiting;
            _status!.Show(waiting ? "Waiting for the other player…" : string.Empty);
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

    private void OnFramePostDraw() => _latency.FrameDrawn(Stopwatch.GetTimestamp());

    private void ApplyVsync() =>
        DisplayServer.WindowSetVsyncMode(_settings.Vsync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
}
