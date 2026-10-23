using System;

namespace FightingGame.Input;

/// <summary>Says when a controller connects or disconnects, through a notice such as a toast; keyboard halves never do.</summary>
public sealed class ControllerNotices : IDisposable
{
    private readonly DeviceRoster _roster;
    private readonly Action<string> _show;

    public ControllerNotices(DeviceRoster roster, Action<string> show)
    {
        _roster = roster;
        _show = show;
        roster.Connected += OnConnected;
        roster.Disconnected += OnDisconnected;
    }

    public void Dispose()
    {
        _roster.Connected -= OnConnected;
        _roster.Disconnected -= OnDisconnected;
    }

    private void OnConnected(InputDeviceInfo device) => _show($"Controller connected: {device.Name}");

    private void OnDisconnected(InputDeviceInfo device) => _show($"Controller disconnected: {device.Name}");
}
