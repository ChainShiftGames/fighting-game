using System;
using System.Collections.Generic;
using FightingGame.Addons.ControlPanel.Catalog;
using FightingGame.Addons.ControlPanel.Documents;
using Xunit;

namespace FightingGame.Tests.Addons.ControlPanel;

public sealed class HitFeelTests : IDisposable
{
    private readonly PanelTestFolders _folders = new();
    private readonly PanelDocument _document;
    private readonly IReadOnlyList<HitRow> _rows;

    public HitFeelTests()
    {
        _document = new PanelDocument(_folders.Folders);
        _rows = HitFeelRows.For(_document, "testman");
    }

    public void Dispose() => _folders.Dispose();

    [Fact]
    public void EveryHitOfEveryMoveIsARowAndMovesWithoutHitsAreNot()
    {
        HitRow heavy = Row("5c");

        Assert.Equal(("normal", 1), (heavy.Category, heavy.HitCount));
        Assert.Equal(12, _document.Number(HitColumn.Hitstop.For(heavy)));
        Assert.DoesNotContain(_rows, row => row.MoveId == "idle");
    }

    [Fact]
    public void ScalingAColumnChangesEveryRowGivenAndNoOther()
    {
        HitRow heavy = Row("5c");
        HitRow light = Row("5a");
        double lightHitstop = _document.Number(HitColumn.Hitstop.For(light));

        HitColumnEdit.Scale(_document, [heavy], HitColumn.Hitstop, 150);

        Assert.Equal(18, _document.Number(HitColumn.Hitstop.For(heavy)));
        Assert.Equal(lightHitstop, _document.Number(HitColumn.Hitstop.For(light)));
    }

    [Fact]
    public void AddingKeepsValuesInRange()
    {
        HitRow heavy = Row("5c");

        HitColumnEdit.Add(_document, [heavy], HitColumn.Hitstun, -1000);

        Assert.Equal(0, _document.Number(HitColumn.Hitstun.For(heavy)));
    }

    [Fact]
    public void FrameAdvantageFollowsBlockstunEdits()
    {
        HitRow heavy = Row("5c");
        int? before = MoveAdvantage.Of(_document, heavy.File).OnBlock;

        HitColumnEdit.Add(_document, [heavy], HitColumn.Blockstun, 3);

        Assert.NotNull(before);
        Assert.Equal(before + 3, MoveAdvantage.Of(_document, heavy.File).OnBlock);
        Assert.Equal("+3", MoveAdvantage.Text(3));
        Assert.Equal("-5", MoveAdvantage.Text(-5));
    }

    [Theory]
    [InlineData(20000, 1000, 41, 20, 210000)]
    [InlineData(20000, 0, 0, 6000, 120_000_000)]
    public void AJumpFliesAsTheLogicMovesIt(int velocity, int gravity, int airFrames, int apexFrame, int apexHeight) =>
        Assert.Equal(new JumpArc(airFrames, apexFrame, apexHeight), JumpArc.Of(velocity, gravity));

    private HitRow Row(string moveId) => _rows[IndexOf(moveId)];

    private int IndexOf(string moveId)
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            if (_rows[i].MoveId == moveId)
            {
                return i;
            }
        }

        throw new InvalidOperationException($"no row for {moveId}");
    }
}
