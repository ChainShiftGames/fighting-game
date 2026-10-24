using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Presentation.World;
using Sim.Fighters.States;
using Xunit;

namespace FightingGame.Tests.Addons.CharacterEditor;

public sealed class MoveAnimationTimingTests
{
    // 236c: startup 33 (frames 1-32, first active 33), active 33, recovery 34-65; its clip plays frames 0-40.
    private static readonly FramePhases _fireball = FramePhases.Attack(33, 1, 32);
    private static readonly ClipPart _clip = new(0, 40);
    private static readonly MoveAnimationTiming _contacts = new(new ContactFrames(29, 32), []);

    [Fact]
    public void PinsAStartupFrameWithAKeyAgainstThePhasesLength()
    {
        MoveAnimationTiming pinned = _contacts.Pin(_fireball, 10, 20, _clip)!;

        Assert.Equal([new TimingKey(MovePhase.Startup, 10, 32, 20)], pinned.Keys);
        Assert.Equal(new ContactFrames(29, 32), pinned.Contacts);
    }

    [Fact]
    public void MovesAContactFrameWhenItsFrameIsPinned()
    {
        MoveAnimationTiming active = _contacts.Pin(_fireball, 33, 27, _clip)!;
        MoveAnimationTiming recovery = _contacts.Pin(_fireball, 34, 35, _clip)!;

        Assert.Equal(new ContactFrames(27, 32), active.Contacts);
        Assert.Equal(new ContactFrames(29, 35), recovery.Contacts);
    }

    [Fact]
    public void KeepsContactFramesInOrderAndInsideTheClip()
    {
        Assert.Equal(new ContactFrames(32, 32), _contacts.Pin(_fireball, 33, 38, _clip)!.Contacts);
        Assert.Equal(new ContactFrames(29, 40), _contacts.Pin(_fireball, 34, 99, _clip)!.Contacts);
        Assert.Equal(0, _contacts.Pin(_fireball, 5, -4, _clip)!.Keys[0].Clip);
    }

    [Fact]
    public void GivesAMoveWithoutContactFramesTheOnesItShowsNow()
    {
        // Frame for frame, the first active frame 33 shows clip frame 32 and the first recovery frame 34 shows 33.
        MoveAnimationTiming pinned = MoveAnimationTiming.None.Pin(_fireball, 10, 20, _clip)!;

        Assert.Equal(new ContactFrames(32, 33), pinned.Contacts);
    }

    [Fact]
    public void CantPinTheFirstFrameOrABasicMove()
    {
        Assert.Null(_contacts.Pin(_fireball, 1, 5, _clip));
        Assert.Null(_contacts.Pin(FramePhases.Basic(60), 10, 5, _clip));
    }

    [Fact]
    public void ListsPinsInFrameOrderWhereTheKeysFallNow()
    {
        // Pinned at frame 10 of a 32-frame startup; with a 16-frame startup it falls on frame 5.
        MoveAnimationTiming pinned = _contacts.Pin(_fireball, 10, 20, _clip)!.Pin(_fireball, 40, 36, _clip)!;
        FramePhases quicker = FramePhases.Attack(17, 1, 32);

        Assert.Equal(
            [
                new TrackPin(5, 20, TrackPinKind.Key),
                new TrackPin(17, 29, TrackPinKind.ActiveFrom),
                new TrackPin(18, 32, TrackPinKind.RecoveryFrom),
                new TrackPin(24, 36, TrackPinKind.Key),
            ],
            pinned.PinsOn(quicker));
    }

    [Fact]
    public void ReplacesTheKeyOnAFrameAndUnpinsIt()
    {
        MoveAnimationTiming pinned = _contacts.Pin(_fireball, 10, 20, _clip)!.Pin(_fireball, 10, 24, _clip)!;

        Assert.Equal([new TimingKey(MovePhase.Startup, 10, 32, 24)], pinned.Keys);
        Assert.Empty(pinned.Unpin(_fireball, 10).Keys);
        Assert.Contains(new TrackPin(33, 29, TrackPinKind.ActiveFrom), pinned.Unpin(_fireball, 33).PinsOn(_fireball));
    }
}
