#if TOOLS
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.ControlPanel.Screen;

/// <summary>
/// The character editor's theme (<see cref="MapTheme"/>) with the controls only the control panel uses styled to
/// match: sliders, spin box arrows and the Hit feel table.
/// </summary>
public static class PanelTheme
{
    public static Theme Build(float scale)
    {
        Theme theme = MapTheme.Build(scale);
        int Px(float design) => Mathf.Max(1, Mathf.RoundToInt(design * scale));
        Sliders(theme, Px);
        Table(theme, Px);
        theme.SetColor("font_color", "SpinBox", MapPalette.Text);
        return theme;
    }

    private static void Sliders(Theme theme, System.Func<float, int> px)
    {
        var track = Box(MapPalette.Background, MapPalette.Line, px(3), 0, px(2));
        var filled = Box(MapPalette.Glow with { A = 0.55f }, null, px(3), 0, px(2));
        theme.SetStylebox("slider", "HSlider", track);
        theme.SetStylebox("grabber_area", "HSlider", filled);
        theme.SetStylebox("grabber_area_highlight", "HSlider", Box(MapPalette.Glow with { A = 0.75f }, null, px(3), 0, px(2)));
    }

    private static void Table(Theme theme, System.Func<float, int> px)
    {
        theme.SetStylebox("panel", "Tree", Box(MapPalette.Background, MapPalette.Line, px(8), px(6), px(6)));
        theme.SetStylebox("focus", "Tree", new StyleBoxEmpty());
        theme.SetStylebox("selected", "Tree", Box(MapPalette.Raised, null, px(4), 0, 0));
        theme.SetStylebox("selected_focus", "Tree", Box(MapPalette.Raised, MapPalette.Glow, px(4), 0, 0));
        theme.SetStylebox("hovered", "Tree", Box(MapPalette.Panel, null, px(4), 0, 0));
        theme.SetStylebox("cursor", "Tree", Box(Colors.Transparent, MapPalette.Glow, px(4), 0, 0));
        theme.SetStylebox("cursor_unfocused", "Tree", Box(Colors.Transparent, MapPalette.Line, px(4), 0, 0));
        StyleBox title = Box(MapPalette.Panel, MapPalette.LineSoft, 0, px(8), px(5));
        foreach (string state in new[] { "title_button_normal", "title_button_hover", "title_button_pressed" })
        {
            theme.SetStylebox(state, "Tree", title);
        }

        theme.SetColor("font_color", "Tree", MapPalette.TextSoft);
        theme.SetColor("font_selected_color", "Tree", MapPalette.Text);
        theme.SetColor("title_button_color", "Tree", MapPalette.Muted);
        theme.SetColor("guide_color", "Tree", MapPalette.LineSoft);
        theme.SetColor("relationship_line_color", "Tree", MapPalette.LineSoft);
        theme.SetConstant("draw_guides", "Tree", 1);
        theme.SetConstant("v_separation", "Tree", px(6));
        theme.SetConstant("h_separation", "Tree", px(10));
        theme.SetFont("title_button_font", "Tree", theme.GetFont(MapTheme.BoldFont, MapTheme.LookType));
        theme.SetFontSize("title_button_font_size", "Tree", px(11));
    }

    private static StyleBoxFlat Box(Color background, Color? border, int radius, int marginX, int marginY)
    {
        var box = new StyleBoxFlat { BgColor = background, AntiAliasing = true };
        box.SetCornerRadiusAll(radius);
        box.ContentMarginLeft = box.ContentMarginRight = marginX;
        box.ContentMarginTop = box.ContentMarginBottom = marginY;
        if (border is Color line)
        {
            box.BorderColor = line;
            box.SetBorderWidthAll(1);
        }

        return box;
    }
}
#endif
