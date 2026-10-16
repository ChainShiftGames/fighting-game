using System.Collections.Generic;

namespace Sim.Data.Input;

/// <summary>The button set from data (ADR-0010): turns a button id from a move file into its input word bit.</summary>
public interface IButtonLookup
{
    /// <summary>All button ids in bit order, for error messages.</summary>
    IReadOnlyList<string> ButtonIds { get; }

    bool TryGetMask(string buttonId, out int mask);
}
