using System;
using Sim.Input;
using Xunit;

namespace Sim.Tests.Input;

public sealed class ButtonSetTests
{
    [Fact]
    public void SixButtonSetFollowsItsDefinitions()
    {
        ButtonSet buttons = TestButtonSets.SixButtons();

        Assert.Equal(6, buttons.Count);
        Assert.True(buttons.TryFind("dash", out int dash));
        Assert.Equal("dash", buttons.IdOf(dash));
        Assert.Equal(1 << 9, buttons.MaskOf(dash));
        Assert.Equal(0b11_1111_0000, buttons.AllButtons);
    }

    [Fact]
    public void FiveButtonSetWorksWithoutCodeChanges()
    {
        ButtonSet buttons = TestButtonSets.FiveButtons();
        Assert.True(buttons.TryFind("heavy", out int heavy));
        Assert.True(buttons.TryFind("dash", out int dash));
        var input = new InputWord((ushort)(InputWord.Down | buttons.MaskOf(dash)));

        Assert.Equal(5, buttons.Count);
        Assert.False(buttons.TryFind("d", out _));
        Assert.True(buttons.IsHeld(input, dash));
        Assert.False(buttons.IsHeld(input, heavy));
        Assert.Equal(1 << 12, buttons.MaskOf(dash));
    }

    [Fact]
    public void UnknownIdIsNotFound()
    {
        ButtonSet buttons = TestButtonSets.SixButtons();

        Assert.False(buttons.TryFind("A", out int button));
        Assert.Equal(-1, button);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(16)]
    public void BitOutsideTheButtonBitsIsRejected(int bit)
    {
        Assert.Throws<ArgumentException>(() => new ButtonSet([new ButtonDefinition("a", bit)]));
    }

    [Fact]
    public void RepeatedBitIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => new ButtonSet([new ButtonDefinition("a", 4), new ButtonDefinition("b", 4)]));
    }

    [Fact]
    public void RepeatedIdIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => new ButtonSet([new ButtonDefinition("a", 4), new ButtonDefinition("a", 5)]));
    }

    [Fact]
    public void EmptyIdIsRejected()
    {
        Assert.Throws<ArgumentException>(() => new ButtonSet([new ButtonDefinition("", 4)]));
    }

    [Fact]
    public void EmptySetIsRejected()
    {
        Assert.Throws<ArgumentException>(() => new ButtonSet([]));
    }
}
