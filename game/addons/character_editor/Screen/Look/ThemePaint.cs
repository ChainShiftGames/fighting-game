#if TOOLS
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Look;

/// <summary>Fills a <see cref="Theme"/> with the editor's styles, one group of controls per method.</summary>
/// <remarks>Sizes are the 1× design, multiplied by the editor's display scale.</remarks>
public sealed class ThemePaint(Theme theme, float scale)
{
    private static readonly string[] _buttonStates = ["normal", "hover", "pressed", "hover_pressed", "disabled", "focus"];
    private static readonly string[] _fontColors =
        ["font_color", "font_hover_color", "font_pressed_color", "font_hover_pressed_color", "font_focus_color"];

    public void Labels(Font display, Font bold, Font mono)
    {
        theme.SetColor("font_color", "Label", MapPalette.Text);
        Label(MapTheme.Title, display, 18, MapPalette.Text);
        Label(MapTheme.Heading, display, 24, MapPalette.Text);
        Label(MapTheme.Section, bold, 11, MapPalette.Muted);
        Label(MapTheme.MutedLabel, null, 12, MapPalette.Muted);
        Label(MapTheme.FactLabel, null, 13, MapPalette.TextSoft);
        Label(MapTheme.FactHint, null, 11, MapPalette.Muted);
        Label(MapTheme.FactValue, mono, 15, MapPalette.Text);
        theme.SetColor("font_color", "TooltipLabel", MapPalette.Text);
        theme.SetStylebox("panel", "TooltipPanel", Box(MapPalette.Raised, MapPalette.Line, 8, 10, 6));
    }

    public void Buttons(Font bold)
    {
        Button("Button", bold, Box(MapPalette.Raised, MapPalette.Line, 8, 12, 6),
            Box(MapPalette.Raised, MapPalette.ArrowDim, 8, 12, 6), Box(MapPalette.Line, MapPalette.Glow, 8, 12, 6),
            MapPalette.Text);
        Button("OptionButton", bold, Box(MapPalette.Background, MapPalette.Line, 8, 10, 6),
            Box(MapPalette.Background, MapPalette.ArrowDim, 8, 10, 6), Box(MapPalette.Raised, MapPalette.Glow, 8, 10, 6),
            MapPalette.Text);
        Button("MenuButton", bold, Box(MapPalette.Panel, MapPalette.Line, 8, 10, 6),
            Box(MapPalette.Raised, MapPalette.ArrowDim, 8, 10, 6), Box(MapPalette.Raised, MapPalette.Glow, 8, 10, 6),
            MapPalette.Text);
        Variation(MapTheme.Segment, "Button", bold, Box(Colors.Transparent, null, 7, 14, 6),
            Box(MapPalette.Panel, null, 7, 14, 6), Box(MapPalette.Raised, null, 7, 14, 6), MapPalette.Muted);
        theme.SetColor("font_pressed_color", MapTheme.Segment, MapPalette.Text);
        theme.SetColor("font_hover_pressed_color", MapTheme.Segment, MapPalette.Text);
        Variation(MapTheme.Chip, "Button", bold, Box(MapPalette.Raised, null, 8, 11, 5),
            Box(MapPalette.Raised, MapPalette.ArrowDim, 8, 11, 5), Box(MapPalette.Line, null, 8, 11, 5), MapPalette.Good);
        Variation(MapTheme.Ghost, "Button", bold, Box(MapPalette.Panel, MapPalette.Line, 8, 8, 5),
            Box(MapPalette.Raised, MapPalette.ArrowDim, 8, 8, 5), Box(MapPalette.Raised, MapPalette.Glow, 8, 8, 5),
            MapPalette.Text);
        StyleBox none = new StyleBoxEmpty();
        Variation(MapTheme.Tab, "Button", bold, none, none, none, MapPalette.Muted);
        theme.SetColor("font_hover_color", MapTheme.Tab, MapPalette.TextSoft);
        theme.SetColor("font_pressed_color", MapTheme.Tab, MapPalette.Text);
        theme.SetColor("font_hover_pressed_color", MapTheme.Tab, MapPalette.Text);
        Variation(MapTheme.Flat, "Button", bold, none, none, none, MapPalette.Text);

        StyleBox check = Box(Colors.Transparent, null, 6, 4, 3);
        foreach (string type in new[] { "CheckBox", "CheckButton" })
        {
            foreach (string state in _buttonStates)
            {
                theme.SetStylebox(state, type, check);
            }

            Colours(type, MapPalette.TextSoft);
            theme.SetColor("font_pressed_color", type, MapPalette.Text);
        }
    }

    public void Fields()
    {
        StyleBox field = Box(MapPalette.Background, MapPalette.Line, 7, 8, 5);
        StyleBox focus = Box(MapPalette.Background, MapPalette.Glow, 7, 8, 5);
        StyleBox readOnly = Box(MapPalette.Panel, MapPalette.LineSoft, 7, 8, 5);
        foreach (string type in new[] { "LineEdit", "TextEdit" })
        {
            theme.SetStylebox("normal", type, field);
            theme.SetStylebox("focus", type, focus);
            theme.SetStylebox("read_only", type, readOnly);
            theme.SetColor("font_color", type, MapPalette.Text);
            theme.SetColor("font_readonly_color", type, MapPalette.Muted);
            theme.SetColor("font_placeholder_color", type, MapPalette.Faint);
            theme.SetColor("caret_color", type, MapPalette.Glow);
            theme.SetColor("selection_color", type, MapPalette.Glow with { A = 0.3f });
        }

        theme.SetStylebox("panel", "ItemList", Box(MapPalette.Background, MapPalette.Line, 8, 6, 6));
        theme.SetStylebox("selected", "ItemList", Box(MapPalette.Raised, null, 6, 4, 2));
        theme.SetStylebox("selected_focus", "ItemList", Box(MapPalette.Raised, MapPalette.Glow, 6, 4, 2));
        theme.SetStylebox("hovered", "ItemList", Box(MapPalette.Panel, null, 6, 4, 2));
        theme.SetColor("font_color", "ItemList", MapPalette.TextSoft);
        theme.SetColor("font_selected_color", "ItemList", MapPalette.Text);
        theme.SetColor("font_hovered_color", "ItemList", MapPalette.Text);
    }

