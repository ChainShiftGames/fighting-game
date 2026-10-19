using Godot;

namespace FightingGame.Presentation.World;

/// <summary>
/// Puts the anime colour grade on a stage's environment, or gives the stage its own look back: a linear tonemap, so
/// flat painted colours aren't compressed and desaturated as a filmic curve does, then saturated mid-tones and shadows
/// tinted violet instead of grey.
/// </summary>
public sealed class AnimeGrade
{
    // Developer settings (presentation).
    private const float Saturation = 1.12f;
    private const float Contrast = 1.1f;
    private static readonly Color _shadowTint = new(0.035f, 0.015f, 0.06f);

    // Each channel is remapped on its own, so lifting black to violet tints the shadows and fades out toward white.
    private static readonly GradientTexture1D _shadowCurve = new()
    {
        Gradient = new Gradient { Colors = [_shadowTint, Colors.White], Offsets = [0f, 1f] },
    };

    private readonly Environment _environment;
    private readonly Environment.ToneMapper _stageTonemap;
    private readonly bool _stageAdjustments;
    private bool? _shown;

    public AnimeGrade(Environment environment)
    {
        _environment = environment;
        _stageTonemap = environment.TonemapMode;
        _stageAdjustments = environment.AdjustmentEnabled;
    }

    /// <summary>Shows the grade or the stage's own look; cheap to call every frame.</summary>
    public void Show(bool graded)
    {
        if (_shown == graded)
        {
            return;
        }

        _shown = graded;
        _environment.TonemapMode = graded ? Environment.ToneMapper.Linear : _stageTonemap;
        _environment.AdjustmentEnabled = graded || _stageAdjustments;
        if (graded)
        {
            _environment.AdjustmentSaturation = Saturation;
            _environment.AdjustmentContrast = Contrast;
            _environment.AdjustmentColorCorrection = _shadowCurve;
        }
        else
        {
            _environment.AdjustmentSaturation = 1f;
            _environment.AdjustmentContrast = 1f;
            _environment.AdjustmentColorCorrection = null;
        }
    }
}
