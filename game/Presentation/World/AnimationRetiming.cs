using System.Collections.Generic;
using Sim.Fighters.States;

namespace FightingGame.Presentation.World;

/// <summary>
/// Turns the logic's move frame into the time in an animation clip (ADR-0028). Without contact frames the clip plays
/// frame for frame; with them, startup, active and recovery each stretch over their own span of the clip, so frame
/// data can change without re-exporting the animation and the hit always lands on the authored contact pose. Timing
/// keys pin frames inside a phase, so a phase can play its clip unevenly (a quick wind-up that lingers before the hit).
/// </summary>
/// <remarks>
/// Logic frames count from 1 and the clip's first key is time 0, so move frame 1 shows the first key. Each logic frame
/// shows the start of its share of a span; a span the move doesn't have (0 frames) is skipped. Poses on 2s or 3s are
/// held within each phase, so every phase starts on its own first pose.
/// </remarks>
public static class AnimationRetiming
{
    public const float FramesPerSecond = 60f;

    /// <summary>The clip time, in seconds, the model shows for <paramref name="pose"/>.</summary>
    public static double SecondsAt(FighterPose pose, AnimationClipTiming clip) => FrameAt(pose, clip) / FramesPerSecond;

    /// <summary>The clip position, in animation frames from its first key, for <paramref name="pose"/>.</summary>
    public static float FrameAt(FighterPose pose, AnimationClipTiming clip)
    {
        FighterPose held = pose with { Frame = Held(pose, clip.HoldFrames) };
        float frame;
        if (clip.Fit && pose.TotalFrames > 0)
        {
            frame = Span(held.Frame - 1, pose.TotalFrames, 0f, clip.LengthFrames);
        }
        else if (clip.Contacts is { } contacts && pose.FirstActiveFrame > 0)
        {
            frame = Retimed(held, clip, contacts);
        }
        else
        {
            frame = (held.Frame - 1) * clip.Speed;
        }

        return clip.StartFrame + Fit(frame, clip);
    }

    // On 2s, frames 1-2 show frame 1's pose, 3-4 frame 3's: the pose steps instead of moving every frame. Each phase
    // of the frame data holds from its own first frame, so the first active frame shows the contact pose whatever the
    // startup's length, instead of the last startup pose held a frame into the hit.
    private static int Held(FighterPose pose, int holdFrames)
    {
        if (holdFrames <= 1 || pose.Frame < 1)
        {
            return pose.Frame;
        }

        int phaseStart = PhaseStart(pose);
        return phaseStart + ((pose.Frame - phaseStart) / holdFrames * holdFrames);
    }

    // The first frame of the phase the pose is in; 1 for a move without active frames.
    private static int PhaseStart(FighterPose pose)
    {
        if (pose.FirstActiveFrame <= 0 || pose.Frame < pose.FirstActiveFrame)
        {
            return 1;
        }

        return pose.Frame <= pose.LastActiveFrame ? pose.FirstActiveFrame : pose.LastActiveFrame + 1;
    }

    /// <summary>
    /// The clip position for frame <paramref name="frameInTransition"/> (from 1) of a transition squeezed into
    /// <paramref name="frames"/> logic frames; reversed, it starts from the clip's end.
    /// </summary>
    public static float TransitionFrameAt(int frameInTransition, int frames, float lengthFrames, bool reverse)
    {
        float frame = Span(frameInTransition - 1, frames, 0f, lengthFrames);
        return reverse ? lengthFrames - frame : frame;
    }

    private static float Retimed(FighterPose pose, AnimationClipTiming clip, ContactFrames contacts)
    {
        IReadOnlyList<TimingKey>? keys = clip.Keys;
        if (pose.Frame < pose.FirstActiveFrame)
        {
            return Span(pose.Frame - 1, pose.FirstActiveFrame - 1, 0f, contacts.ActiveFrom, keys, MovePhase.Startup);
        }

        if (pose.Frame <= pose.LastActiveFrame)
        {
            int activeFrames = pose.LastActiveFrame - pose.FirstActiveFrame + 1;
            return Span(pose.Frame - pose.FirstActiveFrame, activeFrames, contacts.ActiveFrom, contacts.RecoveryFrom,
                keys, MovePhase.Active);
        }

        int recoveryFrames = pose.TotalFrames - pose.LastActiveFrame;
        return Span(pose.Frame - pose.LastActiveFrame - 1, recoveryFrames, contacts.RecoveryFrom, clip.LengthFrames,
            keys, MovePhase.Recovery);
    }

    // Where logic frame `offset` of a `count`-frame phase falls in the clip span `from`..`to`: straight through, or
    // straight from pin to pin when the phase has timing keys.
    private static float Span(int offset, int count, float from, float to, IReadOnlyList<TimingKey>? keys = null,
        MovePhase phase = MovePhase.None)
    {
        if (count <= 0)
        {
            return from;
        }

        int clamped = offset < 0 ? 0 : offset >= count ? count - 1 : offset;
        float position = clamped / (float)count;
        float fromPosition = 0f;
        if (keys is not null)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                TimingKey key = keys[i];
                if (key.Phase != phase)
                {
                    continue;
                }

                if (key.Position > position)
                {
                    return Between(position, fromPosition, from, key.Position, key.Clip);
                }

                (fromPosition, from) = (key.Position, key.Clip);
            }
        }

        return Between(position, fromPosition, from, 1f, to);
    }

    private static float Between(float position, float fromPosition, float from, float toPosition, float to) =>
        from + ((to - from) * (position - fromPosition) / (toPosition - fromPosition));

    private static float Fit(float frame, AnimationClipTiming clip)
    {
        if (clip.LengthFrames <= 0f || frame < 0f)
        {
            return 0f;
        }

        if (clip.Loops)
        {
            return frame % clip.LengthFrames;
        }

        return frame > clip.LengthFrames ? clip.LengthFrames : frame;
    }
}
