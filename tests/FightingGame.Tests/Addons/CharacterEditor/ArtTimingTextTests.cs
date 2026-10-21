using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Presentation.World;
using Sim.Fighters.States;
using Xunit;

namespace FightingGame.Tests.Addons.CharacterEditor;

/// <summary>The animation track saves into the art file without touching anything but the move's entry (ADR-0028).</summary>
public sealed class ArtTimingTextTests
{
    private const string File = """
        {
          // Test setup: comments stay.
          "artVersion": 1,
          "art": "model",
          "model": "m.glb",
          "animations": {
            "idle": { "clip": "Idle", "loop": true },
            // Hands pushed out at 32 (the clip's end), held through recovery.
            "236c": { "file": "res://a.glb", "clip": "Spell_Simple_Enter", "activeFrom": 29, "recoveryFrom": 32 }
          },
          "holdFrames": 2
        }
        """;

    [Fact]
    public void RewritesOnlyTheMovesContactFrames()
    {
        string written = ArtTimingText.Write(File, "236c", new MoveAnimationTiming(new ContactFrames(27, 31), []));

        Assert.Equal(
            File.Replace("\"activeFrom\": 29, \"recoveryFrom\": 32", "\"activeFrom\": 27, \"recoveryFrom\": 31"),
            written);
    }

    [Fact]
    public void WritesKeysOneALineUnderTheEntrysOtherFields()
    {
        var timing = new MoveAnimationTiming(new ContactFrames(29, 32),
            [new TimingKey(MovePhase.Startup, 10, 32, 20), new TimingKey(MovePhase.Recovery, 4, 32, 36)]);

        string written = ArtTimingText.Write(File, "236c", timing);

        const string Entry = """
            "236c": {
                  "file": "res://a.glb", "clip": "Spell_Simple_Enter", "activeFrom": 29, "recoveryFrom": 32,
                  "keys": [
                    { "phase": "startup", "frame": 10, "of": 32, "clip": 20 },
                    { "phase": "recovery", "frame": 4, "of": 32, "clip": 36 }
                  ]
                }
            """;
        Assert.Contains(Entry.Replace("\r\n", "\n"), written.Replace("\r\n", "\n"));
        Assert.Contains("// Hands pushed out at 32", written);
        Assert.Equal(timing.Keys, FighterArtFile.Read(written, out _)!.KeysFor("236c"));
        Assert.Equal(File, ArtTimingText.Write(written, "236c", new MoveAnimationTiming(new ContactFrames(29, 32), [])));
    }

    [Fact]
    public void AddsAnEntryForAMoveTheFileDoesntListYet()
    {
        string written = ArtTimingText.Write(File, "5a", new MoveAnimationTiming(new ContactFrames(6, 10), []));

        Assert.Contains("\"recoveryFrom\": 32 },\n    \"5a\": { \"activeFrom\": 6, \"recoveryFrom\": 10 }\n  },",
            written.Replace("\r\n", "\n"));
        Assert.Equal(new ContactFrames(6, 10), FighterArtFile.Read(written, out _)!.ContactsFor("5a"));
    }

    [Fact]
    public void AddsTheFirstEntryToAnEmptyAnimationsObject()
    {
        const string Empty = """{ "artVersion": 1, "art": "model", "model": "m.glb", "animations": {} }""";

        string written = ArtTimingText.Write(Empty, "5a", new MoveAnimationTiming(new ContactFrames(6, 10), []));

        Assert.Equal(new ContactFrames(6, 10), FighterArtFile.Read(written, out _)!.ContactsFor("5a"));
    }

    [Fact]
    public void KeepsAnArtFileUnchangedWhenSavedWithItsOwnTiming()
    {
        ArtTimingFile file = ArtTimingFile.FromText("unused", File);

        Assert.Equal(string.Empty, file.SetTiming("236c", file.Timing("236c")));
        Assert.False(file.IsChanged);
        Assert.Equal(File, file.Text);
    }

    [Fact]
    public void RefusesTimingTheGameWouldntRead()
    {
        ArtTimingFile file = ArtTimingFile.FromText("unused", File);
        var backwards = new MoveAnimationTiming(new ContactFrames(29, 32),
            [new TimingKey(MovePhase.Recovery, 4, 32, 36), new TimingKey(MovePhase.Startup, 10, 32, 20)]);

        Assert.Contains("keys[1]", file.SetTiming("236c", backwards));
        Assert.Equal(File, file.Text);
    }
}
