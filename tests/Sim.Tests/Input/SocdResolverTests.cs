using Sim.Input;
using Xunit;

namespace Sim.Tests.Input;

public sealed class SocdResolverTests
{
    [Theory]
    [InlineData(InputWord.Left | InputWord.Right, 0)]
    [InlineData(InputWord.Up | InputWord.Down, InputWord.Up)]
    [InlineData(InputWord.Up | InputWord.Down | InputWord.Left | InputWord.Right, InputWord.Up)]
    [InlineData(InputWord.Down | InputWord.Left, InputWord.Down | InputWord.Left)]
    [InlineData(InputWord.Up | InputWord.Right, InputWord.Up | InputWord.Right)]
    public void OppositeDirectionsResolveByTheFixedRule(int raw, int expected)
    {
        Assert.Equal(new InputWord((ushort)expected), SocdResolver.Resolve(new InputWord((ushort)raw)));
    }

    [Fact]
    public void BitsOutsideTheDirectionsAreKept()
    {
        const ushort Button = 1 << 4;

        InputWord resolved = SocdResolver.Resolve(new InputWord(InputWord.Left | InputWord.Right | Button));

        Assert.Equal(new InputWord(Button), resolved);
    }
}
