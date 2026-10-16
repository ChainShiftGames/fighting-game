using System;
using Sim.Input;

namespace FightingGame.Loop;

/// <summary>A running match's 60 Hz loop, whatever its state type (toy online match, real local match).</summary>
public interface IMatchLoop
{
    /// <summary>The logic frame the match is at.</summary>
    int CurrentFrame { get; }

    /// <summary>The raw words read for the last logic frame, one per local player (for the developer overlay).</summary>
    ReadOnlySpan<InputWord> LastInputs { get; }

    /// <summary>Runs every logic frame due at <paramref name="timestamp"/>; returns how many ran.</summary>
    int Advance(long timestamp);
}
