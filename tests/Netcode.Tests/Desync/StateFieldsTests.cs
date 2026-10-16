using System;
using System.Collections.Generic;
using Netcode.Desync;
using Sim.Input;
using Sim.Math;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Desync;

public sealed class StateFieldsTests
{
    [Fact]
    public void EveryMatchStateFieldIsNamedWithItsValue()
    {
        MatchState state = default;
        state.Frame = 42;
        state.Fighters[1].Health = 9876;
        state.Fighters[0].Position = new IntVector2(-1234, 0);
        state.Inputs[1].Record(new InputWord(InputWord.Down));

        Dictionary<string, long> fields = StateFields.Read(in state);

        Assert.Equal(42, fields["Frame"]);
        Assert.Equal(9876, fields["Fighters[1].Health"]);
        Assert.Equal(-1234, fields["Fighters[0].Position.X"]);
        Assert.Equal((long)InputWord.Down, fields["Inputs[1]._frames[1].Bits"]);
        Assert.Equal(0, fields["Fighters[0].Health"]);
    }

    [Fact]
    public void AStateWithPaddingIsRefused()
    {
        Padded state = default;
        Assert.Throws<InvalidOperationException>(() => StateFields.Read(in state));
    }

    private readonly record struct Padded(byte Small, int Large);
}
