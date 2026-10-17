using System;
using Sim.Characters;
using Sim.Data.Tests.Support;
using Sim.Data.Validation;
using Sim.Math;
using Xunit;

namespace Sim.Data.Tests.Characters;

/// <summary>Move hits as schema 2 lists them (ADR-0022): several hits with their own frames and reaction data.</summary>
public sealed class MoveHitReaderTests
{
    // fixture_man's 5a: active frames 5-7, one hit box on 5-7.
    private const string Hit =
        "\"damage\": 300, \"chipDamage\": 0, \"guard\": \"mid\", \"hitstun\": 12, \"blockstun\": 9, \"hitstop\": 8, " +
        "\"pushback\": 12000";

    private static readonly string _move5A = Fixture.MoveFile("5a");

    private readonly InMemoryDataSource _data = Fixture.InMemory();

    [Fact]
    public void ASingleHitWithoutFramesCoversTheActiveFrames()
    {
        HitData hit = Assert.Single(HitsOf5A());

        Assert.Equal((5, 7), (hit.FirstFrame, hit.LastFrame));
    }

    [Fact]
    public void SeveralHitsKeepTheirOwnFrames()
    {
        SetHits($"{{ \"firstFrame\": 5, \"lastFrame\": 6, {Hit} }}", $"{{ \"firstFrame\": 7, \"lastFrame\": 7, {Hit} }}");

        HitData[] hits = HitsOf5A();

        Assert.Equal([(5, 6), (7, 7)], Array.ConvertAll(hits, hit => (hit.FirstFrame, hit.LastFrame)));
    }

    [Fact]
    public void ReactionDataIsRead()
    {
        SetHits($"{{ {Hit}, \"launch\": {{ \"x\": 1000, \"y\": 20000 }}, \"airLaunch\": {{ \"x\": 500, \"y\": 9000 }}, " +
            "\"groundBounce\": { \"y\": 15000 }, \"wallBounce\": { \"x\": 8000, \"y\": 6000 }, \"knockdownFrames\": 60, " +
            "\"otg\": true }");

        HitData hit = Assert.Single(HitsOf5A());

        Assert.Equal((1, new IntVector2(1000, 20000)), (hit.HasLaunch, hit.Launch));
        Assert.Equal((1, new IntVector2(500, 9000)), (hit.HasAirLaunch, hit.AirLaunch));
        Assert.Equal(15000, hit.GroundBounce);
        Assert.Equal(new IntVector2(8000, 6000), hit.WallBounce);
        Assert.Equal((60, 1), (hit.KnockdownFrames, hit.Otg));
    }

    [Fact]
    public void WithSeveralHitsEachNeedsItsFrames()
    {
        SetHits($"{{ {Hit} }}", $"{{ \"firstFrame\": 7, \"lastFrame\": 7, {Hit} }}");

        AssertErrorOn("hits[0].firstFrame", "required field is missing");
    }

    [Fact]
    public void HitsMustNotOverlap()
    {
        SetHits($"{{ \"firstFrame\": 5, \"lastFrame\": 6, {Hit} }}", $"{{ \"firstFrame\": 6, \"lastFrame\": 7, {Hit} }}");

        AssertErrorOn("hits[1]", "must be in frame order and must not overlap");
    }

    [Fact]
    public void EveryHitBoxFrameNeedsAHit()
    {
        SetHits($"{{ \"firstFrame\": 5, \"lastFrame\": 5, {Hit} }}", $"{{ \"firstFrame\": 7, \"lastFrame\": 7, {Hit} }}");

        AssertErrorOn("hits", "no hit covers frame 6");
    }

    [Fact]
    public void HitsOutsideTheActiveFramesAreReported()
    {
        SetHits($"{{ \"firstFrame\": 5, \"lastFrame\": 7, {Hit} }}", $"{{ \"firstFrame\": 8, \"lastFrame\": 8, {Hit} }}");

        AssertErrorOn("hits[1].firstFrame", "must be a whole number from 5 to 7, got 8");
    }

    [Fact]
    public void TheSchema1HitFieldPointsToTheMigrator()
    {
        _data.Replace(_move5A, "\"hits\": [", "\"hit\": {},\n  \"hits\": [");

        AssertErrorOn("hit", "tools/DataMigrate");
    }

    private void SetHits(params string[] hits)
    {
        string text = _data.GetText(_move5A);
        int start = text.IndexOf("\"hits\": [", StringComparison.Ordinal);
        int end = text.IndexOf(']', start);
        _data.SetText(_move5A, text[..start] + "\"hits\": [" + string.Join(", ", hits) + text[end..]);
    }

    private HitData[] HitsOf5A()
    {
        CharacterData character = Fixture.Load(_data).Characters[0];
        int move = Array.IndexOf(character.MoveIds.ToArray(), "5a");
        return character.HitsOf(in character.Moves[move]).ToArray();
    }

    private void AssertErrorOn(string field, string reasonPart)
    {
        DataValidationException exception = Assert.Throws<DataValidationException>(() => Fixture.Load(_data));
        Assert.Contains(exception.Errors, error => error.File == _move5A && error.Field == field &&
            error.Reason.Contains(reasonPart, StringComparison.Ordinal));
    }
}
