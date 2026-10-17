using System;
using Sim.Characters;
using Sim.Data.Tests.Support;
using Sim.Data.Validation;
using Xunit;

namespace Sim.Data.Tests.Characters;

/// <summary>Throw moves in data (P1-23): button combinations, the <c>throw</c> object and the throw rules.</summary>
public sealed class ThrowDataTests
{
    private const string HitBox = "{ \"kind\": \"hit\", \"firstFrame\": 5";
    private const string ThrowBox = "{ \"kind\": \"throw\", \"firstFrame\": 5";
    private const string Release = "\"throw\": { \"releaseX\": -60000 }, ";

    private readonly InMemoryDataSource _data = Fixture.InMemory();
    private readonly string _5a = Fixture.MoveFile("5a");

    [Fact]
    public void ButtonsPressedTogetherBecomeOneMask()
    {
        _data.Replace(_5a, "\"button\": \"a\"", "\"buttons\": [\"a\", \"b\"]");

        Assert.Equal((1 << 4) | (1 << 5), Move("5a").ButtonMask);
    }

    [Theory]
    [InlineData("\"button\": \"a\", \"buttons\": [\"a\", \"b\"]", "input.button")]
    [InlineData("\"buttons\": [\"a\"]", "input.buttons")]
    [InlineData("\"buttons\": [\"a\", \"a\"]", "input.buttons")]
    [InlineData("\"buttons\": [\"a\", \"z\"]", "input.buttons")]
    public void AWrongButtonCombinationIsReported(string buttons, string field)
    {
        _data.Replace(_5a, "\"button\": \"a\"", buttons);

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal((_5a, field), (error.File, error.Field));
    }

    [Fact]
    public void AThrowMoveReadsWhereItReleasesTheOpponent()
    {
        _data.Replace(_5a, HitBox, ThrowBox);
        _data.Replace(_5a, "\"hits\": [", Release + "\"hits\": [");

        Assert.Equal(-60000, Move("5a").ThrowReleaseX);
    }

    [Fact]
    public void AMoveWithThrowBoxesNeedsThrowData()
    {
        _data.Replace(_5a, HitBox, ThrowBox);

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal((_5a, "throw"), (error.File, error.Field));
    }

    [Fact]
    public void ThrowDataWithoutThrowBoxesIsReported()
    {
        _data.Replace(_5a, "\"hits\": [", Release + "\"hits\": [");

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal((_5a, "throw"), (error.File, error.Field));
    }

    [Fact]
    public void AMoveCannotHitAndThrow()
    {
        string extraThrowBox = $"{ThrowBox}, \"lastFrame\": 7, \"x\": 0, \"y\": 0, \"width\": 1, \"height\": 1 }}";
        _data.Replace(_5a, HitBox, $"{extraThrowBox}, {HitBox}");
        _data.Replace(_5a, "\"hits\": [", Release + "\"hits\": [");

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal((_5a, "boxes"), (error.File, error.Field));
    }

    [Fact]
    public void AThrowMoveNeedsAThrowBox()
    {
        _data.Replace(_5a, "\"category\": \"normal\"", "\"category\": \"throw\"");

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal((_5a, "boxes"), (error.File, error.Field));
    }

    [Fact]
    public void AirThrowsAreReported()
    {
        string jumpB = Fixture.MoveFile("j_b");
        _data.Replace(jumpB, "{ \"kind\": \"hit\", \"firstFrame\": 8", "{ \"kind\": \"throw\", \"firstFrame\": 8");
        _data.Replace(jumpB, "\"hits\": [", Release + "\"hits\": [");

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal((jumpB, "usableIn"), (error.File, error.Field));
    }

    private MoveData Move(string moveId)
    {
        CharacterData character = Fixture.Load(_data).Character(Fixture.CharacterId);
        return character.Moves[Array.IndexOf(character.MoveIds.ToArray(), moveId)];
    }
}
