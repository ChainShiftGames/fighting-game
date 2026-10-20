using FightingGame.Loop;
using FightingGame.Presentation.Hud;
using Godot;
using Netcode.Drivers;
using Sim.Characters;
using Sim.Modules;
using Sim.Rules;
using Sim.State;

namespace FightingGame.Presentation;

/// <summary>
/// The match HUD in the Aether Glass style: health with a damage trail, burst, names and round wins, the timer, the
/// meter and the combo counter, on frosted glass over the stage, and the KO / time-out banner. Reads the game state
/// only; burst, meter and combo show only when their modules are enabled.
/// </summary>
public partial class MatchHud : Node2D
{
    private static readonly GaugeLook _healthLook = new(
        Start: new Color(0.35f, 0.72f, 1f),
        Middle: new Color(0.25f, 0.82f, 0.76f),
        End: new Color(0.73f, 1f, 0.94f),
        Glow: new Color(0.56f, 0.96f, 0.87f, 0.55f),
        Trail: new Color(1f, 0.62f, 0.69f, 0.85f),
        Inset: 4f,
        GlowSize: 8f);

    private static readonly GaugeLook _burstLook = new(
        Start: Colors.White,
        Middle: Colors.White,
        End: Colors.White,
        Glow: new Color(1f, 1f, 1f, 0.6f),
        Trail: Colors.Transparent,
        Inset: 1f,
        GlowSize: 6f);

    private static readonly GaugeLook _meterLook = new(
        Start: new Color(0.25f, 0.82f, 0.76f),
        Middle: new Color(0.5f, 0.91f, 0.85f),
        End: new Color(0.73f, 1f, 0.94f),
        Glow: new Color(0.56f, 0.96f, 0.87f, 0.55f),
        Trail: Colors.Transparent,
        Inset: 3f,
        GlowSize: 6f);

    private readonly GlassPanel _timer = new();
    private readonly GlassGauge[] _health = [new(_healthLook), new(_healthLook)];
    private readonly GlassGauge[] _burst = [new(_burstLook), new(_burstLook)];
    private readonly GlassGauge[] _meter = new GlassGauge[MatchState.PlayerCount * HudLayout.MeterSegments];
    private readonly RoundBanner _banner = new();
    private IMatchDriver<MatchState>? _driver;
    private HudReadout? _readout;
    private HudLettering? _lettering;

    /// <param name="characters">P1's and P2's character, for their names and max health.</param>
    /// <param name="rules">The match's rules, for the rounds to win and which modules are enabled.</param>
    public void Show(IMatchDriver<MatchState> driver, CharacterData[] characters, in MatchRules rules)
    {
        _driver = driver;
        var gauges = new HudGauges(RegisteredModules.Create(), in rules);
        _readout = new HudReadout(characters, gauges);
        _lettering = new HudLettering([characters[0].DisplayName, characters[1].DisplayName], rules.RoundsToWin);

        AddChild(_timer);
        for (int player = 0; player < MatchState.PlayerCount; player++)
        {
            AddChild(_health[player]);
            _burst[player].Visible = gauges.HasBurst;
            AddChild(_burst[player]);
            for (int segment = 0; segment < HudLayout.MeterSegments; segment++)
            {
                var gauge = new GlassGauge(_meterLook) { Visible = gauges.HasMeter };
                _meter[MeterIndex(player, segment)] = gauge;
                AddChild(gauge);
            }
        }

        AddChild(_lettering);
        AddChild(_banner);
    }

    // Drawn on top of the stage; redrawn every displayed frame, like the view under it.
    public override void _Ready() => ZIndex = 1;

    public override void _Process(double delta)
    {
        if (_driver is null || _readout is null || _lettering is null)
        {
            return;
        }

        ref readonly MatchState state = ref _driver.State;
        var layout = HudLayout.For(GetViewportRect().Size);
        HudPlayerValues player1 = _readout.Read(in state, 0);
        HudPlayerValues player2 = _readout.Read(in state, 1);
        _timer.Place(layout.Timer, cornerShare: 0.5f);
        ShowPlayer(layout, 0, player1);
        ShowPlayer(layout, 1, player2);

        string timer = HudText.Timer(state.Round.TimerFramesLeft, LoopSettings.Default.FramesPerSecond);
        _lettering.Show(layout, player1, player2, timer, HudText.Round(state.Round.Round), _readout.Gauges.HasMeter);
        _banner.Show(layout, in state.Round);
    }

    private static int MeterIndex(int player, int segment) => (player * HudLayout.MeterSegments) + segment;

    // P1's bars fill towards the timer, so damage eats them from the outer edge; P2 mirrors P1.
    private void ShowPlayer(HudLayout layout, int player, HudPlayerValues values)
    {
        bool isMirrored = player == 1;
        float scale = layout.Scale;
        _health[player].Show(layout.HealthBar(player), values.Health, values.DamageTrail, !isMirrored, isMirrored, scale);
        _burst[player].Show(layout.BurstLine(player), values.Burst, 0f, !isMirrored, isMirrored, scale);

        // The meter fills outer segment first, from the outer end.
        for (int segment = 0; segment < HudLayout.MeterSegments; segment++)
        {
            float share = HudText.MeterSegmentFill(values.Meter, segment, HudLayout.MeterSegments, HudGauges.FullMeter);
            _meter[MeterIndex(player, segment)].Show(layout.MeterSegment(player, segment), share, 0f, isMirrored, isMirrored, scale);
        }
    }
}
