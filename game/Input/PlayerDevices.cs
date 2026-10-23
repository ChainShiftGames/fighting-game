using System;
using System.Collections.Generic;

namespace FightingGame.Input;

/// <summary>
/// Which input devices each player on this PC reads (ADR-0038). A device belongs to at most one player, and stays
/// assigned while disconnected so it goes back to its player when it reconnects.
/// </summary>
public sealed class PlayerDevices
{
    private readonly List<InputDeviceId>[] _devices;

    /// <param name="claimsNewJoypads">
    /// Whether a joypad that connects unassigned goes to a player by itself (see <see cref="ControllerReconnect"/>):
    /// the default assignment's way, as before players chose their devices.
    /// </param>
    public PlayerDevices(int playerCount, bool claimsNewJoypads = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(playerCount);
        _devices = new List<InputDeviceId>[playerCount];
        for (int player = 0; player < playerCount; player++)
        {
            _devices[player] = [];
        }

        ClaimsNewJoypads = claimsNewJoypads;
    }

    public int PlayerCount => _devices.Length;

    public bool ClaimsNewJoypads { get; }

    /// <summary>The devices <paramref name="player"/> reads, connected or not, in the order they were given.</summary>
    public IReadOnlyList<InputDeviceId> Of(int player) => _devices[player];

    /// <summary>The player who reads <paramref name="device"/>, or -1 when nobody does.</summary>
    public int PlayerOf(InputDeviceId device)
    {
        for (int player = 0; player < _devices.Length; player++)
        {
            if (_devices[player].Contains(device))
            {
                return player;
            }
        }

        return -1;
    }

    public bool HasJoypad(int player) => _devices[player].Exists(device => device.IsJoypad);

    /// <summary>Gives <paramref name="device"/> to <paramref name="player"/>, taking it from whoever had it.</summary>
    public void Assign(int player, InputDeviceId device)
    {
        Release(device);
        _devices[player].Add(device);
    }

    /// <summary>
    /// <paramref name="player"/> reads <paramref name="replacement"/> in the place of <paramref name="device"/>,
    /// taking it from whoever had it.
    /// </summary>
    public void Replace(int player, InputDeviceId device, InputDeviceId replacement)
    {
        if (!_devices[player].Contains(device))
        {
            throw new ArgumentException($"Player {player + 1} doesn't read {device}.", nameof(device));
        }

        Release(replacement);
        _devices[player][_devices[player].IndexOf(device)] = replacement;
    }

    private void Release(InputDeviceId device)
    {
        foreach (List<InputDeviceId> devices in _devices)
        {
            devices.Remove(device);
        }
    }
}
