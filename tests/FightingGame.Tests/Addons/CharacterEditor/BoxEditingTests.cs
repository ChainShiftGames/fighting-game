using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using Xunit;

namespace FightingGame.Tests.Addons.CharacterEditor;

/// <summary>Boxes edited per frame: split, copy, join, drag and resize (P3-42).</summary>
public sealed class BoxEditingTests
{
    private const int Pixel = 1000;

    [Fact]
    public void AFrameShowsTheBoxesWhoseRangeCoversIt()
    {
        // 5a: hurt 1..15 and 5..9, push 1..15, hit 5..7.
        JsonObject move = Read5A();

        Assert.Equal(2, MoveBoxes.OnFrame(move, 1).Count);
        Assert.Equal(4, MoveBoxes.OnFrame(move, 5).Count);
        Assert.Equal(new BoxArea(-20000, 0, 40000, 110000), MoveBoxes.Body(move, 1));
    }

    [Fact]
    public void IsolatingAFrameSplitsTheRangeAroundIt()
    {
        JsonObject move = Read5A();
        JsonObject hurt = MoveBoxes.All(move)[0];

        JsonObject single = MoveBoxes.Isolate(move, hurt, 4);

        Assert.Same(hurt, single);
        Assert.Equal([(1, 3), (4, 4), (5, 15)], Ranges(move, "hurt", -25000));
        Assert.Same(single, MoveBoxes.All(move)[1]);
    }

    [Fact]
    public void JoiningPutsTheSplitBoxBackTogether()
    {
        JsonObject move = Read5A();
        MoveBoxes.Isolate(move, MoveBoxes.All(move)[0], 4);

        MoveBoxes.Merge(move);

        Assert.Equal([(1, 15)], Ranges(move, "hurt", -25000));
        Assert.Equal(4, MoveBoxes.All(move).Count);
    }

    [Fact]
    public void CopyingAFrameReplacesTheBoxesOnTheOtherFrame()
    {
        JsonObject move = Read5A();

        // Frame 5 has the hit box and the extended hurt box; frame 10 has neither.
        MoveBoxes.CopyFrame(move, 5, 10, []);

        Assert.Equal(4, MoveBoxes.OnFrame(move, 10).Count);
        Assert.Equal([(5, 7), (10, 10)], Ranges(move, "hit", 20000));
        Assert.Equal([(1, 15)], Ranges(move, "push", -20000));

        MoveBoxes.CopyFrame(move, 1, 10, []);
        Assert.Equal(2, MoveBoxes.OnFrame(move, 10).Count);
        Assert.Equal([(5, 7)], Ranges(move, "hit", 20000));
    }

    [Fact]
    public void ANewBoxCoversTheBodyOnItsFrame()
    {
        JsonObject move = Read5A();

        JsonObject box = MoveBoxes.Add(move, "throw", 6, null, []);

        Assert.Equal(MoveBoxes.Body(move, 6), BoxArea.Of(box));
        Assert.Equal((6, 6), MoveBoxes.Range(box));
    }

    [Fact]
    public void DraggingSnapsAndNeverTurnsABoxInsideOut()
    {
        BoxArea area = new(0, 0, 10 * Pixel, 10 * Pixel);

        Assert.Equal(new BoxArea(3 * Pixel, -2 * Pixel, 10 * Pixel, 10 * Pixel), area.Dragged(BoxHandle.Move, 2600, -2400, Pixel));
        Assert.Equal(new BoxArea(0, 0, 15 * Pixel, 12 * Pixel), area.Dragged(BoxHandle.TopRight, 5 * Pixel, 2 * Pixel, Pixel));
        Assert.Equal(new BoxArea(10 * Pixel - 1, 0, 1, 10 * Pixel), area.Dragged(BoxHandle.Left, 50 * Pixel, 0, Pixel));
        Assert.Equal(new BoxArea(-5 * Pixel, 0, 5 * Pixel, 3 * Pixel), BoxArea.Between(0, 3 * Pixel, -5 * Pixel, 0, Pixel));
    }

    private static JsonObject Read5A() => EditorFixtures.Testman5A();

    // The frame ranges of the boxes of a kind at one x, in file order.
    private static List<(int, int)> Ranges(JsonObject move, string kind, int x)
    {
        List<(int, int)> ranges = [];
        foreach (JsonObject box in MoveBoxes.All(move))
        {
            if (box["kind"]!.GetValue<string>() == kind && box["x"]!.GetValue<int>() == x)
            {
                ranges.Add(MoveBoxes.Range(box)!.Value);
            }
        }

        return ranges;
    }
}
