#if TOOLS
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Look;

/// <summary>
/// The fonts and size scale a custom-drawn control of the editor draws with, read from the theme the editor screen
/// sets (<see cref="MapTheme"/>), so drawn text matches the themed controls around it.
/// </summary>
/// <param name="Scale">The Godot editor's display scale: every size in the editor's look is multiplied by it.</param>
public sealed record MapLook(Font Display, Font Body, Font Bold, Font Mono, float Scale)
{
    public static MapLook Of(Control control) => new(
        control.GetThemeFont(MapTheme.DisplayFont, MapTheme.LookType),
        control.GetThemeFont(MapTheme.BodyFont, MapTheme.LookType),
        control.GetThemeFont(MapTheme.BoldFont, MapTheme.LookType),
        control.GetThemeFont(MapTheme.MonoFont, MapTheme.LookType),
        Mathf.Max(0.5f, control.GetThemeConstant(MapTheme.ScaleConstant, MapTheme.LookType) / 100f));

    /// <summary>A length of the 1× design, in screen pixels.</summary>
    public float Px(float designPixels) => designPixels * Scale;

    /// <summary>A font size of the 1× design, in screen pixels.</summary>
    public int FontSize(float designPixels) => Mathf.Max(1, Mathf.RoundToInt(designPixels * Scale));
}
#endif
