using System;

namespace FightingGame.Presentation.World;

/// <summary>
/// Crossfades into a clip from the pose shown just before (ADR-0028), so walking, crouching, ending an attack or being
/// hit again mid-reaction doesn't pop. Attacks and first hits never fade in (<see cref="MayFadeInto"/>).
/// </summary>
/// <remarks>
/// Weighted by match frames, never elapsed time: paused or stepped, the blend holds; in hitstop, which holds the
/// fighter but not the match clock, a re-hit eases into its held pose. After a rollback or a rewound replay the match
/// frame goes back and the fade is dropped, a cosmetic loss. A fade that starts mid-fade starts from the pose faded
/// into, not the blend shown, a small pop on hits a few frames apart.
/// </remarks>
public sealed class PoseFade
{
    private const int None = -1;

    private int _lastClip = None;
    private double _lastSeconds;
    private int _startFrame;
    private int _frames;

    /// <summary>The clip faded from; -1 when there is no fade.</summary>
    public int FromClip { get; private set; } = None;

    /// <summary>Where in <see cref="FromClip"/> the faded-from pose is held.</summary>
    public double FromSeconds { get; private set; }

    /// <param name="clip">The clip shown this frame, by <see cref="BoundClip.Id"/>.</param>
    /// <param name="fadeFrames">Frames the clip fades in over if it starts this frame; 0 snaps.</param>
    /// <param name="startsOver">The clip starts again though it was shown last frame too (a re-hit).</param>
    public void Observe(int clip, double seconds, int fadeFrames, bool startsOver, int matchFrame)
    {
        if (clip != _lastClip || startsOver)
        {
            bool fades = fadeFrames > 0 && _lastClip != None;
            FromClip = fades ? _lastClip : None;
            FromSeconds = _lastSeconds;
            _startFrame = matchFrame;
            _frames = fadeFrames;
        }
        else if (matchFrame < _startFrame)
        {
            FromClip = None;
        }

        _lastClip = clip;
        _lastSeconds = seconds;
    }

    /// <summary>How much of the new clip shows: rising to 1 over the fade's frames after it starts.</summary>
    public float Weight(int matchFrame) =>
        FromClip == None || _frames <= 0 ? 1f : Math.Min(1f, (matchFrame - _startFrame + 1f) / (_frames + 1f));

    /// <summary>
    /// Whether the clip shown for <paramref name="pose"/> may fade in as a basic move does. Attacks snap, so their
    /// first frame shows exactly what the frame data says; so do hit and block reactions, so the frame a hit lands
    /// shows it, the snap that makes hits read (as in Guilty Gear). A hit landing mid-reaction fades by its own
    /// setting instead (<see cref="HitReactionClock.IsReHit"/>).
    /// </summary>
    public static bool MayFadeInto(in FighterPose pose) =>
        pose.FirstActiveFrame == 0 && !HitReactionActions.Includes(pose.Action);
}
