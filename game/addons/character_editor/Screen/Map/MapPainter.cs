#if TOOLS
using System.Collections.Generic;
using System.Globalization;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Map;

/// <summary>
/// Draws the move map onto its canvas: dot grid, column headings, category boxes, arrows, then the cards on top.
/// <see cref="Look"/> is already zoomed, so one design pixel is <c>Look.Scale</c> screen pixels.
/// </summary>
public sealed class MapPainter(CanvasItem canvas, MapLook look, Vector2 offset)
{
    private const float GridStep = 22f;
    private const float CardRadius = 14f;
    private const float PadX = 12f;
    private const float NameY = 21f;
    private const float MetaY = 42f;
    private const float BarTop = 60f;
    private const float BarHeight = 6f;
    private const float BarGap = 2f;
    private const float DimAlpha = 0.55f;

    public MapLook Look => look;

    public Vector2 ToScreen(Vector2 design) => offset + (design * look.Scale);

    public Rect2 ToScreen(Rect2 design) => new(ToScreen(design.Position), design.Size * look.Scale);

    public void Grid(Vector2 size)
    {
        float step = look.Px(GridStep);
        if (step < 6f)
        {
            return;
        }

        float dot = Mathf.Max(1f, look.Px(1.2f));
        for (float x = Mathf.PosMod(offset.X, step); x < size.X; x += step)
        {
            for (float y = Mathf.PosMod(offset.Y, step); y < size.Y; y += step)
            {
                canvas.DrawRect(new Rect2(x, y, dot, dot), MapPalette.Line);
            }
        }
    }

    public void Headers(MapPlacement placement)
    {
        foreach ((CharacterEditor.Map.MapColumn column, Vector2 at) in placement.Headers)
        {
            Vector2 position = ToScreen(at);
            canvas.DrawString(look.Bold, position + new Vector2(0f, look.Px(11f)),
                column.Title.ToUpper(CultureInfo.InvariantCulture), HorizontalAlignment.Left, -1f,
                look.FontSize(11f), MapPalette.Muted);
            canvas.DrawString(look.Body, position + new Vector2(0f, look.Px(28f)), column.Hint,
                HorizontalAlignment.Left, -1f, look.FontSize(11.5f), MapPalette.Faint);
        }
    }

    public void Group(Rect2 design, string label)
    {
        Rect2 rect = ToScreen(design);
        ShapeArt.Dashed(canvas, rect, look.Px(20f), MapPalette.ArrowDim, Mathf.Max(1f, look.Px(1.5f)), look.Px(6f));
        int size = look.FontSize(11.5f);
        float width = look.Bold.GetStringSize(label, HorizontalAlignment.Left, -1f, size).X;
        var at = new Vector2(rect.Position.X + look.Px(16f), rect.Position.Y);
        canvas.DrawRect(new Rect2(at - new Vector2(look.Px(8f), look.Px(8f)), new Vector2(width + look.Px(16f),
            look.Px(16f))), MapPalette.Background);
        canvas.DrawString(look.Bold, at + new Vector2(0f, look.Px(4f)), label, HorizontalAlignment.Left, -1f, size,
            MapPalette.Muted);
    }

    public void Edge(MapEdges.Edge edge)
    {
        var points = new Vector2[edge.Points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = ToScreen(edge.Points[i]);
        }

        Vector2 direction = points[^1] - points[^2];
        if (edge.Hot)
        {
            canvas.DrawPolyline(points, MapPalette.Glow with { A = 0.22f }, look.Px(6f), antialiased: true);
            canvas.DrawPolyline(points, MapPalette.Glow, Mathf.Max(1f, look.Px(2f)), antialiased: true);
            ShapeArt.Arrowhead(canvas, points[^1] + (direction.Normalized() * look.Px(4f)), direction, look.Px(8f),
                MapPalette.Glow);
        }
        else
        {
            canvas.DrawPolyline(points, MapPalette.EdgeDim, Mathf.Max(1f, look.Px(1.6f)), antialiased: true);
            ShapeArt.Arrowhead(canvas, points[^1] + (direction.Normalized() * look.Px(4f)), direction, look.Px(7f),
                MapPalette.ArrowDim);
        }
    }

