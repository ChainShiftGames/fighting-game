using System;

namespace FightingGame.Presentation.World;

/// <summary>
/// Crossfades into a basic move's clip from the pose shown just before (ADR-0028), so walking, crouching or ending
/// an attack doesn't pop. Attacks never fade in: their first frame shows exactly what the frame data says.
/// </summary>
/// <remarks>
/// Weighted by match frames, never elapsed time: paused or stepped, the blend holds. After a rollback or a rewound
/// replay the match frame goes back and the fade is dropped, a cosmetic loss.
/// </remarks>
public sealed class PoseFade
{
    private const int None = -1;

    private int _lastClip = None;
    private double _lastSeconds;
    private int _startFrame;

    /// <summary>The clip faded from; -1 when there is no fade.</summary>
    public int FromClip { get; private set; } = None;

    /// <summary>Where in <see cref="FromClip"/> the faded-from pose is held.</summary>
    public double FromSeconds { get; private set; }

    /// <param name="clip">The clip shown this frame, by <see cref="BoundClip.Id"/>.</param>
    /// <param name="fadeIn">The clip may fade in when it starts: it belongs to a basic move.</param>
    public void Observe(int clip, double seconds, bool fadeIn, int matchFrame)
    {
        if (clip != _lastClip)
        {
            bool fades = fadeIn && _lastClip != None;
            FromClip = fades ? _lastClip : None;
            FromSeconds = _lastSeconds;
            _startFrame = matchFrame;
        }
        else if (matchFrame < _startFrame)
        {
            FromClip = None;
        }

        _lastClip = clip;
        _lastSeconds = seconds;
    }

    /// <summary>How much of the new clip shows: rising to 1 over <paramref name="frames"/> frames after it starts.</summary>
    public float Weight(int matchFrame, int frames) =>
        FromClip == None || frames <= 0 ? 1f : Math.Min(1f, (matchFrame - _startFrame + 1f) / (frames + 1f));
}
