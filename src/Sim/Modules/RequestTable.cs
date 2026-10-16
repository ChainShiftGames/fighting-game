using System;

namespace Sim.Modules;

/// <summary>Fixed-size table of one kind of core request for one frame, at most one per key.</summary>
/// <remarks>
/// Keys come from <see cref="CoreRequests"/> and grow with the module slot, so reading keys in ascending order is the
/// fixed order the core applies requests in. Allocated once at match start.
/// </remarks>
public sealed class RequestTable<TRequest>
    where TRequest : struct
{
    private readonly TRequest[] _requests;
    private readonly bool[] _isSet;

    public RequestTable(int capacity)
    {
        _requests = new TRequest[capacity];
        _isSet = new bool[capacity];
    }

    public int Capacity => _requests.Length;

    /// <summary>Records a request; a later request with the same key in the same frame replaces it.</summary>
    public void Set(int key, in TRequest request)
    {
        _requests[key] = request;
        _isSet[key] = true;
    }

    public bool TryGet(int key, out TRequest request)
    {
        request = _requests[key];
        return _isSet[key];
    }

    public void Clear()
    {
        Array.Clear(_requests);
        Array.Clear(_isSet);
    }
}
