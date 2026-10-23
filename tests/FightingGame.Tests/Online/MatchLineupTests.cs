using FightingGame.Online;
using Netcode.Replays;
using Xunit;

namespace FightingGame.Tests.Online;

public sealed class MatchLineupTests
{
    [Fact]
    public void ThePicksCharactersAndColoursGoIntoTheReplayHeader()
    {
        MatchLineup lineup = MatchLineup.From(
            TestGameData.Data, new FighterPick("testman", 2), new FighterPick("testman", 3));

        ReplayHeader header = lineup.CreateReplayHeader(TestGameData.Data);

        Assert.Equal(new ReplayPlayer("testman", 2), header.Player1);
        Assert.Equal(new ReplayPlayer("testman", 3), header.Player2);
    }

    [Fact]
    public void WithoutPicksBothPlayTheDefaultLineupInTheFirstColour()
    {
        ReplayHeader header = MatchLineup.From(TestGameData.Data).CreateReplayHeader(TestGameData.Data);

        Assert.Equal(0, header.Player1.Colour);
        Assert.Equal(0, header.Player2.Colour);
    }
}
