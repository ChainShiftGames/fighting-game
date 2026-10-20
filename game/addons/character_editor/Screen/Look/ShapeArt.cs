#if TOOLS
using System.Collections.Generic;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Look;

/// <summary>
/// Shapes the editor's custom-drawn controls share that <see cref="CanvasItem"/> has no call for: rounded panels
/// with borders, gradient fills, dashed outlines, curved arrows.
/// </summary>
public static class ShapeArt
{
    private const int CornerSteps = 6;
    private const int CurveSteps = 28;

    /// <summary>A rounded rectangle with an optional border and soft shadow, drawn through a flat style box.</summary>
    public static void Panel(CanvasItem canvas, Rect2 rect, Color fill, float radius, Color? border = null,
        float borderWidth = 1f, Color? shadow = null, float shadowSize = 0f)
    {
        var box = new StyleBoxFlat { BgColor = fill, AntiAliasing = true };
        box.SetCornerRadiusAll(Mathf.RoundToInt(radius));
        if (border is Color line)
        {
            box.BorderColor = line;
            box.SetBorderWidthAll(Mathf.Max(1, Mathf.RoundToInt(borderWidth)));
        }

        if (shadow is Color shade)
        {
            box.ShadowColor = shade;
            box.ShadowSize = Mathf.RoundToInt(shadowSize);
            box.ShadowOffset = new Vector2(0f, shadowSize / 3f);
        }

        canvas.DrawStyleBox(box, rect);
    }

    /// <summary>A rounded rectangle filled left to right from <paramref name="left"/> to <paramref name="right"/>.</summary>
    public static void Gradient(CanvasItem canvas, Rect2 rect, float radius, Color left, Color right)
    {
        Vector2[] points = Rounded(rect, radius);
        var colors = new Color[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            colors[i] = left.Lerp(right, (points[i].X - rect.Position.X) / Mathf.Max(1f, rect.Size.X));
        }

        canvas.DrawPolygon(points, colors);
    }

    /// <summary>A dashed outline of a rounded rectangle.</summary>
    public static void Dashed(CanvasItem canvas, Rect2 rect, float radius, Color color, float width, float dash)
    {
        Vector2[] outline = Rounded(rect, radius);
        bool drawing = true;
        float left = dash;
        for (int i = 0; i < outline.Length; i++)
        {
            Vector2 from = outline[i];
            Vector2 to = outline[(i + 1) % outline.Length];
            float length = from.DistanceTo(to);
            float done = 0f;
            while (done < length)
            {
                float step = Mathf.Min(left, length - done);
                Vector2 a = from.Lerp(to, done / length);
                Vector2 b = from.Lerp(to, (done + step) / length);
                if (drawing)
                {
                    canvas.DrawLine(a, b, color, width, antialiased: true);
                }

                done += step;
                left -= step;
                if (left <= 0f)
                {
                    drawing = !drawing;
                    left = dash;
                }
            }
        }
    }

    /// <summary>Points along a cubic curve from <paramref name="from"/> to <paramref name="to"/>.</summary>
    public static Vector2[] Curve(Vector2 from, Vector2 control1, Vector2 control2, Vector2 to)
    {
        var points = new Vector2[CurveSteps + 1];
        for (int i = 0; i <= CurveSteps; i++)
        {
            float t = i / (float)CurveSteps;
            float u = 1f - t;
            points[i] = (u * u * u * from) + (3f * u * u * t * control1) + (3f * u * t * t * control2) + (t * t * t * to);
        }

        return points;
    }

    /// <summary>A filled arrowhead with its tip at <paramref name="tip"/>, pointing along <paramref name="direction"/>.</summary>
    public static void Arrowhead(CanvasItem canvas, Vector2 tip, Vector2 direction, float size, Color color)
    {
        Vector2 along = direction.Normalized();
        Vector2 across = new(-along.Y, along.X);
        Vector2 back = tip - (along * size);
        canvas.DrawColoredPolygon([tip, back + (across * size * 0.55f), back - (across * size * 0.55f)], color);
    }

    private static Vector2[] Rounded(Rect2 rect, float radius)
    {
        float r = Mathf.Min(radius, Mathf.Min(rect.Size.X, rect.Size.Y) / 2f);
        List<Vector2> points = [];
        Corner(points, new Vector2(rect.End.X - r, rect.Position.Y + r), r, -90f);
        Corner(points, new Vector2(rect.End.X - r, rect.End.Y - r), r, 0f);
        Corner(points, new Vector2(rect.Position.X + r, rect.End.Y - r), r, 90f);
        Corner(points, new Vector2(rect.Position.X + r, rect.Position.Y + r), r, 180f);
        return [.. points];
    }

    private static void Corner(List<Vector2> points, Vector2 centre, float radius, float startDegrees)
    {
        for (int i = 0; i <= CornerSteps; i++)
        {
            float angle = Mathf.DegToRad(startDegrees + (90f * i / CornerSteps));
            points.Add(centre + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius));
        }
    }
}
#endif
