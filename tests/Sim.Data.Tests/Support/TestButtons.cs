using System;
using System.Collections.Generic;
using Sim.Data.Input;

namespace Sim.Data.Tests.Support;

/// <summary>The ADR-0010 button set (A B C D Special Dash after the 4 direction bits), until P1-05's loader exists.</summary>
public sealed class TestButtons : IButtonLookup
{
    private const int FirstButtonBit = 4;

    public IReadOnlyList<string> ButtonIds { get; } = ["a", "b", "c", "d", "special", "dash"];

    public bool TryGetMask(string buttonId, out int mask)
    {
        for (int i = 0; i < ButtonIds.Count; i++)
        {
            if (string.Equals(ButtonIds[i], buttonId, StringComparison.Ordinal))
            {
                mask = 1 << (FirstButtonBit + i);
                return true;
            }
        }

        mask = 0;
        return false;
    }
}
