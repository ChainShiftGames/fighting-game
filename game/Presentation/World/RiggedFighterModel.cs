using System;
using System.Collections.Generic;
using Godot;
using Sim.Math;

namespace FightingGame.Presentation.World;

/// <summary>
/// A fighter drawn as its rigged model (ADR-0028): each displayed frame it seeks the move's clip to the time
/// <see cref="AnimationRetiming"/> gives for the logic's move frame, never advancing by elapsed time, and stands on the
/// logic position, mirrored for facing. A move without a clip shows idle, with one warning. Hit and block reactions
/// count from each hit and snap in. On top, by match frames: transition clips into listed moves, a crossfade into other
/// basic moves and a turn when the facing switches.
/// </summary>
public partial class RiggedFighterModel : Node3D, IFighterArt
{
    private const string IdleMoveId = "idle";
    private const string KnockedOutState = "knocked_out";
    private const string StandingState = "standing";

    private readonly HashSet<string> _warnedMoves = new(StringComparer.Ordinal);
    private readonly ClipMixer _mixer = new();
    private readonly MoveEntryTracker _entries = new();
    private readonly MoveEntryTracker _knockOut = new();
    private readonly HitReactionClock _reaction = new();
    private readonly PoseFade _fade = new();
    private readonly FacingTurn _turn = new();
    private readonly Node3D _pivot = new();
    private BoundClips _clips = new([], []);
    private StringName[] _clipNames = [];
    private FighterArtSettings _settings = FighterArtSettings.Placeholder;
    private string _characterId = string.Empty;

    /// <summary>Puts the model in and gathers its move clips; once, before the first frame.</summary>
    /// <param name="model">The imported model scene (<c>.glb</c>; <c>.fbx</c> for test models).</param>
    /// <param name="moveIds">The character's moves; each gets the clip its art file or its name points to.</param>
    public void Build(PackedScene model, FighterArtSettings settings, string characterId, IReadOnlyList<string> moveIds)
    {
        _characterId = characterId;
        _settings = settings;
        var adapter = new Node3D
        {
            Basis = new Basis(Vector3.Up, Mathf.DegToRad(settings.TurnDegrees)).Scaled(Vector3.One * settings.Scale),
        };
        Node instance = model.Instantiate();
        adapter.AddChild(instance);
        _pivot.AddChild(adapter);
        AddChild(_pivot);

        // The model's own player only lends its clips; the mixer plays them, with track paths from the same root.
        AnimationPlayer? modelClips = SceneSearch.First<AnimationPlayer>(instance);
        if (modelClips is not null)
        {
            modelClips.Active = false;
        }

        instance.AddChild(_mixer.Tree);
        var library = new MoveClipLibrary(settings, characterId, modelClips, RootBone(instance));
        _clips = library.AddTo(_mixer.Tree, moveIds);
        _clipNames = NamesById(_clips);
        _mixer.Tree.Active = true;
    }

    public bool HasLimbs => true;

    /// <summary>How a move's clip is retimed now; null when the move has no clip.</summary>
    public AnimationClipTiming? TimingOf(string moveId) =>
        _clips.Moves.TryGetValue(moveId, out BoundClip clip) ? clip.Timing : null;

    /// <summary>
    /// Retimes a move's clip with contact frames and keys as edited, written in clip frames, without reloading the
    /// model: the character editor's animation track. Nothing when the move has no clip.
    /// </summary>
    /// <returns>Why some of the timing was left out; empty when all of it applies.</returns>
    public string Retime(string moveId, ContactFrames? contacts, IReadOnlyList<TimingKey> keys)
    {
        if (!_clips.Moves.TryGetValue(moveId, out BoundClip clip))
        {
            return string.Empty;
        }

        _clips.Moves[moveId] = clip with { Timing = ClipPartFrames.Fit(clip.Timing, contacts, keys, out string problem) };
        return problem;
    }

