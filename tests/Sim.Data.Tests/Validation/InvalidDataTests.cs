using System;
using System.Linq;
using Sim.Data.Tests.Support;
using Sim.Data.Validation;
using Xunit;

namespace Sim.Data.Tests.Validation;

/// <summary>P1-07 done-when: invalid files give errors naming file, field and reason.</summary>
public sealed class InvalidDataTests
{
    private const string Character = Fixture.CharacterFile;
    private static readonly string _move5A = Fixture.MoveFile("5a");
    private static readonly string _moveIdle = Fixture.MoveFile("idle");
    private static readonly string _moveBackdash = Fixture.MoveFile("backdash");
    private static readonly string _move236C = Fixture.MoveFile("236c");

    private readonly InMemoryDataSource _data = Fixture.InMemory();

    [Fact]
    public void FractionalNumber()
    {
        _data.Replace(_move5A, "\"damage\": 300", "\"damage\": 300.5");

        AssertSingleError(_move5A, "hit.damage", "must be a whole number from 0 to 1000000, got 300.5");
    }

    [Fact]
    public void NumberOutOfRange()
    {
        _data.Replace(_move5A, "\"startup\": 5", "\"startup\": 0");

        AssertSingleError(_move5A, "frameData.startup", "must be a whole number from 1 to 999, got 0");
    }

    [Fact]
    public void MissingRequiredField()
    {
        _data.Replace(_move5A, "\"hitstun\": 12,", "");

        AssertSingleError(_move5A, "hit.hitstun", "required field is missing");
    }

    [Fact]
    public void MisspelledField()
    {
        _data.Replace(_move5A, "\"hitstop\": 8", "\"hitstop\": 8, \"hitstpo\": 8");

        AssertSingleError(_move5A, "hit.hitstpo", "unknown field");
    }

    [Fact]
    public void DuplicateField()
    {
        _data.Replace(_move5A, "\"hitstop\": 8", "\"hitstop\": 8, \"hitstop\": 9");

        AssertSingleError(_move5A, "hit.hitstop", "field appears more than once");
    }

    [Fact]
    public void UnknownChoice()
    {
        _data.Replace(_move5A, "\"guard\": \"mid\"", "\"guard\": \"middle\"");

        AssertSingleError(_move5A, "hit.guard", "must be one of \"mid\", \"low\", \"overhead\", \"unblockable\"");
    }

    [Fact]
    public void WrongType()
    {
        _data.Replace(_move5A, "\"boxes\": [", "\"boxes\": 4, \"unused\": [");

        DataValidationException exception = LoadFails();
        Assert.Contains(exception.Errors, error => error.Field == "boxes" && error.Reason.Contains("must be an array"));
    }

    [Fact]
    public void InvalidJson()
    {
        _data.Replace(_move5A, "\"category\": \"normal\",", "\"category\": \"normal\"");

        DataError error = AssertSingleError(_move5A, "", "not valid JSON at line 6");
        Assert.Equal("", error.Field);
    }

    [Fact]
    public void UnsupportedSchemaVersion()
    {
        _data.Replace(_move5A, "\"schemaVersion\": 1", "\"schemaVersion\": 2");

        AssertSingleError(_move5A, "schemaVersion", "this build reads schema version 1, the file has 2");
    }

    [Fact]
    public void IdNotMatchingFileName()
    {
        _data.Replace(_move5A, "\"id\": \"5a\"", "\"id\": \"5b\"");

        AssertSingleError(_move5A, "id", "must match its file name '5a'");
    }

    [Fact]
    public void UppercaseId()
    {
        _data.Replace(Character, "\"idle\": \"idle\"", "\"idle\": \"Idle\"");

        AssertSingleError(Character, "states.idle", "must be a lowercase snake_case id");
    }

    [Fact]
    public void ListedMoveWithoutFile()
    {
        _data.Remove(_move5A);

        AssertSingleError(_move5A, "", "file does not exist");
    }

    [Fact]
    public void MoveFileMissingFromTheMoveList()
    {
        _data.SetText(Fixture.MoveFile("6b"), _data.GetText(_move5A).Replace("\"5a\"", "\"6b\"", StringComparison.Ordinal));

        AssertSingleError(Fixture.MoveFile("6b"), "", "move file is not in the \"moves\" list");
    }

