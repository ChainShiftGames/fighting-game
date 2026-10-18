using Godot;

namespace FightingGame.Presentation.World;

/// <summary>A move's clip in a fighter model's player, ready to seek: its full name and how to retime it.</summary>
public readonly record struct BoundClip(StringName Name, AnimationClipTiming Timing);
