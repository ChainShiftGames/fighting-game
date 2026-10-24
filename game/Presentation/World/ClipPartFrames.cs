using System.Collections.Generic;

namespace FightingGame.Presentation.World;

/// <summary>
/// Moves a move's contact frames and timing keys, written in clip frames as an animator reads them, to count from the
/// start of the part of the clip played (<c>clipFrom</c>), as the retiming does (ADR-0028).
/// </summary>
public static class ClipPartFrames
{
    /// <param name="start">Where the part played starts, in clip frames.</param>
    /// <param name="end">Where it ends.</param>
    /// <param name="problem">
    /// Why something was left out: contact frames outside the part drop the keys too (the clip plays frame for frame),
    /// keys outside it drop only the keys. Empty when everything fits.
    /// </param>
    public static AnimationClipTiming Fit(
        AnimationClipTiming timing, ContactFrames? contacts, IReadOnlyList<TimingKey>? keys, out string problem)
    {
        problem = string.Empty;
        float start = timing.StartFrame;
        float end = start + timing.LengthFrames;
        if (contacts is not { } written)
        {
            return timing with { Contacts = null, Keys = null };
        }

        if (written.ActiveFrom < start || written.RecoveryFrom > end)
        {
            problem = $"contact frames {written.ActiveFrom}-{written.RecoveryFrom} are outside the part played " +
                $"({start:0.#}-{end:0.#}); playing it frame for frame";
            return timing with { Contacts = null, Keys = null };
        }

        var fromStart = new ContactFrames(written.ActiveFrom - (int)start, written.RecoveryFrom - (int)start);
        if (keys is not { Count: > 0 })
        {
            return timing with { Contacts = fromStart, Keys = null };
        }

        var moved = new TimingKey[keys.Count];
        for (int i = 0; i < keys.Count; i++)
        {
            TimingKey key = keys[i];
            if (key.Clip < start || key.Clip > end)
            {
                problem = $"timing key {i} (clip frame {key.Clip}) is outside the part played ({start:0.#}-{end:0.#}); " +
                    "leaving the keys out";
                return timing with { Contacts = fromStart, Keys = null };
            }

            moved[i] = key with { Clip = key.Clip - (int)start };
        }

        return timing with { Contacts = fromStart, Keys = moved };
    }
}