    [Fact]
    public void MoveListedTwice()
    {
        _data.Replace(Character, "\"crouch\", \"backdash\"]", "\"crouch\", \"backdash\", \"5a\"]");

        AssertSingleError(Character, "moves[7]", "move '5a' is listed twice");
    }

    [Fact]
    public void CancelIntoUnknownMove()
    {
        _data.Replace(_move5A, "\"into\": [\"2b\"]", "\"into\": [\"2c\"]");

        AssertSingleError(_move5A, "cancels[0].into[0]", "move '2c' is not in the character's move list");
    }

    [Fact]
    public void CancelWithoutTarget()
    {
        _data.Replace(_move5A, ", \"into\": [\"5a\"]", "");

        AssertSingleError(_move5A, "cancels[1].into", "needs at least one target");
    }

    [Fact]
    public void HitboxOutsideActiveFrames()
    {
        _data.Replace(_move5A, "\"kind\": \"hit\", \"firstFrame\": 5, \"lastFrame\": 7",
            "\"kind\": \"hit\", \"firstFrame\": 5, \"lastFrame\": 8");

        AssertSingleError(_move5A, "boxes[3].firstFrame", "within the active frames 5 to 7, got 5 to 8");
    }

    [Fact]
    public void BoxLongerThanTheMove()
    {
        _data.Replace(_move5A, "\"kind\": \"hurt\", \"firstFrame\": 1, \"lastFrame\": 15",
            "\"kind\": \"hurt\", \"firstFrame\": 1, \"lastFrame\": 16");

        AssertSingleError(_move5A, "boxes[0].lastFrame", "must be a whole number from 1 to 15, got 16");
    }

    [Fact]
    public void HitboxWithoutHitProperties()
    {
        string text = _data.GetText(_move5A);
        _data.SetText(_move5A, text[..text.IndexOf(",\n  \"hit\"", StringComparison.Ordinal)] + "\n}\n");

        AssertSingleError(_move5A, "hit", "required because the move has hit or throw boxes");
    }

    [Fact]
    public void UnknownButton()
    {
        _data.Replace(_move5A, "\"button\": \"a\"", "\"button\": \"e\"");

        AssertSingleError(_move5A, "input.button", "unknown button 'e' (the button set has: a, b, c, d, special, dash)");
    }

    [Fact]
    public void MotionNotInNumpadNotation()
    {
        _data.Replace(Fixture.MoveFile("236c"), "\"motion\": \"236\"", "\"motion\": \"qcf\"");

        AssertSingleError(Fixture.MoveFile("236c"), "input.motion", "must be numpad notation");
    }

    [Fact]
    public void BasicMoveWithInput()
    {
        _data.Replace(_moveIdle, "\"duration\": 60,", "\"duration\": 60, \"input\": { \"motion\": \"5\", \"button\": \"a\" },");

        AssertSingleError(_moveIdle, "input", "basic moves are started by the state machine");
    }

    [Fact]
    public void BasicStateUsingAnAttack()
    {
        _data.Replace(Character, "\"crouch\": \"crouch\"", "\"crouch\": \"2b\"");

        AssertSingleError(Character, "states.crouch", "move '2b' must have category \"basic\"");
    }

    [Fact]
    public void UnknownBasicState()
    {
        _data.Replace(Character, "\"crouch\": \"crouch\"", "\"crouch\": \"crouch\", \"sit\": \"crouch\"");

        AssertSingleError(Character, "states.sit", "unknown field");
    }

