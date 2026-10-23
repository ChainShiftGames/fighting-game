using System;
using Godot;

namespace FightingGame.Input;

/// <summary>
/// One match's side of controller hot-plug (ADR-0038): hands joypads that connect to <see cref="ControllerReconnect"/>,
/// lets Start on a free pad take a disconnected one's place, and, in a match that can pause, holds it and says why
/// while a player's controller is gone. Added under the match's view, so it goes when the match does.
/// </summary>
public partial class ControllerDisconnectOverlay : CanvasLayer
{
    // Above the HUD and the pause menu: nothing plays until the controller is back.
    private const int OverlayLayer = 20;
    private static readonly Color _dim = new(0f, 0f, 0f, 0.6f);

    private readonly ColorRect _background = new() { Color = _dim, AnchorRight = 1f, AnchorBottom = 1f };
    private readonly Label _label = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        AnchorRight = 1f,
        AnchorBottom = 1f,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
    };

    private ControllerReconnect? _reconnect;
    private DeviceRoster? _roster;
    private Action<bool>? _hold;
    private int _shownPlayer = ControllerReconnect.NobodyWaiting;

    /// <param name="hold">
    /// Holds the match while true; null for a match that can't pause (online), which just reads the controller again
    /// once it is back.
    /// </param>
    public void Watch(PlayerDevices assignment, DeviceRoster roster, Action<bool>? hold)
    {
        _reconnect = new ControllerReconnect(assignment, roster);
        _roster = roster;
        _hold = hold;
    }

    public override void _Ready()
    {
        Layer = OverlayLayer;
        Visible = false;
        AddChild(_background);
        AddChild(_label);
    }

    public override void _EnterTree()
    {
        if (_roster is not null)
        {
            _roster.Connected += _reconnect!.OnConnected;
        }
    }

    public override void _ExitTree()
    {
        if (_roster is not null)
        {
            _roster.Connected -= _reconnect!.OnConnected;
        }

        _hold?.Invoke(false);
    }

    public override void _Process(double delta)
    {
        int waiting = _reconnect?.WaitingPlayer ?? ControllerReconnect.NobodyWaiting;
        if (_hold is null || waiting == _shownPlayer)
        {
            return;
        }

        _shownPlayer = waiting;
        _hold(waiting != ControllerReconnect.NobodyWaiting);
        Visible = waiting != ControllerReconnect.NobodyWaiting;
        _label.Text = $"Player {waiting + 1}: controller disconnected — reconnect or press Start on another controller";
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (_hold is not null && inputEvent is InputEventJoypadButton { Pressed: true, ButtonIndex: JoyButton.Start } press &&
            _reconnect!.TakeOver(press.Device))
        {
            GetViewport().SetInputAsHandled();
        }
    }
}
