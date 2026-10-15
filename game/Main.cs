using System.Diagnostics;
using FightingGame.Debug;
using FightingGame.Diagnostics;
using FightingGame.Input;
using FightingGame.Loop;
using FightingGame.Platform;
using FightingGame.Presentation;
using FightingGame.Settings;
using FightingGame.UI;
using Godot;
using Netcode.Drivers;
using Sim.Toy;

namespace FightingGame;

/// <summary>Startup for the Spike A local match: builds the pieces, then feeds the loop time each displayed frame.</summary>
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
    private LocalMatchDriver<ToyState>? _driver;
    private MatchLoop<ToyState>? _loop;
    private ToyMatchView? _view;
    private DeveloperOverlay? _overlay;
    private IPlatformServices? _platform;

    public override void _Ready()
    {
        _platform = PlatformStartup.Start(SteamAppId);
        _settings = _settingsStore.Load();
        ApplyVsync();

        ToyRules rules = ToyRules.Default;
        _driver = new LocalMatchDriver<ToyState>(new ToyMatchSimulation(new ToySimulation(rules)));
        PlayerInputReader[] players =
        [
            new(new KeyboardDevice(KeyboardLayout.Player1Default), new GamepadDevice(0, StickDeadzone)),
            new(new KeyboardDevice(KeyboardLayout.Player2Default), new GamepadDevice(1, StickDeadzone)),
        ];
        _loop = new MatchLoop<ToyState>(
            new FixedStepClock(LoopSettings.Default, Stopwatch.Frequency), _driver, players, _latency);

        _view = new ToyMatchView();
        AddChild(_view);
        _view.Show(_driver, rules);

        _overlay = new DeveloperOverlay { Visible = OS.IsDebugBuild() };
        AddChild(_overlay);

        var playerName = new PlayerNameLabel();
        AddChild(playerName);
        playerName.Show(_platform);

        RenderingServer.FramePostDraw += OnFramePostDraw;
    }

    public override void _Process(double delta)
    {
        _platform!.Update();
        long now = Stopwatch.GetTimestamp();
        int framesRun = _loop!.Advance(now);
        _logicRate.Count(framesRun, now);
        if (framesRun > 0)
        {
            _view!.QueueRedraw();
        }

        if (_overlay!.Visible)
        {
            _overlay.Refresh(_logicRate.PerSecond, _settings.Vsync, _latency.AverageMilliseconds, _latency.MaxMilliseconds);
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
        _driver?.Dispose();
        _platform?.Dispose();
    }

    private void OnFramePostDraw() => _latency.FrameDrawn(Stopwatch.GetTimestamp());

    private void ApplyVsync() =>
        DisplayServer.WindowSetVsyncMode(_settings.Vsync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
}
