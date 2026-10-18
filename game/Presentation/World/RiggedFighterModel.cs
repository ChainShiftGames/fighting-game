using System;
using System.Collections.Generic;
using Godot;
using Sim.Characters;
using Sim.Math;

namespace FightingGame.Presentation.World;

/// <summary>
/// A fighter drawn as its rigged model (ADR-0028): each displayed frame it seeks the move's clip to the time
/// <see cref="AnimationRetiming"/> gives for the logic's move frame, never advancing by elapsed time, and stands on the
/// logic position, mirrored for facing. A move without a clip shows idle, with one warning.
/// </summary>
public partial class RiggedFighterModel : Node3D, IFighterArt
{
    private const string IdleMoveId = "idle";

    private readonly HashSet<string> _warnedMoves = new(StringComparer.Ordinal);
    private readonly AnimationPlayer _player = new();
    private Dictionary<string, BoundClip> _clips = [];
    private StringName? _shownClip;
    private string _characterId = string.Empty;

    /// <summary>Puts the model in and gathers its move clips; once, before the first frame.</summary>
    /// <param name="model">The imported model scene (<c>.glb</c>; <c>.fbx</c> for test models).</param>
    public void Build(PackedScene model, FighterArtSettings settings, CharacterData character)
    {
        _characterId = character.Id;
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
        var library = new MoveClipLibrary(settings, character.Id, modelClips, RootBone(instance));
        _clips = library.AddTo(_player, character);
    }

    public void ShowPose(FighterPose pose, IntVector2 position, int facing, List<WorldBox> boxes)
    {
        // Mirroring, not turning, keeps the side facing the camera the same; the model faces +X.
        Transform = new Transform3D(Basis.FromScale(new Vector3(facing, 1f, 1f)), WorldScale.ToWorld(position));
        if (!_clips.TryGetValue(pose.MoveId, out BoundClip clip))
        {
            WarnMissingOnce(pose.MoveId);
            if (!_clips.TryGetValue(IdleMoveId, out BoundClip idle))
            {
                return;
            }

            clip = idle with { Timing = idle.Timing with { Contacts = null } };
        }

        if (_shownClip != clip.Name)
        {
            _player.Play(clip.Name);
            _player.Pause();
            _shownClip = clip.Name;
        }

        // updateOnly: method and audio tracks never fire; sounds come from the logic's events (ARCHITECTURE.md §9).
        _player.Seek(AnimationRetiming.SecondsAt(pose, clip.Timing), update: true, updateOnly: true);
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
