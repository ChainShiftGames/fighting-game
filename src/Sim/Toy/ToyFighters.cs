using System.Runtime.CompilerServices;

namespace Sim.Toy;

/// <summary>Both fighters as a fixed-size inline array, so the state stays one flat block of integers.</summary>
[InlineArray(Count)]
public struct ToyFighters
{
    public const int Count = 2;

    private ToyFighter _element;
}
