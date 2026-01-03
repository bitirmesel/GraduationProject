// Assets/Scripts/Managers/PronunciationManager.cs

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine.Android;
using GraduationProject.Models;
using GraduationProject.Utilities;
using Newtonsoft.Json;
using TMPro;

namespace GraduationProject.Managers
{
    public class PronunciationManager : MonoBehaviour
    {
        public static PronunciationManager Instance;

        [SerializeField] private PronunciationApi api; // Inspector’dan bağlayacağız

        public void Send(byte[] wavBytes, string text)
        {
            StartCoroutine(api.CheckPronunciation(text, wavBytes));
        }

        [SerializeField] private PronunciationApi pronunciationApi;

        private void SendForScoring(string text, byte[] wavBytes)
        {
            StartCoroutine(pronunciationApi.CheckPronunciation(text, wavBytes));
        }


        [Header("Backend Ayarları")]
        // ESKİ VE DOĞRU ADRES:
        [SerializeField] private string backendUrl = "https://backendapi-8nfn.onrender.com/api/pronunciation/check";

        [Header("UI & Görsel Referanslar")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private RectTransform focusPosition;
        [SerializeField] private Transform cardParent;
        [SerializeField] private GameObject cardPrefab;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = true;

        private AudioClip _recordingClip;
        private string _microphoneDevice;
        private bool _isRecording;

        private List<AssetItem> _levelAssets = new List<AssetItem>();
        private TaskCompletionSource<string> _currentApiTask;
        private string _activeTargetWord;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CacheMicrophoneDevice();
        }

        private void Start()
        {
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
#endif
            CacheMicrophoneDevice();
        }

        // =======================
        // UI & HELPERS
        // =======================

        private void EnsureUIRefs()
        {
            if (statusText == null) statusText = FindSceneObjectByName<TMP_Text>("StatusText");
            if (focusPosition == null) focusPosition = FindSceneObjectByName<RectTransform>("FocusPosition");
            if (cardParent == null)
            {
                var holder = FindSceneObjectByName<RectTransform>("CardHolder");
                if (holder != null) cardParent = holder.transform;
            }
            if (cardParent == null && focusPosition != null) cardParent = focusPosition.parent;
        }

        private T FindSceneObjectByName<T>(string targetName) where T : UnityEngine.Object
        {
            var all = Resources.FindObjectsOfTypeAll<T>();
            foreach (var obj in all)
            {
                if (obj == null) continue;
                if (obj.name != targetName) continue;
                if (obj is Component c && c.gameObject.scene.IsValid() && c.gameObject.scene.isLoaded) return obj;
            }
            return null;
        }

        // =======================
        // FLOW
        // =======================

        public void StartPronunciationSession(List<AssetItem> levelData)
        {
            if (levelData == null || levelData.Count == 0) return;
            if (UIPanelManager.Instance != null) UIPanelManager.Instance.ShowPronunciationPanel(true);
            EnsureUIRefs();
            _levelAssets = new List<AssetItem>(levelData);
            GameObject.Find("MemoryRoot")?.SetActive(false);
            StartSequence();
        }

