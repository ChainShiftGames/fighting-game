using FightingGame.VfxLab;
using FightingGame.VfxLab.Files;
using Godot;
using Xunit;

namespace FightingGame.Tests.VfxLab;

public sealed class UniformSpecTests
{
    [Fact]
    public void TakesTheRangeFromTheShadersHint()
    {
        UniformSpec? spec = UniformSpec.From("aura", "orb", Variant.Type.Float, PropertyHint.Range, "0.3,2.5,0.01", LabValue.Number(0.92f), [0f, 9f]);

        Assert.NotNull(spec);
        Assert.Equal(UniformControlKind.Slider, spec.Kind);
        Assert.Equal((0.3f, 2.5f, 0.01f), (spec.Min, spec.Max, spec.Step));
        Assert.Equal("orb", spec.Group);
    }

    [Fact]
    public void FallsBackToTheSheetsRangeThenToTwiceTheDefault()
    {
        UniformSpec? sheet = UniformSpec.From("aura", string.Empty, Variant.Type.Float, PropertyHint.None, string.Empty, LabValue.Number(0.92f), [0f, 4f, 0.5f]);
        UniformSpec? guess = UniformSpec.From("aura", string.Empty, Variant.Type.Float, PropertyHint.None, string.Empty, LabValue.Number(3f), null);
        UniformSpec? whole = UniformSpec.From("arms", string.Empty, Variant.Type.Int, PropertyHint.None, string.Empty, LabValue.Number(1f), null);

        Assert.Equal((0f, 4f, 0.5f), (sheet!.Min, sheet.Max, sheet.Step));
        Assert.Equal((0f, 6f), (guess!.Min, guess.Max));
        Assert.Equal((UniformControlKind.WholeSlider, 4f, 1f), (whole!.Kind, whole.Max, whole.Step));
    }

    [Theory]
    [InlineData("t", Variant.Type.Float)]
    [InlineData("progress", Variant.Type.Float)]
    [InlineData("card_size", Variant.Type.Vector2)]
    [InlineData("screen_texture", Variant.Type.Object)]
    [InlineData("frames", Variant.Type.PackedFloat32Array)]
    public void GivesNoControlToWhatTheLabDrivesOrCantShow(string name, Variant.Type type)
    {
        Assert.Null(UniformSpec.From(name, string.Empty, type, PropertyHint.None, string.Empty, LabValue.Number(0f), null));
    }

    [Fact]
    public void ColoursKnowWhetherTheyHaveAlpha()
    {
        LabValue black = LabValue.Colour(0f, 0f, 0f, 1f);

        Assert.True(UniformSpec.From("c", string.Empty, Variant.Type.Color, PropertyHint.None, string.Empty, black, null)!.HasAlpha);
        Assert.False(UniformSpec.From("c", string.Empty, Variant.Type.Color, PropertyHint.ColorNoAlpha, string.Empty, black, null)!.HasAlpha);
    }
}
