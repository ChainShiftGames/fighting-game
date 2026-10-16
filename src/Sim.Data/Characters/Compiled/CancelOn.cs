using System;

namespace Sim.Data.Characters.Compiled;

/// <summary>Which outcomes of the move open a cancel window.</summary>
[Flags]
public enum CancelOn
{
    None = 0,
    Hit = 1 << 0,
    Block = 1 << 1,
    Whiff = 1 << 2,
}
