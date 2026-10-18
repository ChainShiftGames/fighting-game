using System;
using System.Collections.Generic;
using Godot;
using Sim.Math;

namespace FightingGame.Presentation.World;

/// <summary>
/// A fighter drawn as its rigged model (ADR-0028): each displayed frame it seeks the move's clip to the time
/// <see cref="AnimationRetiming"/> gives for the logic's move frame, never advancing by elapsed time, and stands on the
/// logic position, mirrored for facing. A move without a clip shows idle, with one warning. Entering a move from
/// certain others first plays the art file's transition clip; looping clips of basic moves run on the match clock.
/// </summary>
public partial class RiggedFighterModel : Node3D, IFighterArt
{
    private const string IdleMoveId = "idle";

    private readonly HashSet<string> _warnedMoves = new(StringComparer.Ordinal);
    private readonly AnimationPlayer _player = new();
    private readonly MoveEntryTracker _entries = new();
    private BoundClips _clips = new([], []);
    private FighterArtSettings _settings = FighterArtSettings.Placeholder;
    private StringName? _shownClip;
    private string _characterId = string.Empty;

    /// <summary>Puts the model in and gathers its move clips; once, before the first frame.</summary>
    /// <param name="model">The imported model scene (<c>.glb</c>; <c>.fbx</c> for test models).</param>
    /// <param name="moveIds">The character's moves; each gets the clip its art file or its name points to.</param>
    public void Build(PackedScene model, FighterArtSettings settings, string characterId, IReadOnlyList<string> moveIds)
    {
        _characterId = characterId;
        _settings = settings;
        var pivot = new Node3D
        {
            Basis = new Basis(Vector3.Up, Mathf.DegToRad(settings.TurnDegrees)).Scaled(Vector3.One * settings.Scale),
        };
        Node instance = model.Instantiate();
        pivot.AddChild(instance);
        AddChild(pivot);

        // The model's own player only lends its clips; ours plays them, with track paths from the same scene root.
        AnimationPlayer? modelClips = SceneSearch.First<AnimationPlayer>(instance);
        if (modelClips is not null)
        {
            modelClips.Active = false;
        }

        instance.AddChild(_player);
        // Manual: the player never advances by itself; ShowPose seeks it to the logic's frame (ARCHITECTURE.md §9).
        _player.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual;
        var library = new MoveClipLibrary(settings, characterId, modelClips, RootBone(instance));
        _clips = library.AddTo(_player, moveIds);
    }

    public void ShowPose(FighterPose pose, IntVector2 position, int facing, List<WorldBox> boxes)
    {
        // Mirroring, not turning, keeps the side facing the camera the same; the model faces +X.
        Transform = new Transform3D(Basis.FromScale(new Vector3(facing, 1f, 1f)), WorldScale.ToWorld(position));
        if (pose.MatchFrame > 0)
        {
            _entries.Observe(pose.MoveId, pose.MatchFrame);
        }

        if (Transition(pose) is { } transition)
        {
            Seek(transition.Clip, transition.Seconds);
        }
        else if (MoveClip(pose.MoveId) is { } clip)
        {
            Seek(clip, AnimationRetiming.SecondsAt(Timed(pose, clip), clip.Timing));
        }
    }

    private BoundClip? MoveClip(string moveId)
    {
        if (_clips.Moves.TryGetValue(moveId, out BoundClip clip))
        {
            return clip;
        }

        WarnMissingOnce(moveId);
        return _clips.Moves.TryGetValue(IdleMoveId, out BoundClip idle)
            ? idle with { Timing = idle.Timing with { Contacts = null } }
            : null;
    }

    // A basic move's looping clip counts the frames since the move began, so it runs on through the move's own loop
    // (idle restarts every 60 frames, crouch repeats one frame) instead of jumping back to its start.
    private FighterPose Timed(FighterPose pose, BoundClip clip) =>
        pose.MatchFrame > 0 && pose.FirstActiveFrame == 0 && clip.Timing.Loops
            ? pose with { Frame = _entries.FramesInMove(pose.MatchFrame) }
            : pose;

    private (BoundClip Clip, double Seconds)? Transition(FighterPose pose)
    {
        if (pose.MatchFrame <= 0 || _entries.CameFrom is not { } from)
        {
            return null;
        }

        int index = _settings.TransitionBetween(from, pose.MoveId);
        int frame = _entries.FramesInMove(pose.MatchFrame);
        if (index < 0 || _clips.Transitions[index] is not { } clip || frame > _settings.Transitions[index].Frames)
        {
            return null;
        }

        ClipTransition transition = _settings.Transitions[index];
        float clipFrame = AnimationRetiming.TransitionFrameAt(
            frame, transition.Frames, clip.Timing.LengthFrames, transition.Reverse);
        return (clip, clipFrame / AnimationRetiming.FramesPerSecond);
    }

    private void Seek(BoundClip clip, double seconds)
    {
        if (_shownClip != clip.Name)
        {
            _player.Play(clip.Name);
            _player.Pause();
            _shownClip = clip.Name;
        }

        // updateOnly: method and audio tracks never fire; sounds come from the logic's events (ARCHITECTURE.md §9).
        _player.Seek(seconds, update: true, updateOnly: true);
    }

    private static string? RootBone(Node instance)
    {
        if (SceneSearch.First<Skeleton3D>(instance) is not { } skeleton)
        {
            return null;
        }

        int[] roots = skeleton.GetParentlessBones();
        return roots.Length > 0 ? skeleton.GetBoneName(roots[0]) : null;
    }

    private void WarnMissingOnce(string moveId)
    {
        if (_warnedMoves.Add(moveId))
        {
            GD.PushWarning($"{_characterId}: the model has no clip for move '{moveId}'; showing idle");
        }
    }
}
