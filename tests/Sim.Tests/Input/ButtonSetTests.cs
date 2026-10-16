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
        Assert.True(buttons.TryFind("Dash", out int dash));
        Assert.Equal("Dash", buttons.NameOf(dash));
        Assert.Equal(1 << 9, buttons.MaskOf(dash));
        Assert.Equal(0b11_1111_0000, buttons.AllButtons);
    }

    [Fact]
    public void FiveButtonSetWorksWithoutCodeChanges()
    {
        ButtonSet buttons = TestButtonSets.FiveButtons();
        Assert.True(buttons.TryFind("Heavy", out int heavy));
        Assert.True(buttons.TryFind("Dash", out int dash));
        var input = new InputWord((ushort)(InputWord.Down | buttons.MaskOf(dash)));

        Assert.Equal(5, buttons.Count);
        Assert.False(buttons.TryFind("D", out _));
        Assert.True(buttons.IsHeld(input, dash));
        Assert.False(buttons.IsHeld(input, heavy));
        Assert.Equal(1 << 12, buttons.MaskOf(dash));
    }

    [Fact]
    public void UnknownNameIsNotFound()
    {
        ButtonSet buttons = TestButtonSets.SixButtons();

        Assert.False(buttons.TryFind("a", out int button));
        Assert.Equal(-1, button);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(16)]
    public void BitOutsideTheButtonBitsIsRejected(int bit)
    {
        Assert.Throws<ArgumentException>(() => new ButtonSet([new ButtonDefinition("A", bit)]));
    }

    [Fact]
    public void RepeatedBitIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => new ButtonSet([new ButtonDefinition("A", 4), new ButtonDefinition("B", 4)]));
    }

    [Fact]
    public void RepeatedNameIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => new ButtonSet([new ButtonDefinition("A", 4), new ButtonDefinition("A", 5)]));
    }

    [Fact]
    public void EmptyNameIsRejected()
    {
        Assert.Throws<ArgumentException>(() => new ButtonSet([new ButtonDefinition("", 4)]));
    }

    [Fact]
    public void EmptySetIsRejected()
    {
        Assert.Throws<ArgumentException>(() => new ButtonSet([]));
    }
}
