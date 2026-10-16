using FightingGame.Input;
using Sim.Input;
using Xunit;

namespace FightingGame.Tests.Input;

public sealed class InputWordTextTests
{
    [Fact]
    public void ShowsDirectionsThenHeldButtonsByTheirIds()
    {
        ButtonSet buttons = TestButtons.Repository;
        var word = new InputWord((ushort)(InputWord.Left | InputWord.Right | buttons.Mask("a") | buttons.Mask("dash")));

        Assert.Equal("L R | a dash", InputWordText.Describe(word, buttons));
    }

    [Fact]
    public void ShowsNeutralAsADash()
    {
        Assert.Equal("-", InputWordText.Describe(InputWord.Neutral, TestButtons.Repository));
    }
}
