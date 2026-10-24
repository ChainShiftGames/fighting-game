using System.Collections.Generic;
using Sim.Input;

namespace FightingGame.Input;

/// <summary>All devices one player is assigned, merged into that player's input word for the frame.</summary>
/// <remarks>
/// The assignment is read again each frame, so a controller that reconnects or takes a disconnected one's place
/// counts from the next frame on, with no frame of delay.
/// </remarks>
public sealed class PlayerInputReader
{
    private readonly int _player;
    private readonly PlayerDevices _assignment;
    private readonly InputDeviceReader _devices;

    /// <param name="player">The local player: their place in <paramref name="assignment"/>.</param>
    public PlayerInputReader(int player, PlayerDevices assignment, InputDeviceReader devices)
    {
        _player = player;
        _assignment = assignment;
        _devices = devices;
    }

    /// <summary>
    /// One reader per player in <paramref name="assignment"/>. Called as a match starts, so it also compiles each
    /// device's layout as it is now (<see cref="InputDeviceReader.Resolve"/>).
    /// </summary>
    public static PlayerInputReader[] All(PlayerDevices assignment, InputDeviceReader devices)
    {
        devices.Resolve();
        var players = new PlayerInputReader[assignment.PlayerCount];
        for (int player = 0; player < players.Length; player++)
        {
            players[player] = new PlayerInputReader(player, assignment, devices);
        }

        return players;
    }

    // Indexed, not foreach: an interface enumerator would allocate every frame.
    public InputWord Read()
    {
        IReadOnlyList<InputDeviceId> assigned = _assignment.Of(_player);
        ushort bits = 0;
        for (int index = 0; index < assigned.Count; index++)
        {
            bits |= _devices.Read(assigned[index]).Bits;
        }

        return new InputWord(bits);
    }
}
