#if TOOLS
using System.Collections.Generic;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Look;

/// <summary>
/// Draws a move's input the way a player reads it: the motion as arrows, then each button as a small keycap
/// (<see cref="Map.MoveGlyphs"/>). Used on map cards, in the drawer header and in chain chips.
/// </summary>
public static class KeycapArt
{
    private const float ArrowSize = 14f;
    private const float CapSize = 11f;
    private const float CapHeight = 20f;
    private const float CapPadding = 6f;
    private const float Gap = 3f;
    private const float Radius = 6f;

    /// <summary>The drawn width, in screen pixels.</summary>
    public static float Width(MapLook look, string arrows, IReadOnlyList<string> caps)
    {
        float width = arrows.Length == 0 ? 0f : ArrowWidth(look, arrows) + look.Px(Gap);
        foreach (string cap in caps)
        {
            width += CapWidth(look, cap) + look.Px(Gap);
        }

        return Mathf.Max(0f, width - look.Px(Gap));
    }

    /// <summary>Draws the input with its left edge at <paramref name="left"/>, centred on <paramref name="middleY"/>.</summary>
    public static void Draw(CanvasItem canvas, MapLook look, float left, float middleY, string arrows,
        IReadOnlyList<string> caps, float alpha = 1f)
    {
        float x = left;
        if (arrows.Length > 0)
        {
            int size = look.FontSize(ArrowSize);
            float baseline = middleY + ((look.Mono.GetAscent(size) - look.Mono.GetDescent(size)) / 2f);
            canvas.DrawString(look.Mono, new Vector2(x, baseline), arrows, HorizontalAlignment.Left, -1f, size,
                MapPalette.Muted with { A = alpha });
            x += ArrowWidth(look, arrows) + look.Px(Gap);
        }

        int capSize = look.FontSize(CapSize);
        float height = look.Px(CapHeight);
        foreach (string cap in caps)
        {
            float width = CapWidth(look, cap);
            var rect = new Rect2(x, middleY - (height / 2f), width, height);
            ShapeArt.Panel(canvas, rect, MapPalette.Keycap with { A = alpha }, look.Px(Radius));
            float capBaseline = middleY + ((look.Mono.GetAscent(capSize) - look.Mono.GetDescent(capSize)) / 2f);
            canvas.DrawString(look.Mono, new Vector2(x, capBaseline), cap, HorizontalAlignment.Center, width, capSize,
                MapPalette.Text with { A = alpha });
            x += width + look.Px(Gap);
        }
    }

    private static float ArrowWidth(MapLook look, string arrows) =>
        look.Mono.GetStringSize(arrows, HorizontalAlignment.Left, -1f, look.FontSize(ArrowSize)).X;

    private static float CapWidth(MapLook look, string cap) => Mathf.Max(
        look.Px(CapHeight),
        look.Mono.GetStringSize(cap, HorizontalAlignment.Left, -1f, look.FontSize(CapSize)).X + look.Px(2f * CapPadding));
}
#endif