    [Fact]
    public void EveryErrorInEveryFileIsReportedAtOnce()
    {
        _data.Replace(_move5A, "\"guard\": \"mid\"", "\"guard\": \"middle\"");
        _data.Replace(Fixture.MoveFile("2b"), "\"damage\": 450", "\"damage\": -1");
        _data.Replace(Character, "\"health\": 10000", "\"health\": 0");

        DataValidationException exception = LoadFails();

        Assert.Equal(3, exception.Errors.Count);
        Assert.Contains($"{_move5A}: hit.guard: must be one of", exception.Message, StringComparison.Ordinal);
        Assert.Contains($"{Fixture.MoveFile("2b")}: hit.damage: must be", exception.Message, StringComparison.Ordinal);
        Assert.Contains($"{Character}: stats.health: must be", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OverlappingVelocityRanges()
    {
        _data.Replace(_moveBackdash, "\"firstFrame\": 7, \"lastFrame\": 14", "\"firstFrame\": 6, \"lastFrame\": 14");

        AssertSingleError(_moveBackdash, "velocity[1]", "must be in frame order and must not overlap");
    }

    [Fact]
    public void VelocityOutsideTheMove()
    {
        _data.Replace(_moveBackdash, "\"lastFrame\": 14", "\"lastFrame\": 21");

        AssertSingleError(_moveBackdash, "velocity[1].lastFrame", "must be a whole number from 7 to 20, got 21");
    }

    [Fact]
    public void InvulnerabilityOutsideTheMove()
    {
        _data.Replace(_moveBackdash, "\"invulnerable\": { \"firstFrame\": 1", "\"invulnerable\": { \"firstFrame\": 21");

        AssertSingleError(_moveBackdash, "invulnerable.firstFrame", "must be a whole number from 1 to 20, got 21");
    }

    [Fact]
    public void ProjectileSpawnFrameOutsideTheMove()
    {
        _data.Replace(_move236C, "\"spawnFrame\": 14", "\"spawnFrame\": 42");

        AssertSingleError(_move236C, "projectiles[0].spawnFrame", "must be a whole number from 1 to 41, got 42");
    }

    [Fact]
    public void ProjectileBoxOutsideItsLifetime()
    {
        _data.Replace(_move236C, "\"lastFrame\": 90", "\"lastFrame\": 91");

        AssertSingleError(
            _move236C, "projectiles[0].boxes[0].lastFrame", "must be a whole number from 1 to 90, got 91");
    }

    [Fact]
    public void ProjectileWithoutBoxes()
    {
        _data.Replace(_move236C, "\"boxes\": [\n        {", "\"boxes\": [], \"unusedBoxes\": [\n        {");

        DataValidationException exception = LoadFails();
        Assert.Contains(
            exception.Errors,
            error => error.Field == "projectiles[0].boxes" && error.Reason.Contains("at least one box"));
    }

    [Fact]
    public void ProjectileWithoutHit()
    {
        _data.Replace(_move236C, "\"hit\": { \"damage\": 700", "\"unusedHit\": { \"damage\": 700");

        DataValidationException exception = LoadFails();
        Assert.Contains(exception.Errors, error => error.Field == "projectiles[0].hit");
    }

    [Fact]
    public void ProjectileWithoutDurability()
    {
        _data.Replace(_move236C, "\"durability\": 2", "\"durability\": 0");

        AssertSingleError(_move236C, "projectiles[0].durability", "must be a whole number from 1 to 99, got 0");
    }

    [Fact]
    public void BasicMoveWithProjectiles()
    {
        string projectiles = Between(_data.GetText(_move236C), "\"projectiles\"", "\n}");
        _data.Replace(_moveIdle, "\"boxes\": [", projectiles + ",\n  \"boxes\": [");

        AssertSingleError(_moveIdle, "projectiles", "basic moves can't spawn projectiles");
    }

    private static string Between(string text, string start, string end)
    {
        int from = text.IndexOf(start, StringComparison.Ordinal);
        return text[from..text.LastIndexOf(end, StringComparison.Ordinal)];
    }

    private DataError AssertSingleError(string file, string field, string reasonPart)
    {
        DataValidationException exception = LoadFails();
        DataError error = Assert.Single(exception.Errors);
        Assert.Equal(file, error.File);
        Assert.Equal(field, error.Field);
        Assert.Contains(reasonPart, error.Reason, StringComparison.Ordinal);
        return error;
    }

    private DataValidationException LoadFails() =>
        Assert.Throws<DataValidationException>(() => Fixture.Load(_data));

    [Fact]
    public void FixtureIsValid()
    {
        Assert.Single(Fixture.Load(_data).Characters);
        Assert.Contains(Fixture.MoveFile("5a"), _data.Paths.ToArray());
    }
}
