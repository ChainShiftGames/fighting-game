using System;
using System.Collections.Generic;
using Godot;

namespace FightingGame.Presentation.World;

/// <summary>
/// Gathers a character's move clips into one animation library on the model's player (ADR-0028): each from the model
/// itself or from the file its art file names, copied so loop and in-place settings never touch the imported files.
/// </summary>
public sealed class MoveClipLibrary
{
    private const string LibraryName = "moves";

    private readonly FighterArtSettings _settings;
    private readonly string _characterId;
    private readonly AnimationPlayer? _modelClips;
    private readonly string? _rootBone;
    private readonly Dictionary<string, AnimationPlayer?> _fileClips = new(StringComparer.Ordinal);
    private readonly List<Node> _loadedFiles = [];

    /// <param name="modelClips">The model's own clips; null when it has none.</param>
    /// <param name="rootBone">The skeleton's root bone, whose travel <c>inPlace</c> removes; null when unknown.</param>
    public MoveClipLibrary(FighterArtSettings settings, string characterId, AnimationPlayer? modelClips, string? rootBone)
    {
        _settings = settings;
        _characterId = characterId;
        _modelClips = modelClips;
        _rootBone = rootBone;
    }

    /// <summary>Adds every move and transition that has a clip to <paramref name="player"/>.</summary>
    public BoundClips AddTo(AnimationPlayer player, IReadOnlyList<string> moveIds)
    {
        var library = new AnimationLibrary();
        var moves = new Dictionary<string, BoundClip>(StringComparer.Ordinal);
        foreach (string moveId in moveIds)
        {
            AnimationSettings settings = _settings.For(moveId);
            // A move the art file doesn't name simply has no clip yet; the model warns once if it is used.
            bool named = _settings.Animations.ContainsKey(moveId);
            if (Find(settings.Clip ?? moveId, settings, $"move '{moveId}'", named) is { } source)
            {
                moves[moveId] = Add(library, moveId, Prepare(source, settings), settings.Contacts, $"move '{moveId}'");
            }
        }

        var transitions = new BoundClip?[_settings.Transitions.Count];
        for (int i = 0; i < transitions.Length; i++)
        {
            AnimationSettings settings = _settings.Transitions[i].Clip;
            string what = $"transitions[{i}]";
            if (Find(settings.Clip ?? string.Empty, settings, what, named: true) is { } source)
            {
                transitions[i] = Add(library, $"transition_{i}", Prepare(source, settings), null, what);
            }
        }

        player.AddAnimationLibrary(LibraryName, library);
        foreach (Node loaded in _loadedFiles)
        {
            loaded.Free();
        }

        return new BoundClips(moves, transitions);
    }

    private BoundClip Add(AnimationLibrary library, string name, Animation clip, ContactFrames? contacts, string what)
    {
        library.AddAnimation(name, clip);
        return new BoundClip(new StringName($"{LibraryName}/{name}"), Timing(what, clip, contacts));
    }

    /// <param name="named">The art file names this clip, so a missing one is an error rather than a gap.</param>
    private Animation? Find(string clip, AnimationSettings settings, string what, bool named)
    {
        AnimationPlayer? source = settings.File is { } file ? FileClips(file) : _modelClips;
        if (source is not null && source.HasAnimation(clip))
        {
            return source.GetAnimation(clip);
        }

        if (named)
        {
            string available = source is null ? "none" : string.Join(", ", source.GetAnimationList());
            GD.PushError($"{_characterId}: no clip '{clip}' for {what} (clips there: {available})");
        }

        return null;
    }

    private AnimationPlayer? FileClips(string file)
    {
        if (_fileClips.TryGetValue(file, out AnimationPlayer? cached))
        {
            return cached;
        }

        string path = FighterArtFile.Resolve(_characterId, file);
        AnimationPlayer? player = null;
        if (ResourceLoader.Exists(path) && ResourceLoader.Load(path) is PackedScene scene)
        {
            Node instance = scene.Instantiate();
            _loadedFiles.Add(instance);
            player = SceneSearch.First<AnimationPlayer>(instance);
        }

        if (player is null)
        {
            GD.PushError($"{_characterId}: {path} is not an imported model with clips");
        }

        _fileClips[file] = player;
        return player;
    }

    private Animation Prepare(Animation source, AnimationSettings settings)
    {
        var clip = (Animation)source.Duplicate();
        if (settings.Loop is { } loop)
        {
            clip.LoopMode = loop ? Animation.LoopModeEnum.Linear : Animation.LoopModeEnum.None;
        }

        if (settings.InPlace)
        {
            RemoveRootTravel(clip);
        }

        return clip;
    }

    private void RemoveRootTravel(Animation clip)
    {
        if (_rootBone is null)
        {
            GD.PushWarning($"{_characterId}: inPlace needs a skeleton, and the model has none");
            return;
        }

        for (int track = 0; track < clip.GetTrackCount(); track++)
        {
            if (clip.TrackGetType(track) != Animation.TrackType.Position3D ||
                clip.TrackGetPath(track).GetConcatenatedSubNames() != _rootBone)
            {
                continue;
            }

            if (clip.TrackIsCompressed(track))
            {
                GD.PushWarning($"{_characterId}: can't make a compressed clip in place; turn off its import compression");
                return;
            }

            int keys = clip.TrackGetKeyCount(track);
            Vector3 first = (Vector3)clip.TrackGetKeyValue(track, 0);
            Vector3 last = (Vector3)clip.TrackGetKeyValue(track, keys - 1);
            double end = clip.TrackGetKeyTime(track, keys - 1);
            for (int key = 0; key < keys; key++)
            {
                float progress = end > 0 ? (float)(clip.TrackGetKeyTime(track, key) / end) : 0f;
                Vector3 value = (Vector3)clip.TrackGetKeyValue(track, key);
                clip.TrackSetKeyValue(track, key, RootTravel.Removed(value, first, last, progress));
            }
        }
    }

    private AnimationClipTiming Timing(string what, Animation clip, ContactFrames? contacts)
    {
        float lengthFrames = (float)clip.Length * AnimationRetiming.FramesPerSecond;
        if (contacts is { } frames && frames.RecoveryFrom > lengthFrames)
        {
            GD.PushError(
                $"{_characterId}: {what} recoveryFrom {frames.RecoveryFrom} is past the end of its clip " +
                $"({lengthFrames:0.#} frames); playing it frame for frame");
            contacts = null;
        }

        return new AnimationClipTiming(lengthFrames, clip.LoopMode != Animation.LoopModeEnum.None, contacts);
    }
}
