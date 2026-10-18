namespace FightingGame.VfxLab;

/// <summary>The control the Lab gives a shader uniform.</summary>
public enum UniformControlKind
{
    Slider,
    WholeSlider,
    Toggle,
    Colour,

    /// <summary>One slider per component.</summary>
    Vector,
}
