// Assets/Scripts/PronunciationApi.cs
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class PronunciationApi : MonoBehaviour
{
    [SerializeField] private string baseUrl = "https://<render-domainin>";

    public IEnumerator CheckPronunciation(string text, byte[] wavBytes)
    {
        var form = new WWWForm();
        form.AddField("text", text);
        form.AddBinaryData("audio_file", wavBytes, "recording.wav", "audio/wav");

        using var req = UnityWebRequest.Post($"{baseUrl}/api/pronunciation/check", form);
        req.timeout = 60;

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Pronunciation error: {req.responseCode} - {req.error}\n{req.downloadHandler.text}");
            yield break;
        }

        Debug.Log("Score JSON: " + req.downloadHandler.text);
    }
}
