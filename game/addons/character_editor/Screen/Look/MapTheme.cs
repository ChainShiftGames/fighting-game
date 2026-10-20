#if TOOLS
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Look;

/// <summary>
/// The character editor's own theme, set on its screen so every control under it is styled the same way and none
/// looks like a stock editor inspector: <see cref="MapPalette"/> colours, the game's Marcellus headings, rounded
/// flat controls, and the fonts custom-drawn controls read through <see cref="MapLook"/>.
/// </summary>
public static class MapTheme
{
    /// <summary>The theme type holding the shared fonts and scale (<see cref="MapLook"/>).</summary>
    public const string LookType = "MoveMap";
    public const string DisplayFont = "display";
    public const string BodyFont = "body";
    public const string BoldFont = "bold";
    public const string MonoFont = "mono";
    public const string ScaleConstant = "scale_percent";

    // Variations of stock controls, set through ThemeTypeVariation.
    public const string Segment = "MapSegment";
    public const string Chip = "MapChip";
    public const string Tab = "MapTab";
    public const string Ghost = "MapGhost";
    public const string Flat = "MapFlat";
    public const string Title = "MapTitle";
    public const string Heading = "MapHeading";
    public const string Section = "MapSection";
    public const string MutedLabel = "MapMuted";
    public const string FactLabel = "MapFactLabel";
    public const string FactHint = "MapFactHint";
    public const string FactValue = "MapFactValue";
    public const string Card = "MapCard";

    private const string DisplayPath = "res://Assets/UI/Fonts/Marcellus-Regular.ttf";
    private static readonly string[] _bodyNames = ["Hanken Grotesk", "Segoe UI", "Inter", "Noto Sans", "Arial"];
    private static readonly string[] _monoNames = ["JetBrains Mono", "Cascadia Mono", "Consolas", "DejaVu Sans Mono"];

    /// <param name="scale">The Godot editor's display scale.</param>
    public static Theme Build(float scale)
    {
        Font body = System(_bodyNames, 500);
        Font bold = System(_bodyNames, 650);
        Font mono = System(_monoNames, 700);
        Font display = ResourceLoader.Exists(DisplayPath) ? GD.Load<Font>(DisplayPath) : bold;
        var theme = new Theme { DefaultFont = body, DefaultFontSize = Size(13, scale) };
        theme.SetFont(DisplayFont, LookType, display);
        theme.SetFont(BodyFont, LookType, body);
        theme.SetFont(BoldFont, LookType, bold);
        theme.SetFont(MonoFont, LookType, mono);
        theme.SetConstant(ScaleConstant, LookType, Mathf.RoundToInt(scale * 100f));

        var paint = new ThemePaint(theme, scale);
        paint.Labels(display, bold, mono);
        paint.Buttons(bold);
        paint.Fields();
        paint.Popups();
        paint.Containers();
        return theme;
    }

    private static int Size(float design, float scale) => Mathf.Max(1, Mathf.RoundToInt(design * scale));

    private static SystemFont System(string[] names, int weight) => new()
    {
        FontNames = names,
        FontWeight = weight,
        Antialiasing = TextServer.FontAntialiasing.Gray,
        SubpixelPositioning = TextServer.SubpixelPositioning.Auto,
    };
}
#endif
