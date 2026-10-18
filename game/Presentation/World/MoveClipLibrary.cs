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
    private readonly RootBoneEdit _rootEdit;
    private readonly Dictionary<string, AnimationPlayer?> _fileClips = new(StringComparer.Ordinal);
    private readonly List<Node> _loadedFiles = [];
    private int _nextId;

    /// <param name="modelClips">The model's own clips; null when it has none.</param>
    /// <param name="rootBone">The skeleton's root bone, whose travel <c>inPlace</c> removes; null when unknown.</param>
    public MoveClipLibrary(FighterArtSettings settings, string characterId, AnimationPlayer? modelClips, string? rootBone)
    {
        _settings = settings;
        _characterId = characterId;
        _modelClips = modelClips;
        _rootEdit = new RootBoneEdit(characterId, rootBone);
    }

    /// <summary>Adds every move and transition that has a clip to <paramref name="player"/>.</summary>
    public BoundClips AddTo(AnimationMixer mixer, IReadOnlyList<string> moveIds)
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
                moves[moveId] = Add(library, moveId, Prepare(source, settings), settings, $"move '{moveId}'");
            }
        }

        var transitions = new BoundClip?[_settings.Transitions.Count];
        for (int i = 0; i < transitions.Length; i++)
        {
            AnimationSettings settings = _settings.Transitions[i].Clip;
            string what = $"transitions[{i}]";
            if (Find(settings.Clip ?? string.Empty, settings, what, named: true) is { } source)
            {
                transitions[i] = Add(library, $"transition_{i}", Prepare(source, settings), settings, what);
            }
        }

        BoundClip? knockOut = null;
        if (_settings.KnockOut is { } knockOutSettings && knockOutSettings.Clip is { } name &&
            Find(name, knockOutSettings, "knockOut", named: true) is { } knockOutSource)
        {
            knockOut = Add(library, "knock_out", Prepare(knockOutSource, knockOutSettings), knockOutSettings, "knockOut");
        }

        mixer.AddAnimationLibrary(LibraryName, library);
        foreach (Node loaded in _loadedFiles)
        {
            loaded.Free();
        }

        return new BoundClips(moves, transitions, knockOut);
    }

    private BoundClip Add(AnimationLibrary library, string name, Animation clip, AnimationSettings settings, string what)
    {
        library.AddAnimation(name, clip);
        return new BoundClip(_nextId++, new StringName($"{LibraryName}/{name}"), Timing(what, clip, settings));
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
            _rootEdit.RemoveTravel(clip);
        }

        if (settings.HoldHeight)
        {
            _rootEdit.HoldHeight(clip, (settings.ClipFrom ?? 0) / AnimationRetiming.FramesPerSecond);
        }

        return clip;
    }

    private AnimationClipTiming Timing(string what, Animation clip, AnimationSettings settings)
    {
        float clipFrames = (float)clip.Length * AnimationRetiming.FramesPerSecond;
        float start = settings.ClipFrom ?? 0;
        float end = settings.ClipTo ?? clipFrames;
        if (end > clipFrames || start >= end)
        {
            GD.PushError($"{_characterId}: {what} clipFrom/clipTo {start}-{end} is outside its clip ({clipFrames:0.#} frames); playing all of it");
            (start, end) = (0f, clipFrames);
        }

        // Contact frames are written in clip frames, as an animator reads them; the retiming counts from the part's start.
        ContactFrames? contacts = settings.Contacts is { } frames
            ? new ContactFrames(frames.ActiveFrom - (int)start, frames.RecoveryFrom - (int)start)
            : null;
        if (settings.Contacts is { } written && (written.ActiveFrom < start || written.RecoveryFrom > end))
        {
            GD.PushError(
                $"{_characterId}: {what} contact frames {written.ActiveFrom}-{written.RecoveryFrom} are outside the " +
                $"part played ({start:0.#}-{end:0.#}); playing it frame for frame");
            contacts = null;
        }

        return new AnimationClipTiming(
            end - start,
            clip.LoopMode != Animation.LoopModeEnum.None,
            contacts,
            start,
            settings.Fit,
            settings.HoldFrames ?? _settings.HoldFrames,
            settings.Speed);
    }
}
