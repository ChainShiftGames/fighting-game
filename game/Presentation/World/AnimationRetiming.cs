namespace FightingGame.Presentation.World;

/// <summary>
/// Turns the logic's move frame into the time in an animation clip (ADR-0028). Without contact frames the clip plays
/// frame for frame; with them, startup, active and recovery each stretch over their own span of the clip, so frame
/// data can change without re-exporting the animation and the hit always lands on the authored contact pose.
/// </summary>
/// <remarks>
/// Logic frames count from 1 and the clip's first key is time 0, so move frame 1 shows the first key. Each logic frame
/// shows the start of its share of a span; a span the move doesn't have (0 frames) is skipped.
/// </remarks>
public static class AnimationRetiming
{
    public const float FramesPerSecond = 60f;

    /// <summary>The clip time, in seconds, the model shows for <paramref name="pose"/>.</summary>
    public static double SecondsAt(FighterPose pose, AnimationClipTiming clip) => FrameAt(pose, clip) / FramesPerSecond;

    /// <summary>The clip position, in animation frames from its first key, for <paramref name="pose"/>.</summary>
    public static float FrameAt(FighterPose pose, AnimationClipTiming clip)
    {
        float frame = clip.Contacts is { } contacts && pose.FirstActiveFrame > 0
            ? Retimed(pose, contacts, clip.LengthFrames)
            : pose.Frame - 1;
        return Fit(frame, clip);
    }

    private static float Retimed(FighterPose pose, ContactFrames contacts, float lengthFrames)
    {
        if (pose.Frame < pose.FirstActiveFrame)
        {
            return Span(pose.Frame - 1, pose.FirstActiveFrame - 1, 0f, contacts.ActiveFrom);
        }

        if (pose.Frame <= pose.LastActiveFrame)
        {
            int activeFrames = pose.LastActiveFrame - pose.FirstActiveFrame + 1;
            return Span(pose.Frame - pose.FirstActiveFrame, activeFrames, contacts.ActiveFrom, contacts.RecoveryFrom);
        }

        int recoveryFrames = pose.TotalFrames - pose.LastActiveFrame;
        return Span(pose.Frame - pose.LastActiveFrame - 1, recoveryFrames, contacts.RecoveryFrom, lengthFrames);
    }

    // Where logic frame `offset` of a `count`-frame phase falls in the clip span `from`..`to`.
    private static float Span(int offset, int count, float from, float to)
    {
        if (count <= 0)
        {
            return from;
        }

        int clamped = offset < 0 ? 0 : offset >= count ? count - 1 : offset;
        return from + ((to - from) * clamped / count);
    }

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
