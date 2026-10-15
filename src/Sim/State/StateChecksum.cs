using System;
using System.Runtime.InteropServices;

namespace Sim.State;

/// <summary>Checksum of a game state: a hash over its raw bytes, so both PCs can compare states cheaply.</summary>
public static class StateChecksum
{
    // FNV-1a, 64-bit: simple, allocation-free and identical on every platform.
    private const ulong OffsetBasis = 14695981039346656037;
    private const ulong Prime = 1099511628211;

    /// <remarks>
    /// <typeparamref name="TState"/> must be plain data without padding (only integer fields), otherwise padding
    /// bytes would make equal states hash differently.
    /// </remarks>
    public static ulong Compute<TState>(ref readonly TState state)
        where TState : unmanaged
    {
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(new ReadOnlySpan<TState>(in state));
        ulong hash = OffsetBasis;
        for (int i = 0; i < bytes.Length; i++)
        {
            hash = (hash ^ bytes[i]) * Prime;
        }

        return hash;
    }
}
