using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Netcode.Drivers;
using Sim.Input;

namespace Netcode.Tests.GoldenReplays;

/// <summary>
/// A golden replay file (ADR-0006): the characters, the data hash, every frame's inputs and the expected state
/// checksums at checkpoints.
/// </summary>
/// <remarks>
/// Plain text, one statement per line; <c>#</c> starts a comment.
/// <code>
/// characters testman testman
/// data_hash 3f…           # SHA-256 of all game data when recorded
/// seed 1 frames 3600      # how the inputs were generated (only used to record a replay without inputs)
/// input 12 0008 0004      # run of 12 frames: P1's and P2's raw input words, hex
/// checksum 600 a1b2…      # StateChecksum of the match state after frame 600
/// </code>
/// </remarks>
internal sealed class GoldenReplay
{
    public string Player1 { get; private set; } = string.Empty;

    public string Player2 { get; private set; } = string.Empty;

    public string DataHash { get; set; } = string.Empty;

    public uint Seed { get; private set; }

    public int Frames { get; set; }

    /// <summary>The comment lines at the top of the file, kept when the replay is re-recorded.</summary>
    public List<string> Header { get; } = [];

    public List<FrameInputs> Inputs { get; } = [];

    public SortedDictionary<int, ulong> Checksums { get; } = [];

    /// <exception cref="FormatException">A line isn't a valid statement.</exception>
    public static GoldenReplay Read(string path)
    {
        var replay = new GoldenReplay();
        bool isHeader = true;
        int lineNumber = 0;
        foreach (string rawLine in File.ReadLines(path))
        {
            lineNumber++;
            if (isHeader && rawLine.StartsWith('#'))
            {
                replay.Header.Add(rawLine);
                continue;
            }

            isHeader = false;
            string line = StripComment(rawLine);
            if (line.Length == 0)
            {
                continue;
            }

            try
            {
                replay.ReadStatement(line.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            }
            catch (Exception exception) when (exception is FormatException or IndexOutOfRangeException)
            {
                throw new FormatException($"{path}:{lineNumber}: {exception.Message}", exception);
            }
        }

        return replay;
    }

    public void Write(string path)
    {
        var text = new StringBuilder();
        foreach (string line in Header)
        {
            text.Append(line).Append('\n');
        }

        text.Append(CultureInfo.InvariantCulture, $"characters {Player1} {Player2}\n");
        text.Append(CultureInfo.InvariantCulture, $"data_hash {DataHash}\n");
        text.Append(CultureInfo.InvariantCulture, $"seed {Seed} frames {Frames}\n");
        AppendInputRuns(text);
        foreach ((int frame, ulong checksum) in Checksums)
        {
            text.Append(CultureInfo.InvariantCulture, $"checksum {frame} {checksum:x16}\n");
        }

        File.WriteAllText(path, text.ToString());
    }

    private void ReadStatement(string[] words)
    {
        switch (words[0])
        {
            case "characters":
                Player1 = words[1];
                Player2 = words[2];
                break;
            case "data_hash":
                DataHash = words[1];
                break;
            case "seed":
                Seed = uint.Parse(words[1], CultureInfo.InvariantCulture);
                Frames = int.Parse(words[3], CultureInfo.InvariantCulture);
                break;
            case "input":
                AddRun(int.Parse(words[1], CultureInfo.InvariantCulture), new FrameInputs(Word(words[2]), Word(words[3])));
                break;
            case "checksum":
                Checksums[int.Parse(words[1], CultureInfo.InvariantCulture)] =
                    ulong.Parse(words[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                break;
            default:
                throw new FormatException($"unknown statement '{words[0]}'");
        }
    }

    private void AddRun(int frames, FrameInputs inputs)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            Inputs.Add(inputs);
        }
    }

    private void AppendInputRuns(StringBuilder text)
    {
        int start = 0;
        for (int frame = 1; frame <= Inputs.Count; frame++)
        {
            if (frame < Inputs.Count && Inputs[frame] == Inputs[start])
            {
                continue;
            }

            (InputWord player1, InputWord player2) = Inputs[start];
            text.Append(CultureInfo.InvariantCulture, $"input {frame - start} {player1.Bits:x4} {player2.Bits:x4}\n");
            start = frame;
        }
    }

    private static InputWord Word(string hex) =>
        new(ushort.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));

    private static string StripComment(string line)
    {
        int comment = line.IndexOf('#', StringComparison.Ordinal);
        return (comment < 0 ? line : line[..comment]).Trim();
    }
}
