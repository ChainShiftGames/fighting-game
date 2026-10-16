using FightingGame.Debug;
using Sim.Data.Rules;
using Xunit;

namespace FightingGame.Tests.Debug;

/// <summary>The panel's list reads cleanly from the real <c>data/</c> files, so every key and type is right.</summary>
public sealed class DataSettingListTests
{
    [Fact]
    public void EveryNumberReadsFromTheDataFilesWithinItsRange()
    {
        foreach (DataFileSetting setting in DataSettingList.Numbers())
        {
            int value = setting.Read(TestGameData.DataRoot);
            Assert.InRange(value, setting.Min, setting.Max);
        }
    }

    [Fact]
    public void EveryChoiceReadsFromTheDataFiles()
    {
        foreach (DataFileChoice setting in DataSettingList.Choices())
        {
            Assert.Contains(setting.Read(TestGameData.DataRoot), setting.Choices);
        }
    }

    [Fact]
    public void TheTradeRuleOffersTheNamesTheLoaderAccepts()
    {
        DataFileChoice tradeRule = Assert.Single(DataSettingList.Choices(), choice => choice.Key == "tradeRule");

        Assert.Equal(MatchRulesSchema.TradeRules.Names, tradeRule.Choices);
    }
}
