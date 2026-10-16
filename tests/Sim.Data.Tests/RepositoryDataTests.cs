using Sim.Data.Sources;
using Sim.Data.Tests.Support;
using Xunit;

namespace Sim.Data.Tests;

/// <summary>The repository's real data/ folder always loads; a broken data file fails the PR that adds it.</summary>
public sealed class RepositoryDataTests
{
    [Fact]
    public void RepositoryDataIsValid()
    {
        GameData data = Fixture.Load(new DirectoryDataSource(Fixture.RepositoryDataRoot));

        Assert.NotNull(data.Characters);
    }
}
