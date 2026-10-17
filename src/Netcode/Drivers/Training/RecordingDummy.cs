using Sim.Input;
using Sim.State;

namespace Netcode.Drivers.Training;

/// <summary>Record mode: the player's input drives the dummy, while the player's own fighter stands still.</summary>
/// <remarks>
/// While <see cref="RecordingSlots"/> records, each frame's input goes into the slot relative to the dummy's facing,
/// so the recording plays back from either side.
/// </remarks>
public sealed class RecordingDummy : ITrainingInputs<MatchState>
{
    private readonly RecordingSlots _slots;

    public RecordingDummy(RecordingSlots slots)
    {
        _slots = slots;
    }

    public FrameInputs Next(in MatchState state, InputWord player)
    {
        _slots.Add(FacingInputs.Turn(player, state.Fighters[TrainingSides.Dummy].Facing));
        return new FrameInputs(InputWord.Neutral, player);
    }
}
