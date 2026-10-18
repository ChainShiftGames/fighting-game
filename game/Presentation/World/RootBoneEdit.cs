using Godot;

namespace FightingGame.Presentation.World;

/// <summary>
/// Edits a clip copy's root bone track for clips not made to the asset spec (ADR-0028): takes out its horizontal
/// travel, or holds its height, because the logic already moves the fighter.
/// </summary>
public sealed class RootBoneEdit
{
    private readonly string _characterId;
    private readonly string? _rootBone;

    /// <param name="rootBone">The skeleton's root bone; null when the model has no skeleton.</param>
    public RootBoneEdit(string characterId, string? rootBone)
    {
        _characterId = characterId;
        _rootBone = rootBone;
    }

    public void RemoveTravel(Animation clip)
    {
        if (Track(clip) is not { } track)
        {
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

    /// <summary>Keeps the root bone at its height at <paramref name="seconds"/> for the whole clip.</summary>
    public void HoldHeight(Animation clip, double seconds)
    {
        if (Track(clip) is not { } track)
        {
            return;
        }

        float height = clip.PositionTrackInterpolate(track, seconds).Y;
        for (int key = 0; key < clip.TrackGetKeyCount(track); key++)
        {
            Vector3 value = (Vector3)clip.TrackGetKeyValue(track, key);
            clip.TrackSetKeyValue(track, key, value with { Y = height });
        }
    }

    // The root bone's position track, when it can be edited.
    private int? Track(Animation clip)
    {
        if (_rootBone is null)
        {
            GD.PushWarning($"{_characterId}: inPlace and holdHeight need a skeleton, and the model has none");
            return null;
        }

        for (int track = 0; track < clip.GetTrackCount(); track++)
        {
            if (clip.TrackGetType(track) != Animation.TrackType.Position3D ||
                clip.TrackGetPath(track).GetConcatenatedSubNames() != _rootBone)
            {
                continue;
            }

            if (!clip.TrackIsCompressed(track))
            {
                return track;
            }

            GD.PushWarning($"{_characterId}: can't edit a compressed clip's root; turn off its import compression");
            return null;
        }

        return null;
    }
}
