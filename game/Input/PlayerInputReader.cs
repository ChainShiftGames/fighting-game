using Sim.Input;

namespace FightingGame.Input;

/// <summary>All devices of one player, merged into that player's input word for the frame.</summary>
public sealed class PlayerInputReader
{
    private readonly IInputDevice[] _devices;

    public PlayerInputReader(params IInputDevice[] devices)
    {
        _devices = devices;
    }

    public InputWord Read()
    {
        ushort bits = 0;
        foreach (IInputDevice device in _devices)
        {
            bits |= device.Read().Bits;
        }

        return new InputWord(bits);
    }
}