    public void Popups()
    {
        StyleBox panel = Box(MapPalette.Panel, MapPalette.Line, 10, 6, 6);
        theme.SetStylebox("panel", "PopupMenu", panel);
        theme.SetStylebox("hover", "PopupMenu", Box(MapPalette.Raised, null, 6, 8, 4));
        theme.SetStylebox("separator", "PopupMenu", new StyleBoxLine { Color = MapPalette.LineSoft, Thickness = 1 });
        theme.SetColor("font_color", "PopupMenu", MapPalette.TextSoft);
        theme.SetColor("font_hover_color", "PopupMenu", MapPalette.Text);
        theme.SetColor("font_disabled_color", "PopupMenu", MapPalette.Faint);
        theme.SetConstant("v_separation", "PopupMenu", Px(6));
        theme.SetStylebox("panel", "PopupPanel", Box(MapPalette.Panel, MapPalette.Line, 12, 12, 12));
        theme.SetStylebox("panel", "AcceptDialog", Box(MapPalette.Panel, null, 0, 16, 16));
    }

    public void Containers()
    {
        theme.SetStylebox("panel", "PanelContainer", Box(MapPalette.Panel, null, 0, 0, 0));
        theme.SetTypeVariation(MapTheme.Card, "PanelContainer");
        theme.SetStylebox("panel", MapTheme.Card, Box(MapPalette.Raised, MapPalette.LineSoft, 12, 12, 10));
        theme.SetStylebox("panel", "ScrollContainer", new StyleBoxEmpty());
        foreach (string bar in new[] { "VScrollBar", "HScrollBar" })
        {
            theme.SetStylebox("scroll", bar, new StyleBoxEmpty { ContentMarginLeft = Px(3), ContentMarginRight = Px(3) });
            theme.SetStylebox("grabber", bar, Box(MapPalette.Line, null, 4, 3, 3));
            theme.SetStylebox("grabber_highlight", bar, Box(MapPalette.ArrowDim, null, 4, 3, 3));
            theme.SetStylebox("grabber_pressed", bar, Box(MapPalette.Muted, null, 4, 3, 3));
        }

        theme.SetStylebox("separator", "HSeparator", new StyleBoxLine { Color = MapPalette.LineSoft, Thickness = 1 });
        theme.SetConstant("separation", "HSeparator", Px(14));
        theme.SetConstant("separation", "HSplitContainer", 0);
        theme.SetConstant("minimum_grab_thickness", "HSplitContainer", Px(6));
        theme.SetStylebox("split_bar_background", "HSplitContainer", new StyleBoxFlat { BgColor = MapPalette.Line });
        theme.SetConstant("separation", "VBoxContainer", Px(6));
        theme.SetConstant("separation", "HBoxContainer", Px(8));
        theme.SetConstant("h_separation", "HFlowContainer", Px(6));
        theme.SetConstant("v_separation", "HFlowContainer", Px(6));
    }

    private int Px(float design) => Mathf.RoundToInt(design * scale);

    private StyleBoxFlat Box(Color background, Color? border, float radius, float marginX, float marginY)
    {
        var box = new StyleBoxFlat { BgColor = background, AntiAliasing = true };
        box.SetCornerRadiusAll(Px(radius));
        box.ContentMarginLeft = box.ContentMarginRight = Px(marginX);
        box.ContentMarginTop = box.ContentMarginBottom = Px(marginY);
        if (border is Color line)
        {
            box.BorderColor = line;
            box.SetBorderWidthAll(Mathf.Max(1, Px(1)));
        }

        return box;
    }

    private void Label(string variation, Font? font, float size, Color color)
    {
        theme.SetTypeVariation(variation, "Label");
        if (font is not null)
        {
            theme.SetFont("font", variation, font);
        }

        theme.SetFontSize("font_size", variation, Px(size));
        theme.SetColor("font_color", variation, color);
    }

    private void Button(string type, Font bold, StyleBox normal, StyleBox hover, StyleBox pressed, Color text)
    {
        theme.SetStylebox("normal", type, normal);
        theme.SetStylebox("hover", type, hover);
        theme.SetStylebox("pressed", type, pressed);
        theme.SetStylebox("hover_pressed", type, pressed);
        theme.SetStylebox("disabled", type, normal);
        theme.SetStylebox("focus", type, new StyleBoxEmpty());
        theme.SetFont("font", type, bold);
        theme.SetFontSize("font_size", type, Px(12.5f));
        Colours(type, text);
        theme.SetColor("font_disabled_color", type, MapPalette.Faint);
    }

    private void Variation(string variation, string baseType, Font bold, StyleBox normal, StyleBox hover,
        StyleBox pressed, Color text)
    {
        theme.SetTypeVariation(variation, baseType);
        Button(variation, bold, normal, hover, pressed, text);
    }

    private void Colours(string type, Color text)
    {
        foreach (string name in _fontColors)
        {
            theme.SetColor(name, type, text);
        }
    }
}
#endif
