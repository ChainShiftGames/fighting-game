using System;
using Sim.Input;

namespace Benchmarks;

/// <summary>A fixed, seeded list of input words for both players, so every run measures the same frames.</summary>
internal sealed class InputSequence
{
    private const int Length = 4096;

    // All four direction bits, so opposite directions (SOCD) are exercised too.
    private const int DirectionBitCount = 4;

    private readonly InputWord[] _player1 = new InputWord[Length];
    private readonly InputWord[] _player2 = new InputWord[Length];

    public InputSequence(int seed)
    {
        // System.Random is fine here: it only generates inputs up front and never runs inside the Sim.
        var random = new Random(seed);
        for (int frame = 0; frame < Length; frame++)
        {
            _player1[frame] = new InputWord((ushort)random.Next(1 << DirectionBitCount));
            _player2[frame] = new InputWord((ushort)random.Next(1 << DirectionBitCount));
        }
    }

    public InputWord Player1At(int frame) => _player1[frame % Length];

    public InputWord Player2At(int frame) => _player2[frame % Length];
}
