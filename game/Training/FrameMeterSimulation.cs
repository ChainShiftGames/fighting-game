using Netcode.Drivers;
using Netcode.Drivers.Training;
using Sim.Characters;
using Sim.Diagnostics;
using Sim.Input;
using Sim.State;

namespace FightingGame.Training;

/// <summary>Runs the match logic for the training driver and feeds each frame's frame states to the <see cref="FrameMeter"/>.</summary>
/// <remarks>
/// A frame both fighters spent frozen (hitstop, or a freeze they aren't exempt from) is left out, read from the state
/// before the frame runs. Time scale slowdowns aren't detected: their skipped frames repeat the previous column.
/// Training never rolls back, so every tick is a real frame. It only reads the state (ARCHITECTURE.md §9).
/// </remarks>
public sealed class FrameMeterSimulation : IMatchSimulation<MatchState>
{
    private readonly IMatchSimulation<MatchState> _simulation;
    private readonly CharacterData[] _characters;
    private readonly FrameMeter _meter;

    /// <param name="characters">P1's (the training player's) and P2's character.</param>
    public FrameMeterSimulation(IMatchSimulation<MatchState> simulation, CharacterData[] characters, FrameMeter meter)
    {
        _simulation = simulation;
        _characters = characters;
        _meter = meter;
    }

    public MatchState CreateInitialState() => _simulation.CreateInitialState();

    public void Tick(ref MatchState state, InputWord player1, InputWord player2)
    {
        bool bothFrozen = IsFrozen(in state, TrainingSides.Player) && IsFrozen(in state, TrainingSides.Dummy);
        _simulation.Tick(ref state, player1, player2);
        if (!bothFrozen)
        {
            _meter.Record(Of(in state, TrainingSides.Player), Of(in state, TrainingSides.Dummy));
        }
    }

    private FighterFrameState Of(in MatchState state, int fighter) =>
        FrameStates.Of(in state.Fighters[fighter], _characters[fighter]);

    private static bool IsFrozen(in MatchState state, int fighter)
    {
        bool isExempt = (state.Freeze.ExemptFighters & (1 << fighter)) != 0;
        return state.Fighters[fighter].Timing.HitstopFramesLeft > 0 || (state.Freeze.FramesLeft > 0 && !isExempt);
    }
}
