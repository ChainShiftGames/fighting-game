using Sim.State;

namespace Sim.Combat;

/// <summary>How a hit's hitstop is given to a fighter; shared by fighter attacks and projectiles.</summary>
public static class Hitstop
{
    /// <summary>Gives <paramref name="frames"/> of hitstop; a fighter already in a longer hitstop keeps it.</summary>
    public static void Extend(ref FighterTiming timing, int frames)
    {
        if (frames > timing.HitstopFramesLeft)
        {
            timing.HitstopFramesLeft = frames;
        }
    }
}
