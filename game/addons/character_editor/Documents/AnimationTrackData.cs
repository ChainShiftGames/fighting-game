using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Presentation;
using FightingGame.Presentation.World;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// What the animation track draws for a move (ADR-0028): the clip frame each move frame shows, retimed as in a match
/// (holds on 2s included), and the frames pinned by contact frames and keys.
/// </summary>
/// <param name="ClipFrames">Index 0 is move frame 1; in clip frames at 60 fps.</param>
public sealed record AnimationTrackData(
    FramePhases Phases, ClipPart Part, IReadOnlyList<float> ClipFrames, IReadOnlyList<TrackPin> Pins)
{
    /// <param name="clip">The move's clip as the model binds it (its part, holds and speed); its timing is replaced.</param>
    /// <returns>Null for a basic move or unreadable frame data.</returns>
    public static AnimationTrackData? Of(JsonObject move, AnimationClipTiming clip, MoveAnimationTiming timing)
    {
        if (FramePhases.Of(move) is not { IsBasic: false } phases)
        {
            return null;
        }

        AnimationClipTiming retimed = ClipPartFrames.Fit(clip, timing.Contacts, timing.Keys, out _);
        var frames = new float[phases.TotalFrames];
        for (int frame = 1; frame <= frames.Length; frame++)
        {
            frames[frame - 1] = MovePose.Of(move, frame) is FighterPose pose ? AnimationRetiming.FrameAt(pose, retimed) : 0f;
        }

        var part = new ClipPart((int)System.MathF.Ceiling(clip.StartFrame), (int)(clip.StartFrame + clip.LengthFrames));
        return new AnimationTrackData(phases, part, frames, timing.PinsOn(phases));
    }

    /// <summary>The clip frame <paramref name="frame"/> shows, rounded to a whole frame, for pinning it where it is.</summary>
    public int ClipAt(int frame) => frame >= 1 && frame <= ClipFrames.Count ? (int)System.MathF.Round(ClipFrames[frame - 1]) : Part.Start;

    /// <summary>The pin on <paramref name="frame"/>, if any.</summary>
    public TrackPin? PinOn(int frame)
    {
        foreach (TrackPin pin in Pins)
        {
            if (pin.Frame == frame)
            {
                return pin;
            }
        }

        return null;
    }
}
