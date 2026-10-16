using Scenarios.Tests.Runner;
using Sim.Input;
using Xunit;

namespace Scenarios.Tests.RunnerTests;

/// <summary>Numpad notation as players write it: facing-relative directions plus button ids.</summary>
public sealed class InputNotationTests
{
    [Theory]
    [InlineData("5", 0)]
    [InlineData("6", InputWord.Right)]
    [InlineData("1", InputWord.Down | InputWord.Left)]
    [InlineData("9", InputWord.Up | InputWord.Right)]
    [InlineData("4+6", InputWord.Left | InputWord.Right)]
    [InlineData("a", 1 << 4)]
    [InlineData("2c+d", InputWord.Down | (1 << 6) | (1 << 7))]
    [InlineData("6dash", InputWord.Right | (1 << 9))]
    public void ParsesDirectionsAndButtons(string text, int bits)
    {
        Assert.Equal(new InputWord((ushort)bits), InputNotation.Parse(text));
    }

    [Theory]
    [InlineData("5")]
    [InlineData("3")]
    [InlineData("5a+b")]
    [InlineData("6special")]
    [InlineData("4+6")]
    [InlineData("4+6a+dash")]
    public void FormatReadsBackAsWritten(string text)
    {
        Assert.Equal(text, InputNotation.Format(InputNotation.Parse(text)));
    }

    [Fact]
    public void SpareBitsAreShownByNumber()
    {
        Assert.Equal("5bit12", InputNotation.Format(new InputWord(1 << 12)));
    }

    [Fact]
    public void ForwardIsLeftForAFighterFacingLeft()
    {
        InputWord forwardA = InputNotation.Parse("6a");

        Assert.Equal(InputNotation.Parse("4a"), InputNotation.ForFacing(forwardA, facing: -1));
        Assert.Equal(forwardA, InputNotation.ForFacing(forwardA, facing: 1));
    }
}
