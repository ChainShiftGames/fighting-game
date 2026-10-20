namespace FightingGame.Addons.ControlPanel.Catalog;

/// <summary>
/// A jump as the logic flies it (<c>FighterMovement</c>: move by the speed, then take gravity off): how long it stays
/// up and how high it gets, so the Characters page can show what a gravity or jump speed change means.
/// </summary>
/// <param name="AirFrames">Frames from leaving the ground to landing; 0 when it never lands.</param>
/// <param name="ApexFrame">The airborne frame at the highest point.</param>
/// <param name="ApexHeight">The highest point, in subpixels.</param>
public readonly record struct JumpArc(int AirFrames, int ApexFrame, int ApexHeight)
{
    // Longer than any jump worth showing; with no gravity a jump never comes down.
    private const int MaxFrames = 6000;

    /// <param name="velocity">Upward speed on the first airborne frame, subpixels per frame.</param>
    /// <param name="gravity">Taken off the speed every airborne frame.</param>
    public static JumpArc Of(int velocity, int gravity)
    {
        long height = 0;
        long speed = velocity;
        long apex = 0;
        int apexFrame = 0;
        for (int frame = 1; frame <= MaxFrames; frame++)
        {
            height += speed;
            if (height > apex)
            {
                apex = height;
                apexFrame = frame;
            }

            if (height <= 0 && speed <= 0)
            {
                return new JumpArc(frame, apexFrame, (int)apex);
            }

            speed -= gravity;
        }

        return new JumpArc(0, apexFrame, (int)apex);
    }
}
