using System;

namespace Netcode.Desync;

/// <summary>Compares this PC's checksum of each confirmed frame with the other PC's, whichever arrives first.</summary>
/// <remarks>
/// Both sides' checksums arrive in frame order, so the first mismatch found is the first frame that differs.
/// Pending frames sit in a fixed ring, so comparing allocates nothing.
/// </remarks>
public sealed class ChecksumComparer
{
    private readonly Slot[] _slots;
    private int _lastAgreedFrame;

    /// <param name="capacity">How far one side's checksums may run ahead of the other's, in frames.</param>
    public ChecksumComparer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _slots = new Slot[capacity];
    }

    /// <summary>The newest frame both PCs computed the same checksum for; 0 before the first.</summary>
    public int LastAgreedFrame => _lastAgreedFrame;

    /// <summary>Adds this PC's checksum; true with the mismatch when the other PC's differs.</summary>
    public bool AddLocal(FrameChecksum checksum, out ChecksumMismatch mismatch) =>
        Add(checksum, isLocal: true, out mismatch);

    /// <summary>Adds the other PC's checksum; true with the mismatch when this PC's differs.</summary>
    public bool AddRemote(FrameChecksum checksum, out ChecksumMismatch mismatch) =>
        Add(checksum, isLocal: false, out mismatch);

    private bool Add(FrameChecksum checksum, bool isLocal, out ChecksumMismatch mismatch)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(checksum.Frame);
        ref Slot slot = ref _slots[checksum.Frame % _slots.Length];
        if (slot.Frame != checksum.Frame)
        {
            ThrowIfNeverCompared(in slot, checksum.Frame);
            slot = new Slot { Frame = checksum.Frame };
        }

        if (isLocal)
        {
            slot.Local = checksum.Checksum;
            slot.HasLocal = true;
        }
        else
        {
            slot.Remote = checksum.Checksum;
            slot.HasRemote = true;
        }

        mismatch = default;
        if (!slot.HasLocal || !slot.HasRemote)
        {
            return false;
        }

        if (slot.Local == slot.Remote)
        {
            _lastAgreedFrame = Math.Max(_lastAgreedFrame, slot.Frame);
            return false;
        }

        mismatch = new ChecksumMismatch(slot.Frame, slot.Local, slot.Remote, _lastAgreedFrame);
        return true;
    }

    // Skipping a frame would let a desync pass unseen (ARCHITECTURE.md §8), so a ring too small fails loudly.
    private static void ThrowIfNeverCompared(in Slot slot, int newFrame)
    {
        if (slot.Frame != 0 && !(slot.HasLocal && slot.HasRemote))
        {
            throw new InvalidOperationException(
                $"Frame {slot.Frame}'s checksums were never compared before frame {newFrame} needed its slot: one PC " +
                $"is at least {newFrame - slot.Frame} frames ahead. Raise the desync history length.");
        }
    }

    private struct Slot
    {
        public int Frame;
        public ulong Local;
        public ulong Remote;
        public bool HasLocal;
        public bool HasRemote;
    }
}
