using UnityEngine;
using System.IO;
using System.Text;

public static class WavUtility
{
    // AudioClip'i byte dizisine (WAV dosyasına) çevirir
    public static byte[] FromAudioClip(AudioClip clip)
    {
        using (var stream = new MemoryStream())
        {
            var writer = new BinaryWriter(stream);

            // Ses verisini al
            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            // 16-bit dönüşümü (API'lerin beklediği format)
            short[] intData = new short[samples.Length];
            byte[] bytesData = new byte[samples.Length * 2];
            int rescaleFactor = 32767; // 16-bit max değer

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                byte[] byteArr = System.BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            // WAV Header Yazılıyor (44 byte)
            writer.Write(Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(36 + bytesData.Length);
            writer.Write(Encoding.UTF8.GetBytes("WAVE"));
            writer.Write(Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((ushort)1); // PCM Format
            writer.Write((ushort)clip.channels);
            writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2);
            writer.Write((ushort)(clip.channels * 2));
            writer.Write((ushort)16); // 16-bit
            writer.Write(Encoding.UTF8.GetBytes("data"));
            writer.Write(bytesData.Length);

            // Ses verisini yaz
            writer.Write(bytesData);

            return stream.ToArray();
        }
    }
}