        public async void StartSequence()
        {
            EnsureUIRefs();
            if (_levelAssets == null || _levelAssets.Count == 0)
            {
                UpdateUI("Veri yok!", "");
                return;
            }

            foreach (var asset in _levelAssets)
            {
                _activeTargetWord = asset.Key;
                GameObject activeCard = CreatePronunciationCard(asset);

                if (activeCard != null && focusPosition != null)
                {
                    activeCard.transform.position = focusPosition.position;
                    activeCard.transform.localScale = Vector3.one * 2.5f;
                }

                if (AudioManager.Instance != null) await AudioManager.Instance.PlayVoiceOverAsync(asset.Key);

                bool kelimeDogruMu = false;
                while (!kelimeDogruMu)
                {
                    UpdateUI($"{asset.Key} demen bekleniyor...", "");
                    _currentApiTask = new TaskCompletionSource<string>();
                    string sonucJson = await _currentApiTask.Task;

                    if (string.IsNullOrEmpty(sonucJson))
                    {
                        UpdateUI("Bağlantı hatası/Zaman aşımı.", "");
                        continue;
                    }

                    try
                    {
                        // JSON Parse
                        var response = JsonConvert.DeserializeObject<PronunciationResponseDto>(sonucJson);
                        if (response?.Score == null)
                        {
                            UpdateUI("Sonuç okunamadı.", "");
                            continue;
                        }

                        double puan = response.Score.OverallPoints;
                        Debug.Log($"[PUAN] Gelen Puan: {puan}");

                        if (puan >= 70)
                        {
                            kelimeDogruMu = true;
                            UpdateUI($"Harika! Puanın: {puan:F0}", "");
                            if (AudioManager.Instance != null) AudioManager.Instance.PlayEffect("CorrectSound");
                            await Task.Delay(800);
                            if (activeCard != null) Destroy(activeCard);
                        }
                        else
                        {
                            UpdateUI($"Puanın: {puan:F0}. Tekrar dene!", "");
                            if (AudioManager.Instance != null) await AudioManager.Instance.PlayVoiceOverAsync(asset.Key);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("JSON Parse Hatası: " + ex.Message);
                        UpdateUI("Hata oluştu.", "");
                    }
                }
            }
            UpdateUI("Tüm kelimeler bitti!", "");
            if (UIPanelManager.Instance != null) UIPanelManager.Instance.ShowVictoryPanel(true);
        }

        private GameObject CreatePronunciationCard(AssetItem asset)
        {
            EnsureUIRefs();
            if (focusPosition == null || cardPrefab == null) return null;
            Transform parent = cardParent != null ? cardParent : focusPosition.parent;
            GameObject cardObj = Instantiate(cardPrefab, parent);
            MemoryCard cardScript = cardObj.GetComponent<MemoryCard>();
            if (cardScript != null)
            {
                string fullUrl = APIManager.Instance.GetBaseUrl() + asset.File;
                StartCoroutine(LoadCardSprite(cardScript, fullUrl, asset.File));
            }
            return cardObj;
        }

        private IEnumerator LoadCardSprite(MemoryCard card, string url, string fileName)
        {
            var task = AssetLoader.Instance.GetSpriteAsync(url, fileName);
            yield return new WaitUntil(() => task.IsCompleted);
            Sprite loadedSprite = task.Result;
            if (loadedSprite != null)
            {
                card.Setup(url.GetHashCode(), loadedSprite, null, null);
                card.FlipOpen();
            }
        }

        // =======================
        // MIC & RECORDING
        // =======================

        private void CacheMicrophoneDevice()
        {
            if (Microphone.devices.Length > 0) _microphoneDevice = Microphone.devices[0];
        }

        public void StartRecording()
        {
            Debug.Log("[MIC] StartRecording...");
            if (_isRecording) return;
            if (string.IsNullOrEmpty(_microphoneDevice)) CacheMicrophoneDevice();
            if (string.IsNullOrEmpty(_microphoneDevice)) return;

            // ÖNEMLİ: API için 16000 Hz kayıt alıyoruz (Google Standardı)
            _recordingClip = Microphone.Start(_microphoneDevice, false, 10, 16000);
            _isRecording = true;
            UpdateUI("Kaydediyor... Konuş!", "");
        }

        public void StopRecording()
        {
            Debug.Log("[MIC] StopRecording...");
            if (!_isRecording) return;

            int samplePos = Microphone.GetPosition(_microphoneDevice);
            Microphone.End(_microphoneDevice);
            _isRecording = false;

            if (samplePos <= 0)
            {
                Debug.LogError("Boş kayıt!");
                _currentApiTask?.TrySetResult(null);
                return;
            }

            int channels = _recordingClip.channels;
            int srcHz = _recordingClip.frequency;

            // full buffer (interleaved)
            float[] full = new float[_recordingClip.samples * channels];
            _recordingClip.GetData(full, 0);

            // samplePos "per-channel frame" sayısıdır -> toplam float sayısı = samplePos * channels
            int validFrames = Mathf.Min(samplePos, _recordingClip.samples);
            int validCount = validFrames * channels;

            float[] trimmed = new float[validCount];
            Array.Copy(full, trimmed, validCount);

            Debug.Log($"[MIC INFO] Hz: {srcHz} | Channels: {channels} | Frames: {validFrames} | Floats: {validCount}");

            // 1) Mono'ya indir
            float[] mono = (channels == 1) ? trimmed : DownmixToMono(trimmed, channels);

            // 2) 16k'ya resample et (backend/stt tarafını stabilize eder)
            const int targetHz = 16000;
            float[] mono16k = (srcHz == targetHz) ? mono : ResampleLinear(mono, srcHz, targetHz);

            float durationSec = mono16k.Length / (float)targetHz;
            Debug.Log($"[AUDIO FINAL] Hz: {targetHz} | Channels: 1 | Samples: {mono16k.Length} | Duration: {durationSec:F2}s");

            UpdateUI("Analiz ediliyor...", "");

            StartCoroutine(SendAudioToBackend(mono16k, targetHz, _activeTargetWord, (json) =>
            {
                _currentApiTask?.TrySetResult(json);
            }));
        }


        // =======================
        // API (WWWFORM + CORRECT WAV)
        // =======================

        // DİKKAT: Parametre türü değişti (AudioClip -> float[])
        // Parametreye 'frequency' eklendi 👇
        private IEnumerator SendAudioToBackend(float[] soundData, int frequency, string textReference, Action<string> callback)
        {
            // 1. WAV Bytes oluştur (Gerçek frekansı kullanıyoruz)
            byte[] wavData = EncodeAsWAV(soundData, frequency, 1);

            Debug.Log($"[WAV SIZE] {wavData.Length} bytes | Freq: {frequency}");

            // 2. Form oluştur
            WWWForm form = new WWWForm();
            form.AddBinaryData("audioFile", wavData, "recording.wav", "audio/wav");
            form.AddField("text", textReference);

            // 3. İsteği gönder
            using (UnityWebRequest www = UnityWebRequest.Post(backendUrl, form))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[API OK] " + www.downloadHandler.text);
                    callback?.Invoke(www.downloadHandler.text);
                }
                else
                {
                    Debug.LogError($"[API ERROR] {www.responseCode} | {www.error} | Body: {www.downloadHandler.text}");
                    callback?.Invoke(null);
                }
            }
        }
        // =======================
        // WAV ENCODER (HEADER FIX)
        // =======================
        // Bu fonksiyonu class'ın en altına yapıştır
        private byte[] EncodeAsWAV(float[] samples, int frequency, int channels)
        {
            using (var memoryStream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(memoryStream))
            {
                // 16-bit PCM
                byte[] bytesData = new byte[samples.Length * 2];

                for (int i = 0; i < samples.Length; i++)
                {
                    float s = Mathf.Clamp(samples[i], -1f, 1f);
                    short val = (short)Mathf.RoundToInt(s * 32767f);
                    byte[] b = BitConverter.GetBytes(val); // little-endian (Windows/Android)
                    b.CopyTo(bytesData, i * 2);
                }

                // WAV header (PCM)
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + bytesData.Length);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1); // PCM
                writer.Write((short)channels);
                writer.Write(frequency);
                writer.Write(frequency * channels * 2); // ByteRate
                writer.Write((short)(channels * 2));     // BlockAlign
                writer.Write((short)16);                 // BitsPerSample

                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(bytesData.Length);
                writer.Write(bytesData);

