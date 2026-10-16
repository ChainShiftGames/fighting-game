using Sim.Input;
using Sim.Match;
using Sim.Pipeline;
using Sim.State;
using Xunit;

namespace Sim.Tests.Match;

public sealed class RoundInputLockTests
{
    private static readonly InputWord _player1 = new(InputWord.Right);
    private static readonly InputWord _player2 = new(InputWord.Up);

    [Fact]
    public void InputPassesThroughWhileFighting()
    {
        RecordingInputStage inner = new();
        MatchState state = default;

        new RoundInputLock(inner).Run(ref state, _player1, _player2);

        Assert.Equal((_player1, _player2), (inner.Player1, inner.Player2));
    }

    [Theory]
    [InlineData(RoundPhase.RoundOver)]
    [InlineData(RoundPhase.MatchOver)]
    public void InputIsNeutralWhileNoRoundIsFought(RoundPhase phase)
    {
        RecordingInputStage inner = new();
        MatchState state = default;
        state.Round.Phase = phase;

        new RoundInputLock(inner).Run(ref state, _player1, _player2);

        Assert.Equal((InputWord.Neutral, InputWord.Neutral, 1), (inner.Player1, inner.Player2, inner.Runs));
    }

    private sealed class RecordingInputStage : IInputStage
    {
        public InputWord Player1 { get; private set; }

        public InputWord Player2 { get; private set; }

        public int Runs { get; private set; }

        public void Run(ref MatchState state, InputWord player1, InputWord player2)
        {
            Player1 = player1;
            Player2 = player2;
            Runs++;
        }
    }
}
