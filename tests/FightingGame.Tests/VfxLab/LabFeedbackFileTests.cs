using FightingGame.VfxLab.Files;
using Xunit;

namespace FightingGame.Tests.VfxLab;

public sealed class LabFeedbackFileTests
{
    [Fact]
    public void WritesAndReadsBackPicksNotesChangesAndForks()
    {
        LabFeedback feedback = new();
        VariantFeedback rift = feedback.For("rift");
        rift.Pick = true;
        rift.Rating = 4;
        rift.Note = "Love the negative, lose the flakes";
        rift.Values["flakes"] = LabValue.Number(0.25f);
        rift.Values["accent_color"] = LabValue.Colour(1f, 1f, 1f, 1f);
        rift.Values["card_offset"] = LabValue.Vector(0.5f, -0.25f);
        rift.Values["mirror"] = LabValue.Toggle(true);
        feedback.For("ink");
        feedback.Forks.Add(new LabFork("rift_fork1", "rift", "Rift · fork 1", rift.Values));
        feedback.Timing["speed"] = 8f;

        string json = LabFeedbackFile.Write(feedback);
        LabFeedback? read = LabFeedbackFile.Read(json, out string error);

        Assert.Equal(string.Empty, error);
        Assert.NotNull(read);
        Assert.False(read.Variants.ContainsKey("ink"));
        VariantFeedback back = read.Variants["rift"];
        Assert.True(back.Pick);
        Assert.Equal(4, back.Rating);
        Assert.Equal("Love the negative, lose the flakes", back.Note);
        Assert.Equal(0.25f, back.Values["flakes"].First);
        Assert.Equal(LabValueFormat.Colour, back.Values["accent_color"].Format);
        Assert.Equal([0.5f, -0.25f], back.Values["card_offset"].Numbers);
        Assert.Equal(LabValueFormat.Toggle, back.Values["mirror"].Format);
        Assert.Equal("rift", read.Forks[0].From);
        Assert.Equal(8f, read.Timing["speed"]);
        Assert.Contains("\"accent_color\": \"#ffffff\"", json);
    }

    [Fact]
    public void RefusesAnotherVersion()
    {
        Assert.Null(LabFeedbackFile.Read("""{ "feedbackVersion": 9 }""", out string error));
        Assert.Contains("feedbackVersion", error);
    }
}
