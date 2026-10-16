using Sim.State;
using Xunit;

namespace Sim.Tests.State;

public sealed class RandomStateTests
{
    // Reference value: the upper half of SplitMix64's first output for seed 0 (0xE220A8397B1DCDAF). Pins the
    // algorithm, so a change to it can't slip in and silently change every seeded match.
    [Fact]
    public void MatchesTheSplitMix64ReferenceSequence()
    {
        RandomState random = RandomState.FromSeed(0);

        Assert.Equal(0xE220A839u, random.NextUInt());
    }

    [Fact]
    public void RestoringACopyRepeatsTheSameNumbers()
    {
        RandomState random = RandomState.FromSeed(1234);
        random.NextUInt();
        RandomState snapshot = random;

        uint first = random.NextUInt();
        int second = random.NextBelow(100);
        random = snapshot;

        Assert.Equal(first, random.NextUInt());
        Assert.Equal(second, random.NextBelow(100));
    }

    [Fact]
    public void DifferentSeedsGiveDifferentSequences()
    {
        RandomState first = RandomState.FromSeed(1);
        RandomState second = RandomState.FromSeed(2);

        Assert.NotEqual(first.NextUInt(), second.NextUInt());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(int.MaxValue)]
    public void NextBelowStaysInRange(int exclusiveMax)
    {
        RandomState random = RandomState.FromSeed(99);
        for (int draw = 0; draw < 1000; draw++)
        {
            Assert.InRange(random.NextBelow(exclusiveMax), 0, exclusiveMax - 1);
        }
    }

    [Fact]
    public void NextBelowReachesEveryValue()
    {
        RandomState random = RandomState.FromSeed(7);
        bool[] seen = new bool[6];
        for (int draw = 0; draw < 200; draw++)
        {
            seen[random.NextBelow(seen.Length)] = true;
        }

        Assert.All(seen, Assert.True);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void NextBelowAnEmptyRangeIsZero(int exclusiveMax)
    {
        RandomState random = RandomState.FromSeed(3);

        Assert.Equal(0, random.NextBelow(exclusiveMax));
    }
}
