using System.Collections.Generic;
using FightingGame.Presentation.World;
using Sim.Fighters.States;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// A move's animation timing as the animation track edits it (ADR-0028): its contact frames and timing keys, in clip
/// frames as the art file writes them. Pinning a phase's first active or recovery frame moves a contact frame; any
/// other frame inside a phase gets a key.
/// </summary>
public sealed record MoveAnimationTiming(ContactFrames? Contacts, IReadOnlyList<TimingKey> Keys)
{
    public static MoveAnimationTiming None { get; } = new(null, []);

    /// <summary>Every pinned frame of a move with <paramref name="phases"/>, in frame order; none for a basic move.</summary>
    public IReadOnlyList<TrackPin> PinsOn(FramePhases phases)
    {
        var pins = new List<TrackPin>();
        if (phases.IsBasic || Contacts is not { } contacts)
        {
            return pins;
        }

        AddKeys(pins, phases, MovePhase.Startup);
        pins.Add(new TrackPin(phases.FirstActiveFrame, contacts.ActiveFrom, TrackPinKind.ActiveFrom));
        AddKeys(pins, phases, MovePhase.Active);
        if (phases.Recovery > 0)
        {
            pins.Add(new TrackPin(phases.LastActiveFrame + 1, contacts.RecoveryFrom, TrackPinKind.RecoveryFrom));
            AddKeys(pins, phases, MovePhase.Recovery);
        }

        return pins;
    }

    /// <summary>
    /// The timing with <paramref name="frame"/> showing <paramref name="clip"/>, kept inside the part played and with
    /// the contact frames in order. A move without contact frames gets them first, where it shows those poses now.
    /// Keys are rewritten against the phases' lengths now, so they read the same in the file as on the track.
    /// </summary>
    /// <returns>Null when the frame can't be pinned: a basic move, or frame 1, which shows the part's start.</returns>
    public MoveAnimationTiming? Pin(FramePhases phases, int frame, int clip, ClipPart part)
    {
        if (phases.IsBasic || frame <= 1 || frame > phases.TotalFrames)
        {
            return null;
        }

        ContactFrames contacts = Contacts ?? FrameForFrame(phases, part);
        clip = part.Clamp(clip);
        if (frame == phases.FirstActiveFrame)
        {
            int active = clip > contacts.RecoveryFrom ? contacts.RecoveryFrom : clip;
            return new MoveAnimationTiming(contacts with { ActiveFrom = active }, Rescaled(phases, 0));
        }

        if (frame == phases.LastActiveFrame + 1)
        {
            int recovery = clip < contacts.ActiveFrom ? contacts.ActiveFrom : clip;
            return new MoveAnimationTiming(contacts with { RecoveryFrom = recovery }, Rescaled(phases, 0));
        }

        (MovePhase phase, int phaseFrame, int length) = PhaseOf(phases, frame);
        List<TimingKey> keys = Rescaled(phases, frame);
        keys.Add(new TimingKey(phase, phaseFrame, length, clip));
        keys.Sort(static (a, b) => a.Phase != b.Phase ? a.Phase.CompareTo(b.Phase) : a.Frame.CompareTo(b.Frame));
        return new MoveAnimationTiming(contacts, keys);
    }

    /// <summary>The timing without the key on <paramref name="frame"/>; contact frames stay, the hit lines up with them.</summary>
    public MoveAnimationTiming Unpin(FramePhases phases, int frame) =>
        phases.IsBasic ? this : this with { Keys = Rescaled(phases, frame) };

    // The poses a clip without contact frames shows now, frame for frame from the part's start.
    private static ContactFrames FrameForFrame(FramePhases phases, ClipPart part) =>
        new(part.Clamp(part.Start + phases.FirstActiveFrame - 1), part.Clamp(part.Start + phases.LastActiveFrame));

    // The keys against the phases' lengths now, leaving out the one on `skipFrame` and any that no longer fit.
    private List<TimingKey> Rescaled(FramePhases phases, int skipFrame)
    {
        var keys = new List<TimingKey>(Keys.Count + 1);
        foreach (TimingKey key in Keys)
        {
            int length = LengthOf(phases, key.Phase);
            int phaseFrame = key.FrameIn(length);
            if (length < 2 || phaseFrame < 2 || phaseFrame > length || MoveFrame(phases, key.Phase, phaseFrame) == skipFrame
                || keys.Exists(other => other.Phase == key.Phase && other.Frame == phaseFrame))
            {
                continue;
            }

            keys.Add(new TimingKey(key.Phase, phaseFrame, length, key.Clip));
        }

        return keys;
    }

    private void AddKeys(List<TrackPin> pins, FramePhases phases, MovePhase phase)
    {
        int length = LengthOf(phases, phase);
        foreach (TimingKey key in Keys)
        {
            int phaseFrame = key.FrameIn(length);
            if (key.Phase == phase && phaseFrame >= 2 && phaseFrame <= length)
            {
                pins.Add(new TrackPin(MoveFrame(phases, phase, phaseFrame), key.Clip, TrackPinKind.Key));
            }
        }
    }

    private static (MovePhase Phase, int PhaseFrame, int Length) PhaseOf(FramePhases phases, int frame)
    {
        MovePhase phase = frame < phases.FirstActiveFrame ? MovePhase.Startup
            : frame <= phases.LastActiveFrame ? MovePhase.Active
            : MovePhase.Recovery;
        return (phase, frame - MoveFrame(phases, phase, 1) + 1, LengthOf(phases, phase));
    }

    private static int LengthOf(FramePhases phases, MovePhase phase) => phase switch
    {
        MovePhase.Startup => phases.Startup - 1,
        MovePhase.Active => phases.Active,
        _ => phases.Recovery,
    };

    private static int MoveFrame(FramePhases phases, MovePhase phase, int phaseFrame) => phase switch
    {
        MovePhase.Startup => phaseFrame,
        MovePhase.Active => phases.FirstActiveFrame + phaseFrame - 1,
        _ => phases.LastActiveFrame + phaseFrame,
    };
}
