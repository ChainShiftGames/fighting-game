using System.Collections.Generic;
using System.IO;
using System.Linq;
using FightingGame.VfxLab;
using FightingGame.VfxLab.Files;
using Xunit;

namespace FightingGame.Tests.VfxLab;

public sealed class LabSheetFileTests
{
    private const string Folder = "res://VfxLab/Sheets/test";

    [Fact]
    public void ReadsTheDarkFireballSheet()
    {
        string path = Path.Combine(TestGameData.RepositoryRoot, "game", "VfxLab", "Sheets", "dark_fireball", "sheet.json");

        LabSheet? sheet = LabSheetFile.Read(File.ReadAllText(path), "res://VfxLab/Sheets/dark_fireball", out string error);

        Assert.Equal(string.Empty, error);
        Assert.NotNull(sheet);
        LabSession session = new(sheet, new LabFeedback());
        Assert.Equal(string.Empty, session.Problem);
        Assert.All(session.Variants.Values, variant => Assert.Equal(3, variant.Parts.Count));
        Assert.Equal("res://VfxLab/Sheets/dark_fireball/flight_rift.gdshader", session.Variants["rift"].Parts[LabPartKind.Flight].Shader);
    }

    [Fact]
    public void AVariantTakesWhatItExtendsFieldByField()
    {
        const string Json = """
            {
              "labVersion": 1,
              "rounds": [{ "id": "r1", "variants": [
                { "id": "base", "name": "Base", "pitch": "The base.",
                  "flight": { "shader": "base.gdshader", "card": [1.8, 1.7], "spin": 0.08 },
                  "impact": { "shader": "res://Assets/Effects/hit.gdshader" },
                  "values": { "aura": 1.0, "accent_color": "#ff0000" } },
                { "id": "child", "extends": "base",
                  "flight": { "shader": "child.gdshader" },
                  "values": { "aura": 1.5, "arms": 3 } }
              ] }]
            }
            """;

        LabSheet? sheet = LabSheetFile.Read(Json, Folder, out string error);
        Dictionary<string, LabVariant>? variants = VariantInheritance.Resolve(sheet!.Rounds[0].Variants, out error);

        Assert.Equal(string.Empty, error);
        LabVariant child = variants!["child"];
        Assert.Equal("Base", child.Name);
        Assert.Equal("The base.", child.Pitch);
        Assert.Equal($"{Folder}/child.gdshader", child.Parts[LabPartKind.Flight].Shader);
        Assert.Equal(1.8f, child.Parts[LabPartKind.Flight].CardWidth);
        Assert.Equal(0.08f, child.Parts[LabPartKind.Flight].Spin);
        Assert.Equal("res://Assets/Effects/hit.gdshader", child.Parts[LabPartKind.Impact].Shader);
        Assert.Equal(1.5f, child.Values["aura"].First);
        Assert.Equal(3f, child.Values["arms"].First);
        Assert.Equal(LabValueFormat.Colour, child.Values["accent_color"].Format);
        Assert.Equal([1f, 0f, 0f, 1f], child.Values["accent_color"].Numbers);
    }

    [Theory]
    [InlineData("""{ "labVersion": 2, "rounds": [] }""", "labVersion is 2")]
    [InlineData("""{ "labVersion": 1, "rounds": [] }""", "no rounds")]
    [InlineData("""{ "labVersion": 1, "rounds": [{ "id": "r", "variants": [] }] }""", "round \"r\" has no variants")]
    [InlineData("""{ "labVersion": 1, "rounds": [{ "id": "r", "variants": [{ "id": "a", "extends": "gone" }] }] }""", "\"gone\", which isn't in the sheet")]
    [InlineData("""{ "labVersion": 1, "rounds": [{ "id": "r", "variants": [{ "id": "a", "extends": "b", "flight": { "shader": "x.gdshader" } }, { "id": "b", "extends": "a" }] }] }""", "extends itself")]
    [InlineData("""{ "labVersion": 1, "rounds": [{ "id": "r", "variants": [{ "id": "a" }] }] }""", "no part with a shader")]
    [InlineData("""{ "labVersion": 1, "rounds": [{ "id": "r", "variants": [{ "id": "a", "flight": { "shader": "x.gdshader" }, "values": { "c": "red" } }] }] }""", "#rrggbb")]
    [InlineData("""{ "labVersion": 1, "ranges": { "aura": [2, 1] }, "rounds": [{ "id": "r", "variants": [{ "id": "a", "flight": { "shader": "x.gdshader" } }] }] }""", "range \"aura\"")]
    [InlineData("""{ "labVersion": 1, "colour": 1, "rounds": [] }""", "colour")]
    public void RefusesAnInvalidSheetWithTheReason(string json, string reason)
    {
        LabSheet? sheet = LabSheetFile.Read(json, Folder, out string error);

        Assert.Null(sheet);
        Assert.Contains(reason, error);
    }

    [Fact]
    public void ColoursRoundTripAsHex()
    {
        Assert.True(LabColourHex.TryParse("#f3c2ff", out LabValue colour));
        Assert.Equal("#f3c2ff", LabColourHex.Format(colour));
        Assert.True(LabColourHex.TryParse("#00000080", out LabValue seeThrough));
        Assert.Equal("#00000080", LabColourHex.Format(seeThrough));
        Assert.False(LabColourHex.TryParse("#zzzzzz", out _));
        Assert.Equal(["#000000", "#ffffff"], new[] { "#000000", "#FFFFFF" }.Select(text => LabColourHex.Format(Parse(text))));
    }

    private static LabValue Parse(string text)
    {
        Assert.True(LabColourHex.TryParse(text, out LabValue value));
        return value;
    }
}
