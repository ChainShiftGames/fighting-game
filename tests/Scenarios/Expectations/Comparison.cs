namespace Scenarios.Tests.Expectations;

/// <summary>How an expected value is compared with the actual one: <c>= != &lt; &lt;= &gt; &gt;=</c>.</summary>
public readonly record struct Comparison(string Symbol)
{
    public const string All = "=, !=, <, <=, >, >=";

    /// <summary>Only meaningful for ordered values (positions, frame counts), not for facing or input.</summary>
    public bool IsOrdering => Symbol is not ("=" or "!=");

    public static bool TryParse(string text, out Comparison comparison)
    {
        comparison = new Comparison(text);
        return text is "=" or "!=" or "<" or "<=" or ">" or ">=";
    }

    public bool Holds(int actual, int expected) => Symbol switch
    {
        "=" => actual == expected,
        "!=" => actual != expected,
        "<" => actual < expected,
        "<=" => actual <= expected,
        ">" => actual > expected,
        _ => actual >= expected,
    };
}
