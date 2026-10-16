using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scenarios.Tests.Expectations;

/// <summary>Enum members as scenarios write them, matching data ids: <c>WalkForward</c> is <c>walk_forward</c>.</summary>
public static class SnakeCaseNames
{
    public static string Of<TEnum>(TEnum value)
        where TEnum : struct, Enum => ToSnakeCase(value.ToString());

    /// <exception cref="FormatException">No member has that name; the message lists the names.</exception>
    public static TEnum Parse<TEnum>(string text, string what, params TEnum[] excluded)
        where TEnum : struct, Enum
    {
        foreach (TEnum value in Members(excluded))
        {
            if (Of(value) == text)
            {
                return value;
            }
        }

        throw new FormatException($"'{text}' isn't a {what}; use one of {string.Join(", ", Members(excluded).Select(Of))}");
    }

    private static IEnumerable<TEnum> Members<TEnum>(TEnum[] excluded)
        where TEnum : struct, Enum => Enum.GetValues<TEnum>().Where(value => !excluded.Contains(value));

    private static string ToSnakeCase(string pascalCase)
    {
        StringBuilder name = new();
        for (int i = 0; i < pascalCase.Length; i++)
        {
            if (i > 0 && char.IsUpper(pascalCase[i]))
            {
                name.Append('_');
            }

            name.Append(char.ToLowerInvariant(pascalCase[i]));
        }

        return name.ToString();
    }
}
