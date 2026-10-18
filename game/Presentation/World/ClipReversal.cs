using System.Collections.Generic;
using Godot;

namespace FightingGame.Presentation.World;

/// <summary>
/// Turns a clip copy round so it plays backwards (art file <c>reverse</c>, ADR-0028): every key moves to the clip's
/// length minus its time, so the retiming and loop settings work on it unchanged.
/// </summary>
public static class ClipReversal
{
    public static void Reverse(Animation clip)
    {
        double length = clip.Length;
        for (int track = 0; track < clip.GetTrackCount(); track++)
        {
            var keys = new List<(double Time, Variant Value, float Transition)>(clip.TrackGetKeyCount(track));
            for (int key = 0; key < clip.TrackGetKeyCount(track); key++)
            {
                keys.Add((clip.TrackGetKeyTime(track, key), clip.TrackGetKeyValue(track, key),
                    clip.TrackGetKeyTransition(track, key)));
            }

            for (int key = keys.Count - 1; key >= 0; key--)
            {
                clip.TrackRemoveKey(track, key);
            }

            foreach ((double time, Variant value, float transition) in keys)
            {
                clip.TrackInsertKey(track, length - time, value, transition);
            }
        }
    }
}
