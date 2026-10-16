namespace Sim.Data.Validation;

/// <summary>One problem in a data file: which file, which field and why it is wrong.</summary>
/// <param name="File">Path relative to the data root, with '/' separators.</param>
/// <param name="Field">JSON path inside the file (e.g. <c>boxes[2].lastFrame</c>); empty for the whole file.</param>
public sealed record DataError(string File, string Field, string Reason)
{
    public override string ToString() =>
        Field.Length == 0 ? $"{File}: {Reason}" : $"{File}: {Field}: {Reason}";
}
