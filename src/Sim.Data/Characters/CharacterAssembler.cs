using System.Collections.Generic;
using Sim.Data.Characters.Compiled;
using Sim.Data.Characters.Reading;

namespace Sim.Data.Characters;

/// <summary>Packs a validated character and its moves into the flat arrays of <see cref="CharacterData"/>.</summary>
public static class CharacterAssembler
{
    /// <param name="moves">One record per entry of <see cref="CharacterDefinition.MoveIds"/>, in the same order.</param>
    public static CharacterData Assemble(CharacterDefinition definition, IReadOnlyList<MoveRecord> moves)
    {
        List<MoveData> packedMoves = [];
        List<BoxData> boxes = [];
        List<CancelData> cancels = [];
        List<int> cancelTargets = [];
        List<HitData> hits = [];

        foreach (MoveRecord record in moves)
        {
            int cancelStart = cancels.Count;
            foreach (CancelRecord cancel in record.Cancels)
            {
                cancels.Add(cancel.Cancel with { TargetStart = cancelTargets.Count, TargetCount = cancel.Targets.Count });
                cancelTargets.AddRange(cancel.Targets);
            }

            packedMoves.Add(record.Move with
            {
                BoxStart = boxes.Count,
                BoxCount = record.Boxes.Count,
                CancelStart = cancelStart,
                CancelCount = record.Cancels.Count,
                HitStart = hits.Count,
                HitCount = record.Hits.Count,
            });
            boxes.AddRange(record.Boxes);
            hits.AddRange(record.Hits);
        }

        string[] moveIds = new string[definition.MoveIds.Count];
        for (int i = 0; i < moveIds.Length; i++)
        {
            moveIds[i] = definition.MoveIds[i];
        }

        return new CharacterData(
            definition.Id,
            definition.DisplayName,
            definition.Stats,
            moveIds,
            packedMoves.ToArray(),
            boxes.ToArray(),
            cancels.ToArray(),
            cancelTargets.ToArray(),
            hits.ToArray(),
            definition.BasicStateMoves);
    }
}
