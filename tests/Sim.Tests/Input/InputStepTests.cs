using Sim.Input;
using Sim.State;
using Xunit;

namespace Sim.Tests.Input;

public sealed class InputStepTests
{
    private readonly ButtonSet _buttons = TestButtonSets.SixButtons();

    [Fact]
    public void RawLeftAndRightGiveNeutral()
    {
        InputWord recorded = RecordForBothPlayers(new InputWord(InputWord.Left | InputWord.Right));

        Assert.Equal(InputWord.Neutral, recorded);
    }

    [Fact]
    public void RawUpAndDownGiveUp()
    {
        InputWord recorded = RecordForBothPlayers(new InputWord(InputWord.Up | InputWord.Down));

        Assert.Equal(new InputWord(InputWord.Up), recorded);
    }

    [Fact]
    public void ButtonsAreKeptWhileDirectionsResolve()
    {
        Assert.True(_buttons.TryFind("a", out int a));
        ushort buttonA = _buttons.MaskOf(a);

        InputWord recorded = RecordForBothPlayers(
            new InputWord((ushort)(InputWord.Up | InputWord.Down | InputWord.Right | buttonA)));

        Assert.Equal(new InputWord((ushort)(InputWord.Up | InputWord.Right | buttonA)), recorded);
    }

    [Fact]
    public void BitsOutsideTheButtonSetAreDropped()
    {
        const ushort SpareBit = 1 << 15;

        InputWord recorded = RecordForBothPlayers(new InputWord(InputWord.Left | SpareBit));

        Assert.Equal(new InputWord(InputWord.Left), recorded);
    }

    [Fact]
    public void FiveButtonSetRecordsItsOwnBits()
    {
        ButtonSet buttons = TestButtonSets.FiveButtons();
        var step = new InputStep(buttons);
        Assert.True(buttons.TryFind("dash", out int dash));
        // Bit 9 is Dash in the default set but unused here.
        var raw = new InputWord((ushort)(buttons.MaskOf(dash) | (1 << 9)));
        InputHistories histories = default;

        step.Run(ref histories, raw, InputWord.Neutral);

        Assert.True(buttons.IsHeld(histories[0].FramesAgo(0), dash));
        Assert.Equal(new InputWord(buttons.MaskOf(dash)), histories[0].FramesAgo(0));
    }

    [Fact]
    public void EachRunRecordsOneFramePerPlayer()
    {
        var step = new InputStep(_buttons);
        InputHistories histories = default;

        step.Run(ref histories, new InputWord(InputWord.Right), new InputWord(InputWord.Left));
        step.Run(ref histories, InputWord.Neutral, new InputWord(InputWord.Down));

        Assert.Equal(2, histories[0].Count);
        Assert.Equal(2, histories[1].Count);
        Assert.Equal(new InputWord(InputWord.Right), histories[0].FramesAgo(1));
        Assert.Equal(InputWord.Neutral, histories[0].FramesAgo(0));
        Assert.Equal(new InputWord(InputWord.Left), histories[1].FramesAgo(1));
        Assert.Equal(new InputWord(InputWord.Down), histories[1].FramesAgo(0));
    }

    // Runs the same raw word as P1 in one frame and as P2 in another, so both ports go through the same rule.
    private InputWord RecordForBothPlayers(InputWord raw)
    {
        var step = new InputStep(_buttons);
        InputHistories asPlayer1 = default;
        InputHistories asPlayer2 = default;

        step.Run(ref asPlayer1, raw, InputWord.Neutral);
        step.Run(ref asPlayer2, InputWord.Neutral, raw);

        Assert.Equal(asPlayer1[0].FramesAgo(0), asPlayer2[1].FramesAgo(0));
        return asPlayer1[0].FramesAgo(0);
    }
}
