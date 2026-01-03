// Assets/Scripts/PronunciationDemo.cs
using UnityEngine;
using UnityEngine.Android;

public class PronunciationDemo : MonoBehaviour
{
    [Header("References")]
    public PronunciationApi api;   // Inspector’dan bağlayacağız

    [Header("Recording Settings")]
    public int sampleRate = 44100;
    public int maxSeconds = 6;

    private AudioClip _clip;
    private string _device;

    // UI'dan çağır: Start Recording
    public void StartRecording()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
#endif

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone device found!");
            return;
        }

        _device = Microphone.devices[0];
        _clip = Microphone.Start(_device, false, maxSeconds, sampleRate);

        Debug.Log("Recording started...");
    }

    // UI'dan çağır: Stop & Send
    public void StopAndSend(string text)
    {
        if (_clip == null)
        {
            Debug.LogError("No recording clip! StartRecording first.");
            return;
        }

        int pos = Microphone.GetPosition(_device); // kırpma için
        Microphone.End(_device);

        if (pos <= 0)
        {
            Debug.LogError("Recorded length is 0. Try again.");
            return;
        }

        byte[] wavBytes = WavEncoder.FromAudioClip(_clip, pos);

        // Backend’e gönder
        StartCoroutine(api.CheckPronunciation(text, wavBytes));
        Debug.Log("Sent to backend...");
    }
}
