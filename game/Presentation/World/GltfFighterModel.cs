using System;
using System.Collections.Generic;
using Godot;
using Sim.Characters;
using Sim.Math;

namespace FightingGame.Presentation.World;

/// <summary>
/// A fighter drawn as its rigged glTF model (ADR-0028): each displayed frame it seeks the clip named after the move to
/// the time <see cref="AnimationRetiming"/> gives for the logic's move frame, never advancing by elapsed time, and
/// stands on the logic position, mirrored for facing. A move without a clip shows idle, with one warning.
/// </summary>
public partial class GltfFighterModel : Node3D, IFighterArt
{
    private const string IdleMoveId = "idle";

    private readonly Dictionary<string, BoundClip> _clips = new(StringComparer.Ordinal);
    private readonly HashSet<string> _warnedMoves = new(StringComparer.Ordinal);
    private AnimationPlayer? _player;
    private StringName? _shownClip;
    private string _characterId = string.Empty;

    /// <summary>Puts the model in and matches its clips to the character's moves; once, before the first frame.</summary>
    /// <param name="model">The imported <c>.glb</c> scene.</param>
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

        _player = FindPlayer(instance);
        if (_player is null)
        {
            GD.PushWarning($"{character.Id}: the model has no AnimationPlayer, so it stays in its rest pose");
            return;
        }

        // Manual: the player never advances by itself; ShowPose seeks it to the logic's frame (ARCHITECTURE.md §9).
        _player.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual;
        foreach (string moveId in character.MoveIds)
        {
            Bind(moveId, settings);
        }
    }

    public void ShowPose(FighterPose pose, IntVector2 position, int facing, List<WorldBox> boxes)
    {
        // Mirroring, not turning, keeps the side facing the camera the same; the model faces +X.
        Transform = new Transform3D(Basis.FromScale(new Vector3(facing, 1f, 1f)), WorldScale.ToWorld(position));
        if (_player is null)
        {
            return;
        }

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

    private static AnimationPlayer? FindPlayer(Node root)
    {
        if (root is AnimationPlayer player)
        {
            return player;
        }

        foreach (Node child in root.GetChildren())
        {
            if (FindPlayer(child) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private void Bind(string moveId, FighterArtSettings settings)
    {
        string clipName = settings.ClipFor(moveId);
        if (!_player!.HasAnimation(clipName))
        {
            return;
        }

        Animation animation = _player.GetAnimation(clipName);
        float lengthFrames = (float)animation.Length * AnimationRetiming.FramesPerSecond;
        ContactFrames? contacts = settings.ContactsFor(moveId);
        if (contacts is { } frames && frames.RecoveryFrom > lengthFrames)
        {
            GD.PushError(
                $"{_characterId}: {moveId} recoveryFrom {frames.RecoveryFrom} is past the end of clip " +
                $"'{clipName}' ({lengthFrames:0.#} frames); playing it frame for frame");
            contacts = null;
        }

        bool loops = animation.LoopMode != Animation.LoopModeEnum.None;
        _clips[moveId] = new BoundClip(new StringName(clipName), new AnimationClipTiming(lengthFrames, loops, contacts));
    }

    private void WarnMissingOnce(string moveId)
    {
        if (_warnedMoves.Add(moveId))
        {
            GD.PushWarning($"{_characterId}: the model has no clip for move '{moveId}'; showing idle");
        }
    }

    private readonly record struct BoundClip(StringName Name, AnimationClipTiming Timing);
}
