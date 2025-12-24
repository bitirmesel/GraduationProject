using UnityEngine;
using System.IO;

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        using (var stream = new MemoryStream())
        {
            var writer = new BinaryWriter(stream);
            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            // --- WAV HEADER (Standart Başlık Yazımı) ---
            writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + samples.Length * 2);
            writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
            writer.Write(new char[4] { 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((ushort)1);
            writer.Write((ushort)clip.channels);
            writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2);
            writer.Write((ushort)(clip.channels * 2));
            writer.Write((ushort)16);
            writer.Write(new char[4] { 'd', 'a', 't', 'a' });
            writer.Write(samples.Length * 2);

            // --- SES VERİSİ (PCM Dönüştürme) ---
            foreach (var sample in samples)
            {
                writer.Write((short)(sample * 32767));
            }
            return stream.ToArray();
        }
    }
}