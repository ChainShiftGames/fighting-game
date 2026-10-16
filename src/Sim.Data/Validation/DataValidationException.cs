using System;
using System.Collections.Generic;

namespace Sim.Data.Validation;

/// <summary>Thrown when game data is invalid; the message lists every error with file, field and reason.</summary>
public sealed class DataValidationException : Exception
{
    public DataValidationException(IReadOnlyList<DataError> errors)
        : base(FormatMessage(errors))
    {
        Errors = errors;
    }

    public IReadOnlyList<DataError> Errors { get; }

    private static string FormatMessage(IReadOnlyList<DataError> errors)
    {
        string[] lines = new string[errors.Count + 1];
        lines[0] = $"Game data is invalid ({errors.Count} error{(errors.Count == 1 ? "" : "s")}):";
        for (int i = 0; i < errors.Count; i++)
        {
            lines[i + 1] = "  " + errors[i];
        }

        return string.Join(Environment.NewLine, lines);
    }
}
