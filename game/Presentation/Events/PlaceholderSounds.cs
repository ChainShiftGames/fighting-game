using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using Godot;
using Sim.Events;

namespace FightingGame.Presentation.Events;

/// <summary>
/// Placeholder sound effects: a short generated tone per event kind, stopped if a rollback removes its event.
/// </summary>
/// <remarks>
/// Lives in the scene tree (the host adds it) because Godot only plays audio from there. Final sounds replace the
/// generated tones; the voices and the play/cancel path stay the same.
/// </remarks>
public partial class PlaceholderSounds : Node, IFrameEventEffect
{
    // Developer settings (presentation): voices that can sound at once, tone output rate and volume (0 to 1).
    private const int VoiceCount = 8;
    private const int MixRate = 22050;
    private const float Volume = 0.25f;

    private const int MillisecondsPerSecond = 1000;

    private readonly Dictionary<FrameEventKind, AudioStreamWav> _tones = [];
    private readonly AudioStreamPlayer[] _players = new AudioStreamPlayer[VoiceCount];
    private readonly SoundVoices _voices = new(VoiceCount);

    public PlaceholderSounds()
    {
        for (int voice = 0; voice < VoiceCount; voice++)
        {
            _players[voice] = new AudioStreamPlayer();
            AddChild(_players[voice]);
        }

        foreach (FrameEventKind kind in Enum.GetValues<FrameEventKind>())
        {
            if (ToneOf(kind) is (int frequency, int milliseconds))
            {
                _tones[kind] = Tone(frequency, milliseconds);
            }
        }
    }

    public void Play(in FrameEvent frameEvent)
    {
        if (!IsInsideTree() || !_tones.TryGetValue(frameEvent.Kind, out AudioStreamWav? tone))
        {
            return;
        }

        AudioStreamPlayer player = _players[_voices.Assign(frameEvent.Id)];
        player.Stream = tone;
        player.Play();
    }

    public void Cancel(in FrameEvent frameEvent)
    {
        int voice = _voices.Release(frameEvent.Id);
        if (voice != SoundVoices.NoVoice)
        {
            _players[voice].Stop();
        }
    }

    // Developer settings (presentation): placeholder pitch in Hz and length in milliseconds per kind; null = silent.
    private static (int Frequency, int Milliseconds)? ToneOf(FrameEventKind kind) => kind switch
    {
        FrameEventKind.Hit or FrameEventKind.ProjectileHit => (220, 90),
        FrameEventKind.Block or FrameEventKind.ProjectileBlock => (520, 60),
        FrameEventKind.Swing => (880, 35),
        FrameEventKind.Clash or FrameEventKind.ProjectileClash => (1200, 70),
        FrameEventKind.CounterHit or FrameEventKind.PunishCounter => (150, 140),
        FrameEventKind.ThrowHit => (180, 120),
        FrameEventKind.ThrowTech => (700, 80),
        FrameEventKind.KnockOut => (110, 350),
        _ => null,
    };

    // A square wave that fades out linearly: 16-bit mono PCM, little-endian.
    private static AudioStreamWav Tone(int frequency, int milliseconds)
    {
        int samples = MixRate * milliseconds / MillisecondsPerSecond;
        int samplesPerHalfWave = Math.Max(1, MixRate / frequency / 2);
        byte[] data = new byte[samples * sizeof(short)];
        for (int i = 0; i < samples; i++)
        {
            float fade = 1f - ((float)i / samples);
            int sign = i / samplesPerHalfWave % 2 == 0 ? 1 : -1;
            short value = (short)(sign * fade * Volume * short.MaxValue);
            BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(i * sizeof(short)), value);
        }

        return new AudioStreamWav { Data = data, Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = MixRate };
    }
}
