using System;
using System.Collections.Generic;
using Sim.Characters;
using Sim.Data.Sources;
using Sim.Data.Validation;

namespace Sim.Data.Characters.Reading;

/// <summary>Reads and validates <c>characters/&lt;id&gt;/character.json</c>: stats, move list and basic states.</summary>
public static class CharacterFileReader
{
    /// <summary>The definition, or null when the file is unusable (errors are in <paramref name="errors"/>).</summary>
    public static CharacterDefinition? Read(IDataSource source, string characterId, DataErrors errors)
    {
        FieldReader? file = JsonDataFile.Open(source, CharacterSchema.CharacterFile(characterId), errors);
        if (file is null || !SchemaVersion.Check(file, CharacterSchema.CharacterVersion))
        {
            return null;
        }

        file.Ignore("$schema");
        string? id = file.RequiredId("id");
        if (id is not null && !string.Equals(id, characterId, StringComparison.Ordinal))
        {
            file.Error("id", $"must match its folder name '{characterId}'");
        }

        string displayName = file.RequiredText("displayName");
        CharacterStats stats = ReadStats(file.RequiredObject("stats"));
        List<string> moveIds = ReadMoveList(file);
        int[] basicStateMoves = ReadBasicStates(file.RequiredObject("states"), moveIds);
        file.Finish();

        return new CharacterDefinition(characterId, displayName, stats, moveIds, basicStateMoves);
    }

    private static CharacterStats ReadStats(FieldReader? stats)
    {
        if (stats is null)
        {
            return default;
        }

        const int MaxFrames = CharacterSchema.MaxMoveFrames;
        const int MaxDistance = CharacterSchema.MaxDistance;
        CharacterStats result = new()
        {
            Health = stats.RequiredInt("health", 1, CharacterSchema.MaxHealth),
            WalkForwardSpeed = stats.RequiredInt("walkForwardSpeed", 0, MaxDistance),
            WalkBackSpeed = stats.RequiredInt("walkBackSpeed", 0, MaxDistance),
            DashStyle = stats.RequiredChoice("dashStyle", CharacterJsonNames.DashStyles),
            PrejumpFrames = stats.RequiredInt("prejumpFrames", 0, MaxFrames),
            JumpVelocity = stats.RequiredInt("jumpVelocity", 0, MaxDistance),
            HighJumpVelocity = stats.RequiredInt("highJumpVelocity", 0, MaxDistance),
            JumpForwardSpeed = stats.RequiredInt("jumpForwardSpeed", 0, MaxDistance),
            JumpBackSpeed = stats.RequiredInt("jumpBackSpeed", 0, MaxDistance),
            Gravity = stats.RequiredInt("gravity", 0, MaxDistance),
            AirActions = stats.RequiredInt("airActions", 0, CharacterSchema.MaxAirActions),
        };
        stats.Finish();
        return result;
    }

    // Invalid and duplicate entries stay in the list as "" so later indexes and error paths still line up.
    private static List<string> ReadMoveList(FieldReader file)
    {
        IReadOnlyList<string?> entries = file.IdArray("moves", required: true);
        List<string> moveIds = [];
        for (int i = 0; i < entries.Count; i++)
        {
            string? moveId = entries[i];
            if (moveId is not null && moveIds.Contains(moveId))
            {
                file.Error($"moves[{i}]", $"move '{moveId}' is listed twice");
                moveId = null;
            }

            moveIds.Add(moveId ?? "");
        }

        if (entries.Count == 0)
        {
            file.Error("moves", "must list at least one move");
        }

        return moveIds;
    }

    private static int[] ReadBasicStates(FieldReader? states, List<string> moveIds)
    {
        int[] moves = new int[CharacterJsonNames.BasicStates.Length];
        Array.Fill(moves, CharacterData.NoMove);
        if (states is null)
        {
            return moves;
        }

        foreach ((string name, BasicState state) in CharacterJsonNames.BasicStates)
        {
            // Idle is the state every other one returns to; P1-08 decides which further states are required.
            string? moveId = state == BasicState.Idle ? states.RequiredId(name) : states.OptionalId(name);
            if (moveId is null)
            {
                continue;
            }

            int index = moveIds.IndexOf(moveId);
            if (index < 0)
            {
                states.Error(name, $"move '{moveId}' is not in the character's move list");
            }

            moves[(int)state] = index;
        }

        states.Finish();
        return moves;
    }
}
