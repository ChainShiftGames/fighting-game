using Sim.Data.Hashing;
using Sim.Input.Motions;

namespace Sim.Data.Motions;

/// <summary>Adds the motion file to the data hash: order, steps, windows and timing all decide what counts as input.</summary>
public static class MotionSetDataHash
{
    public static void Append(DataHashBuilder hash, MotionFile file)
    {
        hash.Add(file.MotionWindow);
        hash.Add(file.ButtonBuffer);
        hash.Add(file.Movement.DashMotion);
        hash.Add(file.Movement.BackdashMotion);
        hash.Add(file.Movement.HighJumpMotion);
        hash.Add(file.Movement.DashButtonMask);
        MotionSet motions = file.Motions;
        hash.Add(motions.Count);
        for (int motion = 0; motion < motions.Count; motion++)
        {
            hash.Add(motions.NotationOf(motion));
            hash.Add(motions.WindowOf(motion));
            hash.Add(motions.StepsOf(motion).Length);
            foreach (MotionStep step in motions.StepsOf(motion))
            {
                hash.Add(step.Directions);
            }
        }
    }
}
