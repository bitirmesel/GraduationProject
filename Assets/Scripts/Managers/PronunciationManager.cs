using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using GraduationProject.Models;
using GraduationProject.Utilities;
using Newtonsoft.Json;
using TMPro;

namespace GraduationProject.Managers
{
    public class PronunciationManager : MonoBehaviour
    {
        public static PronunciationManager Instance;

        [Header("Backend Ayarları")]
        [SerializeField] private string backendUrl = "https://backendapi-8nfn.onrender.com/api/pronunciation/check";

        [Header("UI & Görsel Referanslar")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private RectTransform focusPosition;
        [SerializeField] private Transform cardParent;
        [SerializeField] private GameObject cardPrefab;

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

        private void CacheMicrophoneDevice()
        {
            if (Microphone.devices.Length > 0) 
                _microphoneDevice = Microphone.devices[0];
        }

        // =======================
        // FLOW (DÖNGÜ) YÖNETİMİ
        // =======================

        public void StartPronunciationSession(List<AssetItem> levelData)
        {
            if (levelData == null || levelData.Count == 0) return;
            
            if (UIPanelManager.Instance != null) 
                UIPanelManager.Instance.ShowPronunciationPanel(true);

            EnsureUIRefs();
            _levelAssets = new List<AssetItem>(levelData);
            
            GameObject.Find("MemoryRoot")?.SetActive(false);
            StartSequence();
        }

        public async void StartSequence()
        {
            EnsureUIRefs();
            if (_levelAssets == null || _levelAssets.Count == 0) return;

            foreach (var asset in _levelAssets)
            {
                _activeTargetWord = asset.Key;
                GameObject activeCard = CreatePronunciationCard(asset);

                if (activeCard != null && focusPosition != null)
                {
                    activeCard.transform.position = focusPosition.position;
                    activeCard.transform.localScale = Vector3.one * 2.5f;
                }

                if (AudioManager.Instance != null) 
                    await AudioManager.Instance.PlayVoiceOverAsync(asset.Key);

                bool kelimeDogruMu = false;
                while (!kelimeDogruMu)
                {
                    UpdateUI($"{asset.Key} demen bekleniyor...", "");
                    
                    _currentApiTask = new TaskCompletionSource<string>();
                    string sonucJson = await _currentApiTask.Task;

                    if (string.IsNullOrEmpty(sonucJson)) continue;

                    try
                    {
                        var response = JsonConvert.DeserializeObject<PronunciationResponseDto>(sonucJson);
                        if (response?.Score == null) continue;

                        double puan = response.Score.OverallPoints;
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
                        Debug.LogError("JSON Hatası: " + ex.Message);
                    }
                }
            }
            UpdateUI("Tebrikler! Hepsi bitti.", "");
            if (UIPanelManager.Instance != null) UIPanelManager.Instance.ShowVictoryPanel(true);
        }

        // =======================
        // KAYIT VE ANALİZ (MICROPHONE)
        // =======================

        public void StartRecording()
        {
            if (_isRecording) return;
            if (string.IsNullOrEmpty(_microphoneDevice)) CacheMicrophoneDevice();
            
            _recordingClip = Microphone.Start(_microphoneDevice, false, 10, 16000);
            _isRecording = true;
            UpdateUI("Kaydediyor... Konuş!", "");
        }

        public void StopRecording()
        {
            if (!_isRecording) return;

            int samplePos = Microphone.GetPosition(_microphoneDevice);
            Microphone.End(_microphoneDevice);
            _isRecording = false;

            if (samplePos <= 0)
            {
                _currentApiTask?.TrySetResult(null);
                return;
            }

            float[] samples = new float[samplePos * _recordingClip.channels];
            _recordingClip.GetData(samples, 0);

            // Mono ve 16k kontrolü (WAV Header için sabitliyoruz)
            UpdateUI("Analiz ediliyor...", "");
            StartCoroutine(SendAudioToBackend(samples, 16000, _activeTargetWord, (json) =>
            {
                _currentApiTask?.TrySetResult(json);
            }));
        }

        private IEnumerator SendAudioToBackend(float[] soundData, int frequency, string textReference, Action<string> callback)
        {
            byte[] wavData = EncodeAsWAV(soundData, frequency, 1);

            WWWForm form = new WWWForm();
            form.AddBinaryData("audioFile", wavData, "recording.wav", "audio/wav");
            form.AddField("text", textReference);

            using (UnityWebRequest www = UnityWebRequest.Post(backendUrl, form))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(www.downloadHandler.text);
                }
                else
                {
                    Debug.LogError($"API Hatası: {www.error}");
                    UpdateUI("Hata: " + www.error, "");
                    callback?.Invoke(null);
                }
            }
        }

        // =======================
        // YARDIMCI METOTLAR (WAV & UI)
        // =======================

        private byte[] EncodeAsWAV(float[] samples, int frequency, int channels)
        {
            using (var ms = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(ms))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + samples.Length * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channels);
                writer.Write(frequency);
                writer.Write(frequency * channels * 2);
                writer.Write((short)(channels * 2));
                writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(samples.Length * 2);

                foreach (var sample in samples)
                {
                    writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * 32767f));
                }
                return ms.ToArray();
            }
        }

        private void EnsureUIRefs()
        {
            if (statusText == null) statusText = GameObject.Find("StatusText")?.GetComponent<TMP_Text>();
            if (focusPosition == null) focusPosition = GameObject.Find("FocusPosition")?.GetComponent<RectTransform>();
            if (cardParent == null) cardParent = GameObject.Find("CardHolder")?.transform;
        }

        private GameObject CreatePronunciationCard(AssetItem asset)
        {
            if (cardPrefab == null) return null;
            GameObject cardObj = Instantiate(cardPrefab, cardParent);
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
            if (task.Result != null)
            {
                card.Setup(url.GetHashCode(), task.Result, null, null);
                card.FlipOpen();
            }
        }

        private void UpdateUI(string status, string result)
        {
            if (statusText != null) statusText.text = status;
        }
    }
}