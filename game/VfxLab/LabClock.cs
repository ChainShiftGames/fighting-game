namespace FightingGame.VfxLab;

/// <summary>The Lab's frame counter: whole logic frames at 60 Hz times the playback speed, as in a match.</summary>
public sealed class LabClock
{
    private const double FramesPerSecond = 60.0;

    private double _carry;

    public int Frame { get; set; }

    public bool Playing { get; set; } = true;

    /// <summary>1 is game speed; slower plays each frame for longer, never between frames.</summary>
    public double Speed { get; set; } = 1.0;

    public void Advance(double seconds)
    {
        if (!Playing)
        {
            return;
        }

        _carry += seconds * FramesPerSecond * Speed;
        int whole = (int)_carry;
        Frame += whole;
        _carry -= whole;
    }

    /// <summary>Pauses and moves <paramref name="frames"/> frames on (or back, when negative).</summary>
    public void Step(int frames)
    {
        Playing = false;
        _carry = 0.0;
        Frame += frames;
    }
}
