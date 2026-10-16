using FightingGame.Presentation.Events;
using Sim.Events;
using Xunit;

namespace FightingGame.Tests.Presentation.Events;

public sealed class SoundVoicesTests
{
    private readonly SoundVoices _voices = new(2);

    [Fact]
    public void ReleasesTheVoiceThatPlaysTheEvent()
    {
        _voices.Assign(Hit(10));
        int voice = _voices.Assign(Hit(11));

        Assert.Equal(voice, _voices.Release(Hit(11)));
        Assert.Equal(SoundVoices.NoVoice, _voices.Release(Hit(11)));
    }

    [Fact]
    public void ANewSoundTakesTheOldestVoice()
    {
        int oldest = _voices.Assign(Hit(10));
        _voices.Assign(Hit(11));

        Assert.Equal(oldest, _voices.Assign(Hit(12)));
        // The cut-off sound's voice now plays another event, so cancelling the old one must not stop it.
        Assert.Equal(SoundVoices.NoVoice, _voices.Release(Hit(10)));
    }

    private static FrameEventId Hit(int frame) => new(frame, FrameEventKind.Hit, Fighter: 0, Occurrence: 0);
}
