using System;
using Netcode.Drivers;
using Sim.Input;

namespace Netcode.Tests.Drivers;

/// <summary>Random input words from a fixed seed, so every run feeds the match the same frames.</summary>
internal sealed class SeededInputs : IInputSource
{
    private static readonly ushort[] _directions =
    [
        0,
        InputWord.Left,
        InputWord.Right,
        InputWord.Up,
        InputWord.Up | InputWord.Left,
        InputWord.Up | InputWord.Right,
        InputWord.Left | InputWord.Right,
    ];

    private readonly Random _random;

    public SeededInputs(int seed)
    {
        _random = new Random(seed);
    }

    public InputWord Next() => new(_directions[_random.Next(_directions.Length)]);
}
