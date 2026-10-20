#if TOOLS
using System.Collections.Generic;
using FightingGame.Addons.CharacterEditor.Map;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Map;

/// <summary>
/// The arrows of the move map as curves in design pixels: from a card's right edge to the next card's left edge,
/// straight down or up between neighbours in a column, around the left side to cards further down the column,
/// and into the box of a category when the move chains into the whole category.
/// </summary>
public static class MapEdges
{
    private const float MinBend = 40f;
    private const float AroundBend = 54f;
    private const float DownLane = 60f;
    private const float UpLane = 116f;
    private const float TipGap = 4f;

    /// <summary>One arrow; <see cref="Hot"/> when it leaves the selected move.</summary>
    public sealed record Edge(Vector2[] Points, bool Hot);

    public static List<Edge> Of(MapPlacement placement, IReadOnlyList<ChainLink> links, string? selected)
    {
        List<Edge> edges = [];
        foreach (ChainLink link in links)
        {
            if (!placement.Cards.TryGetValue(link.From, out Rect2 from))
            {
                continue;
            }

            bool hot = link.From == selected;
            foreach (string target in Targets(placement, link))
            {
                if (target == link.From)
                {
                    edges.Add(new Edge(SelfLoop(from), hot));
                }
                else if (placement.Cards.TryGetValue(target, out Rect2 to))
                {
                    edges.Add(new Edge(Between(placement, link.From, from, target, to), hot));
                }
            }

            if (link.Kind == ChainTargetKind.Category && placement.Groups.TryGetValue(link.Target, out Rect2 group))
            {
                edges.Add(new Edge(Forward(from, group), hot));
            }
        }

        return edges;
    }

    /// <summary>The cards a link points at; none when it points at a category box.</summary>
    public static IEnumerable<string> Targets(MapPlacement placement, ChainLink link)
    {
        switch (link.Kind)
        {
            case ChainTargetKind.Move:
                yield return link.Target;
                break;
            case ChainTargetKind.Jump:
                yield return MoveMapLayout.JumpEntry;
                break;
            case ChainTargetKind.Category when !placement.Groups.ContainsKey(link.Target):
                // No box (one shown member, or normals): an arrow to each member.
                foreach (MapColumn column in placement.Layout.Columns)
                {
                    foreach (string entry in column.Entries)
                    {
                        if (placement.Categories.TryGetValue(entry, out string? category) && category == link.Target)
                        {
                            yield return entry;
                        }
                    }
                }

                break;
        }
    }

    private static Vector2[] Between(MapPlacement placement, string fromId, Rect2 from, string toId, Rect2 to)
    {
        (int Column, int Row)? source = placement.Layout.Find(fromId);
        (int Column, int Row)? target = placement.Layout.Find(toId);
        if (source is not var (sourceColumn, sourceRow) || target is not var (targetColumn, targetRow))
        {
            return Forward(from, to);
        }

        if (targetColumn > sourceColumn)
        {
            return Forward(from, to);
        }

        if (targetColumn < sourceColumn)
        {
            Vector2 start = LeftMiddle(from);
            Vector2 end = RightMiddle(to) + new Vector2(TipGap, 0f);
            float bend = Mathf.Max(MinBend, (start.X - end.X) / 2f);
            return ShapeArtCurve(start, start - new Vector2(bend, 0f), end + new Vector2(bend, 0f), end);
        }

        if (Mathf.Abs(targetRow - sourceRow) == 1)
        {
            bool down = targetRow > sourceRow;
            float x = from.Position.X + (down ? DownLane : UpLane);
            return down
                ? [new Vector2(x, from.End.Y), new Vector2(x, to.Position.Y - TipGap)]
                : [new Vector2(x, from.Position.Y), new Vector2(x, to.End.Y + TipGap)];
        }

        Vector2 a = LeftMiddle(from);
        Vector2 b = LeftMiddle(to) - new Vector2(TipGap, 0f);
        return ShapeArtCurve(a, a - new Vector2(AroundBend, 0f), b - new Vector2(AroundBend, 0f), b);
    }

    private static Vector2[] Forward(Rect2 from, Rect2 to)
    {
        Vector2 start = RightMiddle(from);
        Vector2 end = LeftMiddle(to) - new Vector2(TipGap, 0f);
        float bend = Mathf.Max(MinBend, (end.X - start.X) / 2f);
        return ShapeArtCurve(start, start + new Vector2(bend, 0f), end - new Vector2(bend, 0f), end);
    }

    private static Vector2[] SelfLoop(Rect2 card)
    {
        Vector2 start = new(card.End.X - 34f, card.Position.Y);
        Vector2 end = new(card.End.X + TipGap, card.Position.Y + 18f);
        return ShapeArtCurve(start, start + new Vector2(0f, -34f), end + new Vector2(30f, 0f), end);
    }

    private static Vector2[] ShapeArtCurve(Vector2 a, Vector2 b, Vector2 c, Vector2 d) => Look.ShapeArt.Curve(a, b, c, d);

    private static Vector2 LeftMiddle(Rect2 rect) => new(rect.Position.X, rect.Position.Y + (rect.Size.Y / 2f));

    private static Vector2 RightMiddle(Rect2 rect) => new(rect.End.X, rect.Position.Y + (rect.Size.Y / 2f));
}
#endif
