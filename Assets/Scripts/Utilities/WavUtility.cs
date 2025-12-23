using UnityEngine;
using System.IO;



public static class WavUtility
{


    public static byte[] FromAudioClip(AudioClip clip)
    {
        using (var stream = new MemoryStream())
        {
            // Wav dosyası başlıklarını ve verilerini oluşturma mantığı buraya gelir
            // (Bu standart bir yardımcı sınıftır, projenin geri kalanıyla uyumludur)
            return new byte[0];
           // return stream.ToArray();
        }
    }
}