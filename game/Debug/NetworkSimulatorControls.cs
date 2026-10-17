using Godot;
using Netcode.Transport;

namespace FightingGame.Debug;

/// <summary>
/// Debug panel rows that set this PC's simulated ping, jitter and packet loss (developer settings). A change applies
/// to the next packet sent, also mid-match; the game logic only ever sees its effect on the network.
/// </summary>
public partial class NetworkSimulatorControls : VBoxContainer
{
    // Ranges of the boxes: past these a match is no longer playable, so there is nothing more to learn.
    private const int MaxPingMilliseconds = 1000;
    private const int MaxJitterMilliseconds = 500;
    private const int MaxPacketLossPercent = 100;
    private const int PingArrowStep = 10;
    private const int JitterArrowStep = 5;
    private const int LossArrowStep = 1;

    private readonly SpinBox _ping = NewBox(MaxPingMilliseconds, PingArrowStep);
    private readonly SpinBox _jitter = NewBox(MaxJitterMilliseconds, JitterArrowStep);
    private readonly SpinBox _loss = NewBox(MaxPacketLossPercent, LossArrowStep);
    private LiveNetworkSimulation? _simulation;
    private NetworkSimulation _shown;

    /// <param name="simulation">The conditions every online match's transport follows.</param>
    public void Build(LiveNetworkSimulation simulation)
    {
        _simulation = simulation;
        AddChild(new Label
        {
            Text = "Adds to the packets this PC sends; set both PCs for the full round trip.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });
        AddRow("Ping, round trip (ms)", _ping);
        AddRow("Jitter, ± (ms)", _jitter);
        AddRow("Packet loss (%)", _loss);

        var presets = new HBoxContainer();
        presets.AddChild(PresetButton("Off", NetworkSimulation.None));
        presets.AddChild(PresetButton("ADR-0005 test conditions", NetworkSimulation.TestConditions));
        AddChild(presets);
        ShowValues(simulation.Current);
    }

    // The command line sets the starting conditions after the panel is built, so the boxes follow outside changes.
    public override void _Process(double delta)
    {
        if (_simulation?.Current is { } current && current != _shown)
        {
            ShowValues(current);
        }
    }

    // Step 1 with a custom arrow step, as the panel's data rows do, so typed values aren't snapped.
    private static SpinBox NewBox(int max, int arrowStep)
    {
        var box = new SpinBox { MinValue = 0, MaxValue = max, Step = 1, CustomArrowStep = arrowStep };
        box.GetLineEdit().FocusMode = FocusModeEnum.Click;
        return box;
    }

    private void AddRow(string text, SpinBox box)
    {
        box.ValueChanged += _ => Apply();
        var row = new HBoxContainer();
        row.AddChild(new Label { Text = text, SizeFlagsHorizontal = SizeFlags.ExpandFill });
        row.AddChild(box);
        AddChild(row);
    }

    private Button PresetButton(string text, NetworkSimulation preset)
    {
        var button = new Button { Text = text, FocusMode = FocusModeEnum.None };
        button.Pressed += () => _simulation!.Current = preset;
        return button;
    }

    private void Apply()
    {
        _shown = new NetworkSimulation
        {
            PingMilliseconds = (int)_ping.Value,
            JitterMilliseconds = (int)_jitter.Value,
            PacketLossPercent = (int)_loss.Value,
        };
        _simulation!.Current = _shown;
    }

    private void ShowValues(NetworkSimulation simulation)
    {
        _shown = simulation;
        _ping.SetValueNoSignal(simulation.PingMilliseconds);
        _jitter.SetValueNoSignal(simulation.JitterMilliseconds);
        _loss.SetValueNoSignal(simulation.PacketLossPercent);
    }
}
