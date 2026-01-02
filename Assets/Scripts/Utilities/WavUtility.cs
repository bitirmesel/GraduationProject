using System;
using System.IO;
using UnityEngine;

namespace GraduationProject.Utilities
{
    public static class WavUtility
    {
        // FluentMe/Google STT için en güvenli format:
        // 16kHz, Mono, PCM16 (LINEAR16), little-endian WAV
        public static byte[] FromAudioClip_16k_Mono_PCM16(AudioClip clip, int targetHz = 16000)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));

            // 1) Clip verisini al
            float[] src = new float[clip.samples * clip.channels];
            clip.GetData(src, 0);

            // 2) Mono'ya indir (downmix)
            float[] mono = DownmixToMono(src, clip.channels);

            // 3) 16kHz'e resample
            float[] resampled = ResampleLinear(mono, clip.frequency, targetHz);

            // 4) PCM16'ya çevir
            byte[] pcm16 = FloatToPCM16(resampled);

            // 5) WAV paketle
            return CreateWav(pcm16, channels: 1, sampleRate: targetHz);
        }

        public static byte[] FromAudioClip_Mono_PCM16_Wav(AudioClip clip)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));

            float[] src = new float[clip.samples * clip.channels];
            clip.GetData(src, 0);

            float[] mono = DownmixToMono(src, clip.channels);

            // RESAMPLE YOK: clip.frequency neyse onu kullan (senin durumda 44100)
            byte[] pcm16 = FloatToPCM16(mono);

            return CreateWav(pcm16, channels: 1, sampleRate: clip.frequency);
        }


        private static float[] DownmixToMono(float[] interleaved, int channels)
        {
            if (channels <= 1) return interleaved;

            int frames = interleaved.Length / channels;
            float[] mono = new float[frames];

            int idx = 0;
            for (int f = 0; f < frames; f++)
            {
                float sum = 0f;
                for (int c = 0; c < channels; c++)
                    sum += interleaved[idx++];

                mono[f] = sum / channels;
            }

            return mono;
        }

        // Basit lineer resampling (STT için yeterli)
        private static float[] ResampleLinear(float[] input, int srcHz, int dstHz)
        {
            if (srcHz == dstHz) return input;

            float ratio = (float)dstHz / srcHz;
            int outLen = Mathf.Max(1, Mathf.RoundToInt(input.Length * ratio));
            float[] output = new float[outLen];

            float step = (float)(input.Length - 1) / (outLen - 1);
            for (int i = 0; i < outLen; i++)
            {
                float pos = i * step;
                int i0 = (int)pos;
                int i1 = Mathf.Min(i0 + 1, input.Length - 1);
                float t = pos - i0;
                output[i] = Mathf.Lerp(input[i0], input[i1], t);
            }

            return output;
        }

        private static byte[] FloatToPCM16(float[] samples)
        {
            byte[] pcm16 = new byte[samples.Length * 2];
            int o = 0;

            for (int i = 0; i < samples.Length; i++)
            {
                float s = Mathf.Clamp(samples[i], -1f, 1f);
                short v = (short)Mathf.RoundToInt(s * short.MaxValue);

                // little-endian
                pcm16[o++] = (byte)(v & 0xFF);
                pcm16[o++] = (byte)((v >> 8) & 0xFF);
            }

            return pcm16;
        }

        private static byte[] CreateWav(byte[] audioData, int channels, int sampleRate)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                int byteRate = sampleRate * channels * 2; // 16-bit
                int blockAlign = channels * 2;

                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + audioData.Length);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

                bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);
                bw.Write((short)1); // PCM
                bw.Write((short)channels);
                bw.Write(sampleRate);
                bw.Write(byteRate);
                bw.Write((short)blockAlign);
                bw.Write((short)16);

                bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                bw.Write(audioData.Length);
                bw.Write(audioData);

                return ms.ToArray();
            }
        }
    }
}
