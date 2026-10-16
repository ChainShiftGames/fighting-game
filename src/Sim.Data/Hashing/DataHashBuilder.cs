using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Sim.Data.Hashing;

/// <summary>Feeds compact game data into the <see cref="DataHash"/> in a fixed, platform-independent byte form.</summary>
/// <remarks>
/// Only compact data is hashed, never file bytes, file names or paths: line endings, whitespace, key order, a byte
/// order mark or the operating system can't change the hash, only values the game logic sees can. Every sequence
/// is prefixed with its length so neighbouring values can't run into each other.
/// </remarks>
public sealed class DataHashBuilder : IDisposable
{
    private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

    public void Add(int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        _hash.AppendData(bytes);
    }

    /// <summary>Ids only; text that players read (display names) stays out of the hash.</summary>
    public void Add(string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        Add(bytes.Length);
        _hash.AppendData(bytes);
    }

    /// <summary>
    /// Hashes the values' memory directly. <typeparamref name="T"/> must contain only 4-byte fields (ints, enums,
    /// nested such structs), so there is no padding; Sim.Data.Tests checks this for every compact type.
    /// </summary>
    public void Add<T>(ReadOnlySpan<T> values)
        where T : unmanaged
    {
        // All supported platforms (x64 Windows, Linux, Steam Deck) are little-endian; refuse rather than differ.
        if (!BitConverter.IsLittleEndian)
        {
            throw new PlatformNotSupportedException("The data hash assumes a little-endian platform.");
        }

        Add(values.Length);
        _hash.AppendData(MemoryMarshal.AsBytes(values));
    }

    public void Add<T>(in T value)
        where T : unmanaged => Add(new ReadOnlySpan<T>(in value));

    public DataHash Finish()
    {
        Span<byte> bytes = stackalloc byte[DataHash.ByteLength];
        _hash.GetHashAndReset(bytes);
        return DataHash.FromBytes(bytes);
    }

    public void Dispose() => _hash.Dispose();
}