    public void Card(Rect2 design, MapCard card, bool selected, bool dim, bool hovered)
    {
        Rect2 rect = ToScreen(design);
        float alpha = dim ? DimAlpha : 1f;
        float radius = look.Px(CardRadius);
        if (selected)
        {
            ShapeArt.Panel(canvas, rect.Grow(look.Px(4f)), MapPalette.Glow with { A = 0.13f }, radius + look.Px(4f),
                shadow: MapPalette.Glow with { A = 0.25f }, shadowSize: look.Px(22f));
        }

        Color border = selected ? MapPalette.Glow : hovered ? MapPalette.ArrowDim : MapPalette.Line;
        ShapeArt.Panel(canvas, rect, MapPalette.Node with { A = MapPalette.Node.A * alpha }, radius,
            border with { A = alpha }, Mathf.Max(1f, look.Px(1f)), new Color(0f, 0f, 0f, 0.45f * alpha), look.Px(12f));

        float left = rect.Position.X + look.Px(PadX);
        float right = rect.End.X - look.Px(PadX);
        float inputWidth = KeycapArt.Width(look, card.Arrows, card.Keycaps);
        float nameWidth = right - left - inputWidth - look.Px(6f);
        canvas.DrawString(look.Bold, new Vector2(left, rect.Position.Y + look.Px(NameY)), card.Name,
            HorizontalAlignment.Left, nameWidth, look.FontSize(13f), MapPalette.Text with { A = alpha });
        KeycapArt.Draw(canvas, look, right - inputWidth, rect.Position.Y + look.Px(NameY - 4.5f), card.Arrows,
            card.Keycaps, alpha);
        float tagsLeft = GuardTags(right, rect.Position.Y + look.Px(MetaY), card.Guards, alpha);
        canvas.DrawString(look.Body, new Vector2(left, rect.Position.Y + look.Px(MetaY)), card.Meta,
            HorizontalAlignment.Left, tagsLeft - left - look.Px(4f), look.FontSize(11.5f),
            MapPalette.Muted with { A = alpha });
        Bar(new Rect2(left, rect.Position.Y + look.Px(BarTop), right - left, look.Px(BarHeight)), card.Phases, alpha);
        if (card.HasProblem)
        {
            canvas.DrawCircle(new Vector2(rect.End.X - look.Px(6f), rect.Position.Y + look.Px(6f)), look.Px(5f),
                MapPalette.Bad);
        }
    }

    // Small outlined tags (LOW, OVERHEAD…) ending at `right` on the line whose baseline is `baseline`; returns where
    // the first one starts.
    private float GuardTags(float right, float baseline, IReadOnlyList<string> guards, float alpha)
    {
        int size = look.FontSize(9.5f);
        float padding = look.Px(5f);
        float height = look.Px(14f);
        float x = right;
        for (int i = guards.Count - 1; i >= 0; i--)
        {
            string text = guards[i].ToUpper(CultureInfo.InvariantCulture);
            float width = look.Bold.GetStringSize(text, HorizontalAlignment.Left, -1f, size).X + (2f * padding);
            x -= width;
            var tag = new Rect2(x, baseline - height + look.Px(3f), width, height);
            Color color = GuardColor(guards[i]) with { A = alpha };
            ShapeArt.Panel(canvas, tag, color with { A = 0.16f * alpha }, height / 2f, color,
                Mathf.Max(1f, look.Px(1f)));
            canvas.DrawString(look.Bold, new Vector2(x + padding, baseline), text, HorizontalAlignment.Left, -1f,
                size, color);
            x -= look.Px(4f);
        }

        return x;
    }

    private static Color GuardColor(string guard) => guard switch
    {
        "low" => MapPalette.GuardLow,
        "overhead" => MapPalette.GuardOverhead,
        "unblockable" => MapPalette.Bad,
        _ => MapPalette.Muted,
    };

    /// <summary>A move's phases as one bar, each phase as long as its frames (the jump card: one accent bar).</summary>
    public void Bar(Rect2 rect, IReadOnlyList<int> phases, float alpha = 1f)
    {
        float radius = rect.Size.Y / 2f;
        if (phases.Count == 0)
        {
            ShapeArt.Panel(canvas, rect, MapPalette.Glow with { A = alpha }, radius);
            return;
        }

        Color[] colors = phases.Count == 1
            ? [MapPalette.Loop]
            : [MapPalette.Windup, MapPalette.Active, MapPalette.Recovery];
        int total = 0;
        int shown = 0;
        foreach (int length in phases)
        {
            total += length;
            shown += length > 0 ? 1 : 0;
        }

        float gap = look.Px(BarGap);
        float usable = rect.Size.X - (gap * Mathf.Max(0, shown - 1));
        float x = rect.Position.X;
        for (int i = 0; i < phases.Count; i++)
        {
            if (phases[i] <= 0)
            {
                continue;
            }

            float width = usable * phases[i] / Mathf.Max(1, total);
            ShapeArt.Panel(canvas, new Rect2(x, rect.Position.Y, width, rect.Size.Y),
                colors[i % colors.Length] with { A = alpha }, radius);
            x += width + gap;
        }
    }
}
#endif
