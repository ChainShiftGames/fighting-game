using Netcode.Drivers;
using Sim.Input;

namespace Netcode.Tests.Online;

/// <summary>Walks back, stands, walks forward, over and over: no two frames in a row have the same input.</summary>
internal sealed class ChangingInputs : IInputSource
{
    private static readonly ushort[] _cycle = [InputWord.Left, 0, InputWord.Right];
    private int _next;

    public InputWord Next() => new(_cycle[_next++ % _cycle.Length]);
}