    public void ShowPose(FighterPose pose, IntVector2 position, int facing, List<WorldBox> boxes)
    {
        Position = WorldScale.ToWorld(position);
        ShowFacing(facing, pose.MatchFrame);
        if (pose.MatchFrame > 0)
        {
            _entries.Observe(pose.MoveId, pose.MatchFrame);
            _reaction.Observe(in pose);
        }

        (BoundClip Clip, double Seconds)? transition = KnockedOut(pose) ?? Transition(pose);
        bool isTransition = transition is not null;
        BoundClip? shown = transition?.Clip ?? MoveClip(pose.MoveId);
        if (shown is not { } clip)
        {
            return;
        }

        double seconds = transition?.Seconds ?? AnimationRetiming.SecondsAt(Timed(pose, clip), clip.Timing);
        int crossfade = _settings.CrossfadeFramesFor(pose.MoveId);
        bool fadesIn = !isTransition && crossfade > 0 && PoseFade.MayFadeInto(in pose);
        if (pose.MatchFrame <= 0)
        {
            _mixer.Show(clip.Name, seconds, null, 0, 1f);
            return;
        }

        _fade.Observe(clip.Id, seconds, fadesIn, pose.MatchFrame);
        StringName? from = _fade.FromClip >= 0 ? _clipNames[_fade.FromClip] : null;
        _mixer.Show(clip.Name, seconds, from, _fade.FromSeconds, _fade.Weight(pose.MatchFrame, crossfade));
    }

    // Mirroring, not turning, keeps the side facing the camera the same; the model faces +X, cheated toward the
    // camera. While the facing switches it turns through facing the camera, where the mirrored pose looks the same.
    private void ShowFacing(int facing, int matchFrame)
    {
        int side = facing;
        float yaw = -_settings.CameraTurnDegrees;
        if (matchFrame > 0)
        {
            _turn.Observe(facing, matchFrame);
            (side, yaw) = _turn.PoseAt(matchFrame, _settings.TurnFrames, _settings.CameraTurnDegrees);
        }

        Basis = Basis.FromScale(new Vector3(side, 1f, 1f));
        _pivot.Basis = new Basis(Vector3.Up, Mathf.DegToRad(yaw));
    }

    private BoundClip? MoveClip(string moveId)
    {
        if (_clips.Moves.TryGetValue(moveId, out BoundClip clip))
        {
            return clip;
        }

        WarnMissingOnce(moveId);
        return _clips.Moves.TryGetValue(IdleMoveId, out BoundClip idle)
            ? idle with { Timing = idle.Timing with { Contacts = null, Fit = false } }
            : null;
    }

    // A hit or block reaction counts from the hit that started it, by the fighter's own frames, so it holds in hitstop
    // and starts over on every hit of a combo. Other basic moves count the frames since they began by the match clock,
    // so a looping clip runs on through the move's own loop (idle restarts every 60 frames) and a one-frame move
    // repeated (crouch, jump) still animates.
    private FighterPose Timed(FighterPose pose, BoundClip clip)
    {
        if (pose.MatchFrame <= 0 || pose.FirstActiveFrame != 0)
        {
            return pose;
        }

        if (HitReactionActions.Includes(pose.Action))
        {
            return pose with { Frame = _reaction.FrameOf(in pose) };
        }

        return clip.Timing.Fit ? pose : pose with { Frame = _entries.FramesInMove(pose.MatchFrame) };
    }

    // A KO plays the knockout clip from the frame it happened, whatever the logic's moves do meanwhile.
    private (BoundClip Clip, double Seconds)? KnockedOut(FighterPose pose)
    {
        if (pose.MatchFrame <= 0 || _clips.KnockOut is not { } clip)
        {
            return null;
        }

        _knockOut.Observe(pose.IsKnockedOut ? KnockedOutState : StandingState, pose.MatchFrame);
        if (!pose.IsKnockedOut)
        {
            return null;
        }

        var frame = new FighterPose(pose.Action, pose.MoveId, _knockOut.FramesInMove(pose.MatchFrame), 0, pose.Phase);
        return (clip, AnimationRetiming.SecondsAt(frame, clip.Timing));
    }

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
        float clipFrame = clip.Timing.StartFrame + AnimationRetiming.TransitionFrameAt(
            frame, transition.Frames, clip.Timing.LengthFrames, transition.Reverse);
        return (clip, clipFrame / AnimationRetiming.FramesPerSecond);
    }

    private static StringName[] NamesById(BoundClips clips)
    {
        var names = new StringName[clips.Moves.Count + clips.Transitions.Length + 1];
        if (clips.KnockOut is { } knockOut)
        {
            names[knockOut.Id] = knockOut.Name;
        }

        foreach (BoundClip clip in clips.Moves.Values)
        {
            names[clip.Id] = clip.Name;
        }

        foreach (BoundClip? transition in clips.Transitions)
        {
            if (transition is { } clip)
            {
                names[clip.Id] = clip.Name;
            }
        }

        return names;
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
