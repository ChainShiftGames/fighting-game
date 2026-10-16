using Scenarios.Tests.Runner;
using Xunit;

namespace Scenarios.Tests;

/// <summary>Every <c>*.scenario</c> file under tests/Scenarios/ is one test case, named by its path.</summary>
public sealed class ScenarioFileTests
{
    public static TheoryData<string> Files => [.. ScenarioPaths.ScenarioFiles()];

    [Theory]
    [MemberData(nameof(Files))]
    public void ScenarioPasses(string file)
    {
        ScenarioResult result = ScenarioRunner.RunFile(file);

        Assert.True(result.Passed, result.Report);
    }
}
