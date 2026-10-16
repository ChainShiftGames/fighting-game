using System;

namespace Sim.Input.Motions;

/// <summary>One step of a motion: the numpad directions that count for it (just 2, or any of 1 2 3).</summary>
/// <remarks>Directions are relative to the fighter's facing (<see cref="NumpadDirection"/>). Bit d is direction d.</remarks>
public readonly record struct MotionStep(int Directions)
{
    /// <summary>Bits 1–9: every numpad direction.</summary>
    public const int AllDirections = 0b11_1111_1110;

    /// <summary>Accepts at least one direction and nothing outside numpad 1–9.</summary>
    public bool IsValid => Directions != 0 && (Directions & ~AllDirections) == 0;

    /// <summary>A step accepting each of <paramref name="directions"/>. For loading data, not for per-frame logic.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A direction is outside numpad 1–9.</exception>
    public static MotionStep Of(params ReadOnlySpan<int> directions)
    {
        int mask = 0;
        foreach (int direction in directions)
        {
            if (direction is < 1 or > 9)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(directions), direction, "Numpad directions go from 1 to 9.");
            }

            mask |= 1 << direction;
        }

        return new MotionStep(mask);
    }

    public bool Accepts(int direction) => (Directions & (1 << direction)) != 0;

    /// <summary>Both steps accept a common direction.</summary>
    public bool Overlaps(MotionStep other) => (Directions & other.Directions) != 0;
}
