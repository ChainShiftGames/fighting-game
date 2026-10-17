namespace Netcode.Drivers.Training;

/// <summary>How the dummy plays its recording back: a player setting chosen in the training menu.</summary>
/// <remarks>Saved by name, so the values may be reordered.</remarks>
public enum PlaybackStyle
{
    /// <summary>The chosen slot once, then the dummy stands; again after a reset or a loaded savestate.</summary>
    Once,

    /// <summary>The chosen slot over and over.</summary>
    Loop,

    /// <summary>A random recorded slot each time, over and over.</summary>
    Random,
}
