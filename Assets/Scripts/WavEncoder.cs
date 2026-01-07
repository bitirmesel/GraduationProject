// Assets/Scripts/WavEncoder.cs
using System;
using System.IO;
using UnityEngine;

public static class WavEncoder
{
    // clip: AudioClip
    // lengthSamplesPerChannel: kaç sample yazacağımız (Microphone.GetPosition ile kırpacağız)
    public static byte[] FromAudioClip(AudioClip clip, int lengthSamplesPerChannel)
    {
        if (clip == null) throw new ArgumentNullException(nameof(clip));
        if (lengthSamplesPerChannel <= 0) lengthSamplesPerChannel = clip.samples;

        int channels = clip.channels;
        int frequency = clip.frequency;

        // Clip’ten float sample’ları al
        // WavEncoder.cs içinde FromAudioClip metodunu şuna benzet:
        float[] samples = new float[lengthSamplesPerChannel * channels];
        clip.GetData(samples, 0); // lengthSamplesPerChannel kadar veriyi 0. indexten itibaren alır

        // float [-1,1] -> PCM16 short
        byte[] pcm16 = new byte[samples.Length * 2];
        int offset = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            short s = (short)Mathf.Clamp(samples[i] * short.MaxValue, short.MinValue, short.MaxValue);
            pcm16[offset++] = (byte)(s & 0xff);
            pcm16[offset++] = (byte)((s >> 8) & 0xff);
        }

        // WAV header yaz
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        int byteRate = frequency * channels * 2;
        int blockAlign = channels * 2;
        int subChunk2Size = pcm16.Length;
        int chunkSize = 36 + subChunk2Size;

        // RIFF
        bw.Write(new char[] { 'R', 'I', 'F', 'F' });
        bw.Write(chunkSize);
        bw.Write(new char[] { 'W', 'A', 'V', 'E' });

        // fmt
        bw.Write(new char[] { 'f', 'm', 't', ' ' });
        bw.Write(16);               // PCM
        bw.Write((short)1);         // audio format = 1 (PCM)
        bw.Write((short)channels);
        bw.Write(frequency);
        bw.Write(byteRate);
        bw.Write((short)blockAlign);
        bw.Write((short)16);        // bits per sample

        // data
        bw.Write(new char[] { 'd', 'a', 't', 'a' });
        bw.Write(subChunk2Size);
        bw.Write(pcm16);

        bw.Flush();
        return ms.ToArray();
    }
}
