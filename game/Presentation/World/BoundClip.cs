using Godot;

namespace FightingGame.Presentation.World;

/// <summary>A clip in a fighter model's mixer, ready to seek: its id, full name and how to retime it.</summary>
public readonly record struct BoundClip(int Id, StringName Name, AnimationClipTiming Timing);
