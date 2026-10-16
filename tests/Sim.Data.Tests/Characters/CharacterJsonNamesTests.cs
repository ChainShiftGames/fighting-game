using System;
using Sim.Characters;
using Sim.Data.Characters.Reading;
using Xunit;

namespace Sim.Data.Tests.Characters;

public sealed class CharacterJsonNamesTests
{
    // CharacterData indexes basic state moves by (int)BasicState, so the table must list each state at its value.
    [Fact]
    public void BasicStatesListEveryStateAtItsValue()
    {
        BasicState[] states = Enum.GetValues<BasicState>();

        Assert.Equal(states.Length, CharacterJsonNames.BasicStates.Length);
        for (int i = 0; i < states.Length; i++)
        {
            Assert.Equal((BasicState)i, CharacterJsonNames.BasicStates[i].State);
        }
    }
}
