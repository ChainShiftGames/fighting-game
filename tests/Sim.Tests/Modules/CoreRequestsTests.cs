using System;
using Sim.Modules;
using Sim.State;
using Xunit;

namespace Sim.Tests.Modules;

public sealed class CoreRequestsTests
{
    [Fact]
    public void FighterKeysFollowTheModuleOrder()
    {
        Assert.True(CoreRequests.FighterKey(0, 1) < CoreRequests.FighterKey(1, 0));
        Assert.Equal(
            (ModuleStates.MaxModules * MatchState.PlayerCount) - 1,
            CoreRequests.FighterKey(ModuleStates.MaxModules - 1, MatchState.PlayerCount - 1));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(MatchState.PlayerCount)]
    public void AnOutOfRangeFighterIsRejectedInsteadOfLandingInAnotherSlot(int fighter)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CoreRequests.FighterKey(0, fighter));
    }

    [Fact]
    public void ALaterRequestFromTheSameSlotReplacesTheEarlierOne()
    {
        var requests = new CoreRequests();

        requests.HitstopOverrides.Set(2, new HitstopOverrideRequest { Frames = 5 });
        requests.HitstopOverrides.Set(2, new HitstopOverrideRequest { Frames = 8 });

        Assert.True(requests.HitstopOverrides.TryGet(2, out HitstopOverrideRequest request));
        Assert.Equal(8, request.Frames);
    }

    [Fact]
    public void ClearRemovesEveryRequest()
    {
        var requests = new CoreRequests();
        requests.Cancels.Set(0, new CancelRequest { Priority = 1 });
        requests.TimeScales.Set(1, new TimeScaleRequest { Denominator = 2 });
        requests.HitstopOverrides.Set(2, new HitstopOverrideRequest { Frames = 3 });
        requests.Freezes.Set(3, new FreezeRequest { Frames = 4 });
        requests.StunOverrides.Set(4, new StunOverrideRequest { Frames = 5 });
        requests.Events.Set(5, new ModuleEventRequest { Code = 6 });

        requests.Clear();

        Assert.False(requests.StunOverrides.TryGet(4, out _));
        Assert.False(requests.Events.TryGet(5, out _));
        Assert.False(requests.Cancels.TryGet(0, out _));
        Assert.False(requests.TimeScales.TryGet(1, out _));
        Assert.False(requests.HitstopOverrides.TryGet(2, out _));
        Assert.False(requests.Freezes.TryGet(3, out FreezeRequest freeze));
        Assert.Equal(0, freeze.Frames);
    }
}
