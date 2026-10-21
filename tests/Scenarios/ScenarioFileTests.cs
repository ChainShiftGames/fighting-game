using System.IO;
using System.Text.RegularExpressions;
using Scenarios.Tests.Runner;
using Xunit;

namespace Scenarios.Tests;

/// <summary>Every <c>*.scenario</c> file under tests/Scenarios/ is one test case, named by its path.</summary>
/// <remarks>Scenarios that use testman are skipped while his tuning changes often (ADR-0036).</remarks>
public sealed class ScenarioFileTests
{
    // ADR-0036: set to false to run testman's scenarios again.
    private const bool TestmanOnHold = true;

    public static TheoryData<string> Files => [.. ScenarioPaths.ScenarioFiles()];

    [Theory]
    [MemberData(nameof(Files))]
    public void ScenarioPasses(string file)
    {
        if (TestmanOnHold && UsesTestman(file))
        {
            Assert.Skip("Testman scenarios are on hold (ADR-0036).");
        }

        ScenarioResult result = ScenarioRunner.RunFile(file);

        Assert.True(result.Passed, result.Report);
    }

    private static bool UsesTestman(string file) =>
        Regex.IsMatch(File.ReadAllText(ScenarioPaths.FullPath(file)), @"^\s*setup:\s*p[12]\s+character\s+testman\b",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);
}
