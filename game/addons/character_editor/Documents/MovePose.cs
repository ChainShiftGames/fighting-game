using System.Text.Json.Nodes;
using FightingGame.Presentation;
using Sim.Fighters.States;
using Sim.State;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// The pose the game's fighter art shows for a frame of a move, from the move's JSON as edited: so the editor's
/// model preview matches the match, contact frames included (ADR-0028).
/// </summary>
public static class MovePose
{
    /// <returns>Null when the move has no id or no readable frame data.</returns>
    public static FighterPose? Of(JsonObject move, int frame)
    {
        if (JsonNodeValue.Text(move[MoveFields.Id]) is not string id || FramePhases.Of(move) is not { } phases)
        {
            return null;
        }

        if (phases.IsBasic)
        {
            return new FighterPose(FighterAction.Idle, id, frame, phases.TotalFrames, MovePhase.None);
        }

        MovePhase phase = frame < phases.FirstActiveFrame ? MovePhase.Startup
            : frame <= phases.LastActiveFrame ? MovePhase.Active
            : MovePhase.Recovery;
        return new FighterPose(
            FighterAction.Attack, id, frame, phases.TotalFrames, phase, phases.FirstActiveFrame, phases.LastActiveFrame);
    }
}
