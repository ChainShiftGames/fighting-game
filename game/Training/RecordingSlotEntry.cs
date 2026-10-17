using Godot;
using Netcode.Drivers.Training;

namespace FightingGame.Training;

/// <summary>The menu line that picks the recording slot to record into and play back; each press moves to the next.</summary>
public sealed class RecordingSlotEntry : ITrainingMenuEntry
{
    private readonly CurrentTrainingSettings _settings;
    private readonly RecordingSlots _slots;

    public RecordingSlotEntry(Key key, CurrentTrainingSettings settings, RecordingSlots slots)
    {
        Key = key;
        _settings = settings;
        _slots = slots;
    }

    public Key Key { get; }

    public string Describe()
    {
        int slot = _settings.Value.SlotIndex();
        return $"Slot: {slot + 1} ({RecordingText.Length(_slots.Length(slot))})";
    }

    public void Press()
    {
        int next = ((_settings.Value.SlotIndex() + 1) % RecordingSlots.Count) + 1;
        _settings.Change(_settings.Value with { Slot = next });
    }
}
