using Godot;
using Netcode.Drivers.Training;

namespace FightingGame.Training;

/// <summary>
/// The menu line that starts and stops recording the dummy into the chosen slot. Starting switches the dummy to
/// record mode, so the player controls it from the next frame.
/// </summary>
public sealed class RecordEntry : ITrainingMenuEntry
{
    private readonly CurrentTrainingSettings _settings;
    private readonly RecordingSlots _slots;

    public RecordEntry(Key key, CurrentTrainingSettings settings, RecordingSlots slots)
    {
        Key = key;
        _settings = settings;
        _slots = slots;
    }

    public Key Key { get; }

    public string Describe() => _slots.RecordingSlot is { } slot
        ? $"Stop recording slot {slot + 1} ({RecordingText.Length(_slots.Length(slot))})"
        : $"Record slot {_settings.Value.Slot}";

    public void Press()
    {
        if (_slots.RecordingSlot is not null)
        {
            _slots.Stop();
            return;
        }

        if (_settings.Value.Dummy != DummyMode.Record)
        {
            _settings.Change(_settings.Value with { Dummy = DummyMode.Record });
        }

        _slots.Start(_settings.Value.SlotIndex());
    }
}