                return memoryStream.ToArray();
            }
        }

        private float[] DownmixToMono(float[] interleaved, int channels)
        {
            int frames = interleaved.Length / channels;
            float[] mono = new float[frames];

            for (int f = 0; f < frames; f++)
            {
                float sum = 0f;
                int baseIdx = f * channels;
                for (int c = 0; c < channels; c++)
                    sum += interleaved[baseIdx + c];

                mono[f] = sum / channels;
            }
            return mono;
        }

        private float[] ResampleLinear(float[] src, int srcHz, int dstHz)
        {
            if (srcHz == dstHz) return src;
            if (src == null || src.Length < 2) return src;

            int dstLen = Mathf.Max(2, Mathf.RoundToInt(src.Length * (dstHz / (float)srcHz)));
            float[] dst = new float[dstLen];

            float ratio = srcHz / (float)dstHz;

            for (int i = 0; i < dstLen; i++)
            {
                float srcIndex = i * ratio;
                int i0 = Mathf.FloorToInt(srcIndex);
                int i1 = Mathf.Min(i0 + 1, src.Length - 1);
                float t = srcIndex - i0;
                dst[i] = Mathf.Lerp(src[i0], src[i1], t);
            }

            return dst;
        }



        private void UpdateUI(string status, string result)
        {
            if (statusText != null) statusText.text = status;
        }
    }
}