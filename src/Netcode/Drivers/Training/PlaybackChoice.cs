namespace Netcode.Drivers.Training;

/// <summary>What the dummy plays back: the chosen slot (0-based) and how (player settings).</summary>
public readonly record struct PlaybackChoice(int Slot, PlaybackStyle Style);
