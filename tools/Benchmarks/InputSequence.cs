using System;
using Sim.Input;

namespace Benchmarks;

/// <summary>A fixed, seeded list of input words for both players, so every run measures the same frames.</summary>
/// <remarks>
/// Each frame holds a random direction (all four bits, so opposite directions, SOCD, are exercised too) and now and
/// then presses a button, so the fighters attack, hit and block like a match.
/// </remarks>
internal sealed class InputSequence
{
    private const int Length = 4096;
    private const int DirectionBitCount = 4;

    // The default button set's six buttons (ADR-0010); one in four frames presses one of them.
    private const int ButtonCount = 6;
    private const int ButtonPressChance = 4;

    private readonly InputWord[] _player1 = new InputWord[Length];
    private readonly InputWord[] _player2 = new InputWord[Length];

    public InputSequence(int seed)
    {
        // System.Random is fine here: it only generates inputs up front and never runs inside the Sim.
        var random = new Random(seed);
        for (int frame = 0; frame < Length; frame++)
        {
            _player1[frame] = Next(random);
            _player2[frame] = Next(random);
        }
    }

    public InputWord Player1At(int frame) => _player1[frame % Length];

    public InputWord Player2At(int frame) => _player2[frame % Length];

    private static InputWord Next(Random random)
    {
        int bits = random.Next(1 << DirectionBitCount);
        if (random.Next(ButtonPressChance) == 0)
        {
            bits |= 1 << (InputWord.FirstButtonBit + random.Next(ButtonCount));
        }

        return new InputWord((ushort)bits);
    }
}
