using Sim.Input;
using Xunit;

namespace Sim.Tests.Input;

public sealed class NumpadDirectionTests
{
    [Theory]
    [InlineData(0, 1, 5)]
    [InlineData(InputWord.Right, 1, 6)]
    [InlineData(InputWord.Right, -1, 4)]
    [InlineData(InputWord.Left, -1, 6)]
    [InlineData(InputWord.Down | InputWord.Left, 1, 1)]
    [InlineData(InputWord.Down, 1, 2)]
    [InlineData(InputWord.Down | InputWord.Left, -1, 3)]
    [InlineData(InputWord.Up | InputWord.Left, 1, 7)]
    [InlineData(InputWord.Up, -1, 8)]
    [InlineData(InputWord.Up | InputWord.Right, 1, 9)]
    public void DirectionIsRelativeToFacing(int input, int facing, int expected)
    {
        Assert.Equal(expected, NumpadDirection.Of(new InputWord((ushort)input), facing));
    }

    [Theory]
    [InlineData(1, true, false, false, true)]
    [InlineData(3, true, false, true, false)]
    [InlineData(5, false, false, false, false)]
    [InlineData(7, false, true, false, true)]
    [InlineData(9, false, true, true, false)]
    public void ClassifiesEachDirection(int direction, bool isDown, bool isUp, bool isForward, bool isBack)
    {
        Assert.Equal(
            (isDown, isUp, isForward, isBack),
            (NumpadDirection.IsDown(direction), NumpadDirection.IsUp(direction), NumpadDirection.IsForward(direction),
                NumpadDirection.IsBack(direction)));
    }
}
