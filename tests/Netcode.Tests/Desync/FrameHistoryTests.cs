using System;
using Netcode.Desync;
using Sim.Input;
using Xunit;

namespace Netcode.Tests.Desync;

public sealed class FrameHistoryTests
{
    [Fact]
    public void AResimulatedFrameReplacesTheOneRecordedBefore()
    {
        var history = new FrameHistory<int>(8);
        int first = 30;
        int resimulated = 31;
        history.Record(3, in first, InputWord.Neutral, InputWord.Neutral);
        history.Record(3, in resimulated, new InputWord(InputWord.Up), InputWord.Neutral);

        ref readonly FrameRecord<int> record = ref history.Get(3);
        Assert.Equal(31, record.State);
        Assert.Equal(new InputWord(InputWord.Up), record.Player1);
    }

    [Fact]
    public void OnlyTheLastCapacityFramesAreKept()
    {
        var history = new FrameHistory<int>(4);
        for (int frame = 1; frame <= 10; frame++)
        {
            history.Record(frame, in frame, InputWord.Neutral, InputWord.Neutral);
        }

        Assert.False(history.Contains(6));
        Assert.True(history.Contains(7));
        Assert.True(history.Contains(10));
        Assert.Throws<InvalidOperationException>(() => history.Get(6));
    }
}
