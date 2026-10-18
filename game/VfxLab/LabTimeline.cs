using FightingGame.VfxLab.Files;

namespace FightingGame.VfxLab;

/// <summary>Turns the Lab's frame counter into a moment of the cast, looping the stretch chosen.</summary>
public static class LabTimeline
{
    /// <summary>How many frames one loop lasts.</summary>
    public static int Length(LabTiming timing, LabPhaseLoop loop) => loop switch
    {
        LabPhaseLoop.Charge => System.Math.Max(1, timing.Charge),
        LabPhaseLoop.Flight => timing.Flight,
        LabPhaseLoop.Impact => timing.Impact,
        _ => timing.Charge + timing.Flight + timing.Impact + timing.Rest,
    };

    /// <param name="frame">Any frame count; negative counts wrap too.</param>
    public static LabMoment At(int frame, LabTiming timing, LabPhaseLoop loop)
    {
        int length = Length(timing, loop);
        int f = ((frame % length) + length) % length;
        switch (loop)
        {
            case LabPhaseLoop.Charge:
                return new LabMoment(LabPhase.Charge, f + 1, f);
            case LabPhaseLoop.Flight:
                return new LabMoment(LabPhase.Flight, f + 1, f);
            case LabPhaseLoop.Impact:
                return new LabMoment(LabPhase.Impact, f + 1, f);
        }

        int start = 0;
        foreach ((LabPhase phase, int frames) in new[]
        {
            (LabPhase.Charge, timing.Charge),
            (LabPhase.Flight, timing.Flight),
            (LabPhase.Impact, timing.Impact),
        })
        {
            if (f < start + frames)
            {
                return new LabMoment(phase, f - start + 1, f);
            }

            start += frames;
        }

        return new LabMoment(LabPhase.Rest, f - start + 1, f);
    }
}
