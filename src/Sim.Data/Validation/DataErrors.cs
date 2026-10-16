using System.Collections.Generic;

namespace Sim.Data.Validation;

/// <summary>Collects every problem found while loading, so one run reports all of them instead of the first.</summary>
public sealed class DataErrors
{
    private readonly List<DataError> _errors = [];

    public int Count => _errors.Count;

    public IReadOnlyList<DataError> All => _errors;

    public void Add(string file, string field, string reason) => _errors.Add(new DataError(file, field, reason));

    /// <summary>Invalid data fails loudly (ARCHITECTURE.md §7): throws if anything was reported.</summary>
    public void ThrowIfAny()
    {
        if (_errors.Count > 0)
        {
            throw new DataValidationException(_errors.ToArray());
        }
    }
}
