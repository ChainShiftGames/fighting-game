#if TOOLS
using System.Collections.Generic;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Drawer;

/// <summary>The drawer's subtitle: "Press ↓↘→ [C] · special move", with the input drawn as on the map's cards.</summary>
[Tool]
public partial class InputLine : Control
{
    private const float FontSize = 13f;
    private const float Height = 22f;
    private string _before = "";
    private string _after = "";
    private string _arrows = "";
    private IReadOnlyList<string> _keycaps = [];

    /// <summary>Text, then the input (none when it has no buttons), then more text.</summary>
    public void ShowLine(string before, string arrows, IReadOnlyList<string> keycaps, string after)
    {
        _before = before;
        _arrows = arrows;
        _keycaps = keycaps;
        _after = after;
        CustomMinimumSize = new Vector2(0f, MapLook.Of(this).Px(Height));
        QueueRedraw();
    }

    public override void _Draw()
    {
        MapLook look = MapLook.Of(this);
        int size = look.FontSize(FontSize);
        float middle = Size.Y / 2f;
        float baseline = middle + ((look.Body.GetAscent(size) - look.Body.GetDescent(size)) / 2f);
        float x = 0f;
        x = Text(look, x, baseline, _before, size);
        if (_keycaps.Count > 0 || _arrows.Length > 0)
        {
            KeycapArt.Draw(this, look, x, middle, _arrows, _keycaps);
            x += KeycapArt.Width(look, _arrows, _keycaps) + look.Px(4f);
        }

        Text(look, x, baseline, _after, size);
    }

    private float Text(MapLook look, float x, float baseline, string text, int size)
    {
        if (text.Length == 0)
        {
            return x;
        }

        DrawString(look.Body, new Vector2(x, baseline), text, HorizontalAlignment.Left, -1f, size, MapPalette.Muted);
        return x + look.Body.GetStringSize(text, HorizontalAlignment.Left, -1f, size).X + look.Px(4f);
    }
}
#endif
