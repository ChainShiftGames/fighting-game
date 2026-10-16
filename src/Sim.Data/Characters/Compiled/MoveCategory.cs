using System;

namespace Sim.Data.Characters.Compiled;

/// <summary>
/// The kind of move. A move has exactly one; cancels combine several as a mask (normal → special → super).
/// </summary>
[Flags]
public enum MoveCategory
{
    None = 0,

    /// <summary>Started by the state machine itself, not by an input: idle, walk, crouch, dash, hitstun…</summary>
    Basic = 1 << 0,
    Normal = 1 << 1,
    Special = 1 << 2,
    Super = 1 << 3,
    Throw = 1 << 4,
}
