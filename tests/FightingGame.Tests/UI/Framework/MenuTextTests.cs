using System.IO;
using System.Text.RegularExpressions;
using FightingGame.UI.Framework;
using Xunit;

namespace FightingGame.Tests.UI.Framework;

public sealed partial class MenuTextTests
{
    [Fact]
    public void KeysAreTranslatedAndFilledWithTranslatedArguments()
    {
        MenuText text = MenuText.Key("LOBBY_PLAYERS", MenuText.Key("LOBBY_HOST_VS", "Rival"));

        Assert.Equal("Lobby: you (P1) vs Rival (P2)", MenuTextTable.Formatter.Format(text));
    }

    [Fact]
    public void PlainTextIsShownAsItIsEvenWithBraces()
    {
        Assert.Equal("Desync {frame 12}", MenuTextTable.Formatter.Format(MenuText.Plain("Desync {frame 12}")));
    }

    [Fact]
    public void LinesAreJoinedAndEmptyTextIsABlankLine()
    {
        string text = MenuTextTable.Formatter.FormatLines([MenuText.Key("MENU_RESUME"), MenuText.Empty, MenuText.Plain("x")]);

        Assert.Equal("Resume\n\nx", text);
    }

    [Fact]
    public void EveryKeyTheMenusUseIsInTheTranslationTable()
    {
        string folder = Path.Combine(TestGameData.RepositoryRoot, "game", "UI");
        int keys = 0;
        foreach (string file in Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
        {
            foreach (Match match in KeyLiteral().Matches(File.ReadAllText(file)))
            {
                keys++;
                Assert.True(MenuTextTable.English.Has(match.Groups[1].Value), $"{match.Value} in {file}");
            }
        }

        Assert.True(keys > 20);
    }

    [Fact]
    public void TransitionsFadeInAndZeroTurnsThemOff()
    {
        var fade = new MenuTransition(100);
        fade.Start(1000);

        Assert.Equal(0f, fade.Opacity(1000));
        Assert.Equal(0.5f, fade.Opacity(1050));
        Assert.Equal(1f, fade.Opacity(1100));
        Assert.Equal(1f, new MenuTransition(0).Opacity(0));
    }

    [GeneratedRegex("\"((?:MENU|LOBBY|ONLINE|REPLAY)_[A-Z_]+)\"")]
    private static partial Regex KeyLiteral();
}
