using System.IO;
using System.Text.RegularExpressions;
using FightingGame.Online;
using Xunit;

namespace FightingGame.Tests.Online;

// The release version is set once, in Directory.Build.props; Godot can't read it, so project.godot repeats it and the
// export presets leave their version fields empty to take it from there (ADR-0044).
public sealed class GameVersionTests
{
    private static readonly Regex _release = new(@"^\d+\.\d+\.\d+$");
    private static readonly Regex _godotVersion = new(@"^config/version=""(?<version>[^""]*)""", RegexOptions.Multiline);
    private static readonly Regex _presetVersion = new(
        @"^application/(file|product)_version=""(?<version>[^""]*)""", RegexOptions.Multiline);

    private static string Release => GameVersion.Current.Split('+')[0];

    [Fact]
    public void TheReleaseVersionIsMajorMinorPatch() => Assert.Matches(_release, Release);

    [Fact]
    public void TheGodotProjectHasTheSameVersionAsTheBuild()
    {
        Match match = _godotVersion.Match(ReadGameFile("project.godot"));

        Assert.True(match.Success, "game/project.godot has no config/version; set it to the Version in Directory.Build.props.");
        Assert.Equal(Release, match.Groups["version"].Value);
    }

    [Fact]
    public void ExportPresetsTakeTheVersionFromTheProject()
    {
        foreach (Match match in _presetVersion.Matches(ReadGameFile("export_presets.cfg")))
        {
            Assert.True(
                match.Groups["version"].Value.Length == 0,
                $"export_presets.cfg sets its own version ({match.Value}); leave it empty so it reads config/version.");
        }
    }

    private static string ReadGameFile(string name) =>
        File.ReadAllText(Path.Combine(TestGameData.RepositoryRoot, "game", name));
}
