using FightingGame.Online;
using Xunit;

namespace FightingGame.Tests.Online;

public sealed class ReplayLaunchOptionsTests
{
    [Fact]
    public void ReadsTheReplayPath()
    {
        Assert.Equal(
            "user://replays/a.fgreplay",
            ReplayLaunchOptions.ParsePath(["--replay", "user://replays/a.fgreplay"], out string error));
        Assert.Empty(error);
    }

    [Fact]
    public void AsksForNothingWithoutTheOption()
    {
        Assert.Null(ReplayLaunchOptions.ParsePath(["--online"], out string error));
        Assert.Empty(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("--lag")]
    public void SaysWhenThePathIsMissing(string? nextOption)
    {
        string[] args = nextOption is null ? ["--replay"] : ["--replay", nextOption];

        Assert.Null(ReplayLaunchOptions.ParsePath(args, out string error));
        Assert.Contains("--replay", error, System.StringComparison.Ordinal);
    }
}
