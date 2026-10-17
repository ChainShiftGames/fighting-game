using System;
using System.Collections.Generic;
using Sim.Characters;
using Sim.Data.Input;
using Sim.Data.Sources;
using Sim.Data.Validation;

namespace Sim.Data.Characters.Reading;

/// <summary>Reads and validates one <c>moves/&lt;id&gt;.json</c> file of a character.</summary>
public sealed class MoveFileReader
{
    private readonly IDataSource _source;
    private readonly IReadOnlyList<string> _moveIds;
    private readonly IButtonLookup _buttons;
    private readonly DataErrors _errors;

    /// <param name="moveIds">The character's move list; cancels may only target these.</param>
    public MoveFileReader(IDataSource source, IReadOnlyList<string> moveIds, IButtonLookup buttons, DataErrors errors)
    {
        _source = source;
        _moveIds = moveIds;
        _buttons = buttons;
        _errors = errors;
    }

    /// <summary>The move, or null when the file is unusable (errors are reported).</summary>
    public MoveRecord? Read(string characterId, string moveId)
    {
        FieldReader? file = JsonDataFile.Open(_source, CharacterSchema.MoveFile(characterId, moveId), _errors);
        if (file is null || !SchemaVersion.Check(file, CharacterSchema.MoveVersion))
        {
            return null;
        }

        file.Ignore("$schema");
        string? id = file.RequiredId("id");
        if (id is not null && !string.Equals(id, moveId, StringComparison.Ordinal))
        {
            file.Error("id", $"must match its file name '{moveId}'");
        }

        MoveCategory category = file.RequiredChoice("category", CharacterJsonNames.Categories);
        MoveFrames frames = ReadFrames(file, category);
        MoveData move = ReadInput(file, category) with
        {
            Category = category,
            TotalFrames = frames.Total,
            FirstActiveFrame = frames.FirstActive,
            LastActiveFrame = frames.LastActive,
        };

        IReadOnlyList<BoxData> boxes = MoveBoxReader.Read(file, category, frames);
        IReadOnlyList<CancelRecord> cancels = new MoveCancelReader(_moveIds).Read(file, frames);
        IReadOnlyList<HitData> hits = MoveHitReader.Read(file, boxes);
        IReadOnlyList<VelocityData> velocities = MoveVelocityReader.Read(file, frames);
        move = MoveInvulnerabilityReader.Read(file, frames, move);
        move = MoveThrowReader.Read(file, boxes, move);
        IReadOnlyList<ProjectileRecord> projectiles = MoveProjectileReader.Read(file, category, frames);
        file.Finish();

        return new MoveRecord(move, boxes, cancels, hits, velocities, projectiles);
    }

    // Basic moves loop over a plain duration; attacks use fighting-game frame data, where startup counts the
    // first active frame (startup 5 = active from frame 5).
    private MoveFrames ReadFrames(FieldReader file, MoveCategory category)
    {
        int errorsBefore = _errors.Count;
        MoveFrames frames;
        if (category == MoveCategory.Basic)
        {
            file.Forbid("frameData", "basic moves use \"duration\" instead of frame data");
            frames = new MoveFrames(file.RequiredInt("duration", 1, CharacterSchema.MaxMoveFrames), 0, 0);
        }
        else
        {
            file.Forbid("duration", "only basic moves have a duration; use \"frameData\"");
            frames = ReadFrameData(file.RequiredObject("frameData"));
        }

        return _errors.Count == errorsBefore ? frames : MoveFrames.Unknown;
    }

    private static MoveFrames ReadFrameData(FieldReader? frameData)
    {
        if (frameData is null)
        {
            return MoveFrames.Unknown;
        }

        int startup = frameData.RequiredInt("startup", 1, CharacterSchema.MaxMoveFrames);
        int active = frameData.RequiredInt("active", 1, CharacterSchema.MaxMoveFrames);
        int recovery = frameData.RequiredInt("recovery", 0, CharacterSchema.MaxMoveFrames);
        frameData.Finish();

        int lastActive = startup + active - 1;
        return new MoveFrames(lastActive + recovery, startup, lastActive);
    }

    // Basic moves are entered by the state machine; every other move needs an input and a stance.
    private MoveData ReadInput(FieldReader file, MoveCategory category)
    {
        if (category == MoveCategory.Basic)
        {
            file.Forbid("input", "basic moves are started by the state machine, not by an input");
            file.Forbid("usableIn", "basic moves are started by the state machine, not by an input");
            return default;
        }

        MoveStance stance = file.RequiredChoice("usableIn", CharacterJsonNames.Stances);
        FieldReader? input = file.RequiredObject("input");
        if (input is null)
        {
            return new MoveData { Stance = stance };
        }

        int motion = ReadMotion(input);
        int buttonMask = input.Has("buttons") ? ReadButtonCombination(input) : ReadButton(input);
        int shortcut = ReadShortcut(input);
        input.Finish();
        return new MoveData { Stance = stance, Motion = motion, ButtonMask = buttonMask, ShortcutDirection = shortcut };
    }

    // The special button shortcut (ADR-0020): one numpad direction, held as the special button is pressed.
    private static int ReadShortcut(FieldReader input)
    {
        if (!input.Has("shortcut"))
        {
            return 0;
        }

        string shortcut = input.RequiredText("shortcut");
        if (shortcut.Length != 1 || shortcut[0] is < '1' or > '9')
        {
            input.Error("shortcut", $"must be one numpad direction from 1 to 9 (e.g. \"6\"), got \"{shortcut}\"");
            return 0;
        }

        return shortcut[0] - '0';
    }

    private static int ReadMotion(FieldReader input)
    {
        string motion = input.RequiredText("motion");
        int value = 0;
        foreach (char digit in motion)
        {
            if (digit is < '1' or > '9' || motion.Length > CharacterSchema.MaxMotionLength)
            {
                input.Error("motion", $"must be numpad notation, 1 to {CharacterSchema.MaxMotionLength} digits "
                    + $"from 1 to 9 (e.g. \"5\", \"2\", \"236\"), got \"{motion}\"");
                return 0;
            }

            value = (value * 10) + (digit - '0');
        }

        return value;
    }

    // Buttons pressed together (a throw's two buttons): the press is the frame the last of them goes down.
    private int ReadButtonCombination(FieldReader input)
    {
        input.Forbid("button", "use either \"button\" (one button) or \"buttons\" (pressed together), not both");
        IReadOnlyList<string?> buttons = input.IdArray("buttons", required: true);
        if (buttons.Count < 2)
        {
            input.Error("buttons", "list at least two buttons pressed together; a single button goes in \"button\"");
        }

        int combination = 0;
        foreach (string? button in buttons)
        {
            if (button is null)
            {
                continue;
            }

            if (!_buttons.TryGetMask(button, out int mask))
            {
                input.Error("buttons", UnknownButton(button));
            }
            else if ((combination & mask) != 0)
            {
                input.Error("buttons", $"lists '{button}' twice");
            }

            combination |= mask;
        }

        return combination;
    }

    private int ReadButton(FieldReader input)
    {
        string? button = input.RequiredId("button");
        if (button is null)
        {
            return 0;
        }

        if (_buttons.TryGetMask(button, out int mask))
        {
            return mask;
        }

        input.Error("button", UnknownButton(button));
        return 0;
    }

    private string UnknownButton(string button) =>
        $"unknown button '{button}' (the button set has: {string.Join(", ", _buttons.ButtonIds)})";
}
