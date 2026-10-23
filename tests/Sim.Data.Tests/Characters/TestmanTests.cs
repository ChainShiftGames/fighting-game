using System;
using Sim.Characters;
using Sim.Data.Sources;
using Sim.Data.Tests.Support;
using Xunit;

namespace Sim.Data.Tests.Characters;

/// <summary>P1-17: Testman, the placeholder character in data/, is complete enough to play a full match.</summary>
public sealed class TestmanTests
{
    private static readonly GameData _data = Fixture.Load(new DirectoryDataSource(Fixture.RepositoryDataRoot));
    private static readonly CharacterData _testman = _data.Character("testman");

    [Fact]
    public void LoadsFromTheRepositoryData()
    {
        Assert.Contains(_data.Characters, character => character.Id == "testman");
    }

    [Fact]
    public void LoadsFromTheDataEmbeddedForExportedBuilds()
    {
        GameData embedded = Fixture.Load(new EmbeddedDataSource());

        Assert.Equal(_testman.MoveIds.ToArray(), embedded.Character("testman").MoveIds.ToArray());
    }

    // Without its own move a state falls back to idle's boxes and frames; every state should look and last right.
    [Fact]
    public void HasItsOwnMoveForEveryBasicState()
    {
        int idle = _testman.BasicStateMove(BasicState.Idle);
        foreach (BasicState state in Enum.GetValues<BasicState>())
        {
            int move = _testman.BasicStateMove(state);

            Assert.True(move != CharacterData.NoMove, $"{state} has no move");
            Assert.True(state == BasicState.Idle || move != idle, $"{state} uses the idle move");
            Assert.Equal(MoveCategory.Basic, _testman.Moves[move].Category);
        }
    }

    [Theory]
    [InlineData("5a", MoveCategory.Normal)]
    [InlineData("5b", MoveCategory.Normal)]
    [InlineData("5c", MoveCategory.Normal)]
    [InlineData("5d", MoveCategory.Normal)]
    [InlineData("2a", MoveCategory.Normal)]
    [InlineData("2b", MoveCategory.Normal)]
    [InlineData("2c", MoveCategory.Normal)]
    [InlineData("j_c", MoveCategory.Normal)]
    [InlineData("236c", MoveCategory.Special)]
    [InlineData("623c", MoveCategory.Special)]
    [InlineData("236236d", MoveCategory.Super)]
    public void HasItsAttacks(string moveId, MoveCategory category)
    {
        Assert.Equal(category, Move(moveId).Category);
    }

    [Theory]
    [InlineData("2b", Guard.Low)]
    [InlineData("2c", Guard.Low)]
    [InlineData("j_c", Guard.Overhead)]
    public void HasALowAndAnOverhead(string moveId, Guard guard)
    {
        Assert.Equal(guard, _testman.HitsOf(Move(moveId))[0].Guard);
    }

    [Fact]
    public void ItsFireballSpawnsAProjectile()
    {
        Assert.Equal(1, Move("236c").ProjectileCount);
    }

    // A reversal beats a meaty: invulnerable from its first frame up to and including its first active frame.
    [Theory]
    [InlineData("623c")]
    [InlineData("236236d")]
    public void ReversalsAreInvulnerableThroughStartup(string moveId)
    {
        MoveData move = Move(moveId);

        for (int frame = 1; frame <= move.FirstActiveFrame; frame++)
        {
            Assert.True(move.IsInvulnerableOn(frame), $"{moveId} is hittable on frame {frame}");
        }
    }

    private static MoveData Move(string moveId)
    {
        int index = _testman.MoveIds.IndexOf(moveId);
        Assert.True(index >= 0, $"Testman has no move {moveId}");
        return _testman.Moves[index];
    }
}
