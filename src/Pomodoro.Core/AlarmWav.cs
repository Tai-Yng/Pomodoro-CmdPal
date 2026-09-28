// Copyright (c) Tai-Yng. MIT license.

namespace Pomodoro.Core;

/// <summary>
/// Synthesizes the completion alarm as a WAV file containing exactly N chimes — the toast
/// audio loop attribute only supports "forever", so the repeat count is baked into the
/// audio itself (the toast plays the file once).
/// </summary>
public static class AlarmWav
{
    private const int SampleRate = 44100;

    /// <summary>16-bit mono PCM RIFF; each chime is a decaying dual sine followed by silence.</summary>
    public static byte[] Generate(int chimes)
    {
        chimes = Math.Clamp(chimes, 1, 5);
        var chimeSamples = (int)(0.35 * SampleRate);
        var gapSamples = (int)(0.55 * SampleRate);
        var totalSamples = chimes * chimeSamples + (chimes - 1) * gapSamples;

        var data = new byte[totalSamples * 2];
        var offset = 0;
        for (var chime = 0; chime < chimes; chime++)
        {
            for (var i = 0; i < chimeSamples; i++)
            {
                var t = (double)i / SampleRate;
                var envelope = Math.Exp(-t * 9);
                var sample = 0.55 * envelope
                    * (Math.Sin(2 * Math.PI * 880 * t) + 0.5 * Math.Sin(2 * Math.PI * 1318 * t));
                var value = (short)Math.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue);
                data[offset++] = (byte)(value & 0xFF);
                data[offset++] = (byte)((value >> 8) & 0xFF);
            }

            if (chime < chimes - 1)
            {
                offset += gapSamples * 2; // silence between chimes
            }
        }

        return BuildWav(data);
    }

    private static byte[] BuildWav(byte[] pcm)
    {
        var header = new byte[44];
        var dataSize = pcm.Length;
        WriteAscii(header, 0, "RIFF");
        WriteUInt32(header, 4, (uint)(36 + dataSize));
        WriteAscii(header, 8, "WAVE");
        WriteAscii(header, 12, "fmt ");
        WriteUInt32(header, 16, 16);
        WriteUInt16(header, 20, 1);                 // PCM
        WriteUInt16(header, 22, 1);                 // mono
        WriteUInt32(header, 24, SampleRate);
        WriteUInt32(header, 28, SampleRate * 2);    // byte rate
        WriteUInt16(header, 32, 2);                 // block align
        WriteUInt16(header, 34, 16);                // bits per sample
        WriteAscii(header, 36, "data");
        WriteUInt32(header, 40, (uint)dataSize);

        var wav = new byte[44 + dataSize];
        Buffer.BlockCopy(header, 0, wav, 0, 44);
        Buffer.BlockCopy(pcm, 0, wav, 44, dataSize);
        return wav;
    }

    private static void WriteAscii(byte[] buffer, int offset, string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            buffer[offset + i] = (byte)value[i];
        }
    }

    private static void WriteUInt16(byte[] buffer, int offset, ushort value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
    }

    private static void WriteUInt32(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }
}
