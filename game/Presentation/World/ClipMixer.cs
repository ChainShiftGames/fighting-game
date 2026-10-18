using Godot;

namespace FightingGame.Presentation.World;

/// <summary>
/// Shows a fighter model's pose: one clip at a seeked time, or a crossfade between two held poses, through an
/// animation tree that never advances by itself (ADR-0028). Times and weights come from logic frames.
/// </summary>
public sealed class ClipMixer
{
    private static readonly StringName _fromSeek = "parameters/from_seek/seek_request";
    private static readonly StringName _toSeek = "parameters/to_seek/seek_request";
    private static readonly StringName _blend = "parameters/mix/blend_amount";

    private readonly AnimationNodeAnimation _from = new();
    private readonly AnimationNodeAnimation _to = new();

    public ClipMixer()
    {
        var graph = new AnimationNodeBlendTree();
        graph.AddNode("from", _from);
        graph.AddNode("to", _to);
        graph.AddNode("from_seek", new AnimationNodeTimeSeek());
        graph.AddNode("to_seek", new AnimationNodeTimeSeek());
        graph.AddNode("mix", new AnimationNodeBlend2());
        graph.ConnectNode("from_seek", 0, "from");
        graph.ConnectNode("to_seek", 0, "to");
        graph.ConnectNode("mix", 0, "from_seek");
        graph.ConnectNode("mix", 1, "to_seek");
        graph.ConnectNode("output", 0, "mix");
        // Manual: the tree never advances by itself; Show seeks it to the logic's frame (ARCHITECTURE.md §9).
        Tree = new AnimationTree
        {
            TreeRoot = graph,
            CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual,
        };
    }

    /// <summary>The tree to add next to the model's skeleton; its clips are added to it as a library.</summary>
    public AnimationTree Tree { get; }

    /// <param name="weight">How much of <paramref name="to"/> shows: 1 for it alone, less while it fades in.</param>
    public void Show(StringName to, double toSeconds, StringName? from, double fromSeconds, float weight)
    {
        _to.Animation = to;
        _from.Animation = from ?? to;
        Tree.Set(_toSeek, toSeconds);
        Tree.Set(_fromSeek, from is null ? toSeconds : fromSeconds);
        Tree.Set(_blend, from is null ? 1f : weight);
        Tree.Advance(0);
    }
}
