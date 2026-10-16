using Netcode.Drivers;
using Sim.Input;

namespace Netcode.Tests.Matches;

/// <summary>Plays the source for a number of frames, then lets go of everything.</summary>
/// <remarks>Lets two peers settle: once inputs stay neutral, every frame both states depend on gets confirmed.</remarks>
internal sealed class NeutralAfter : IInputSource
{
    private readonly IInputSource _source;
    private int _framesLeft;

    public NeutralAfter(IInputSource source, int frames)
    {
        _source = source;
        _framesLeft = frames;
    }

    public InputWord Next()
    {
        if (_framesLeft == 0)
        {
            return InputWord.Neutral;
        }

        _framesLeft--;
        return _source.Next();
    }
}
