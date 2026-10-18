namespace FightingGame.Presentation.World;

/// <summary>What the retiming needs to know about one animation clip of a fighter model (ADR-0028).</summary>
/// <param name="LengthFrames">The part of the clip played, in frames at 60 fps.</param>
/// <param name="Loops">Whether the clip loops (idle, walk): frames past its end wrap instead of holding.</param>
/// <param name="Contacts">Where the active and recovery poses start, from the part's start; null plays it frame for frame.</param>
/// <param name="StartFrame">Where the played part starts in the clip (<c>clipFrom</c>).</param>
/// <param name="Fit">Stretches or squeezes the part over the whole move (a 4-frame prejump).</param>
/// <param name="HoldFrames">Shows each pose for this many logic frames (2 or 3: animation on 2s or 3s).</param>
/// <param name="Speed">Clip frames per logic frame when played frame for frame.</param>
public readonly record struct AnimationClipTiming(
    float LengthFrames,
    bool Loops,
    ContactFrames? Contacts = null,
    float StartFrame = 0f,
    bool Fit = false,
    int HoldFrames = 1,
    float Speed = 1f);
