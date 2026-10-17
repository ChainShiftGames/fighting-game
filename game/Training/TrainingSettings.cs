using Netcode.Drivers.Training;

namespace FightingGame.Training;

/// <summary>Training mode's player settings (ARCHITECTURE.md §6): chosen in the training menu, kept between sessions.</summary>
/// <remarks>Later training options are added here as further properties.</remarks>
public sealed record TrainingSettings
{
    public DummyMode Dummy { get; init; } = DummyMode.Stand;

    /// <summary>Whether the frame meter is shown (P3-31).</summary>
    public bool ShowFrameMeter { get; init; } = true;

    /// <summary>The recording slot to record into and play back, 1 to <see cref="RecordingSlots.Count"/>.</summary>
    public int Slot { get; init; } = 1;

    public PlaybackStyle Playback { get; init; } = PlaybackStyle.Loop;

    /// <summary>The slot (0-based) and style the dummy plays back; a slot out of range from a hand-edited file is clamped.</summary>
    public PlaybackChoice ToPlaybackChoice() => new(SlotIndex(), Playback);

    /// <summary>The 0-based <see cref="Slot"/>.</summary>
    public int SlotIndex() => int.Clamp(Slot, 1, RecordingSlots.Count) - 1;
}
