#if TOOLS
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.ControlPanel.Screen;

/// <summary>
/// What the control panel's controls share: the character editor's look (<see cref="MapTheme"/>,
/// <see cref="MapPalette"/>), so both tabs read as one tool, plus the panel's colours for unsaved values and problems.
/// </summary>
public sealed class PanelLook
{
    private PanelLook(Theme theme, float scale)
    {
        Theme = theme;
        Scale = scale;
        Mono = theme.GetFont(MapTheme.MonoFont, MapTheme.LookType);
        Display = theme.GetFont(MapTheme.DisplayFont, MapTheme.LookType);
    }

    /// <summary>The theme the screen sets, so every control under it is styled.</summary>
    public Theme Theme { get; }

    /// <summary>The editor's display scale; sizes in code are multiplied by it.</summary>
    public float Scale { get; }

    /// <summary>The font numbers are shown in, as in the character editor's steppers.</summary>
    public Font Mono { get; }

    /// <summary>The game's heading font (Marcellus).</summary>
    public Font Display { get; }

    /// <summary>The colour of an unsaved value's name.</summary>
    public Color Changed => MapPalette.Warn;

    /// <summary>The colour of problems that stop a save.</summary>
    public Color Problem => MapPalette.Bad;

    public static PanelLook FromEditor()
    {
        float scale = EditorInterface.Singleton.GetEditorScale();
        return new PanelLook(PanelTheme.Build(scale), scale);
    }

    /// <summary>A length of the 1× design, in screen pixels.</summary>
    public int Px(float design) => Mathf.RoundToInt(design * Scale);
}
#endif
