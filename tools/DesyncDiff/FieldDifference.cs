namespace DesyncDiff;

/// <summary>One state field whose value differs between the two dumps; null where a dump lacks the field.</summary>
public sealed record FieldDifference(string Field, long? First, long? Second);
