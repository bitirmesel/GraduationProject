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
        [SerializeField] private UnityEngine.UI.Button listenButton; // 🔊 Dinle butonu

        private AudioClip _recordingClip;
        private string _microphoneDevice;
        private bool _isRecording;

        private List<AssetItem> _levelAssets = new List<AssetItem>();
        private string _imageBaseUrl;
        private string _audioBaseUrl;
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
            Debug.Log("[PronunciationManager] Start() çalıştı.");
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
#endif
            CacheMicrophoneDevice();

            // Sahnede Inspector'dan bağlanmamışsa bile biz kodla zorluyoruz
            if (listenButton != null)
            {
                Debug.Log("[PronunciationManager] listenButton REFERANSI BULUNDU! Koda onClick Event'i ekleniyor.");
                listenButton.onClick.RemoveAllListeners();
                listenButton.onClick.AddListener(PlayCurrentWordAudio);
            }
            else
            {
                Debug.LogError("[PronunciationManager] HATA: listenButton REFERANSI NULL! Lütfen Inspector'dan butonu sürükleyin.");
            }
        }

        private void CacheMicrophoneDevice()
        {
            if (Microphone.devices.Length > 0)
                _microphoneDevice = Microphone.devices[0];
        }

        public void StartPronunciationSession(List<AssetItem> levelData, string imageBaseUrl, string audioBaseUrl = null)
        {
            Debug.Log($"[PronunciationManager] Oturum Başlatılıyor... Gelen imageBaseUrl: '{imageBaseUrl}', audioBaseUrl: '{audioBaseUrl}', Liste uzunluğu: {levelData?.Count}");

            if (levelData == null || levelData.Count == 0) return;

            if (UIPanelManager.Instance != null)
                UIPanelManager.Instance.ShowPronunciationPanel(true);

            EnsureUIRefs();
            _levelAssets = new List<AssetItem>(levelData);
            _imageBaseUrl = imageBaseUrl;
            _audioBaseUrl = audioBaseUrl;

            GameObject gridContainer = GameObject.Find("GridContainer");
            if (gridContainer != null) gridContainer.SetActive(false);

            StartSequence();
        }

        public async void StartSequence()
{
    EnsureUIRefs();
    if (_levelAssets == null || _levelAssets.Count == 0) return;

    foreach (var asset in _levelAssets)
    {
        string wordToProcess = asset.Key;
        // Karakter düzeltmeleri
        if (wordToProcess.ToLower() == "kopek") wordToProcess = "köpek";
        if (wordToProcess.ToLower() == "kus") wordToProcess = "kuş";
        if (wordToProcess.ToLower() == "kedi") wordToProcess = "kedi";
        if (wordToProcess.ToLower() == "kurbaga") wordToProcess = "kurbağa";

        _activeTargetWord = wordToProcess;

        GameObject activeCard = CreatePronunciationCard(asset);
        if (activeCard != null && focusPosition != null)
        {
            activeCard.transform.position = focusPosition.position;
            activeCard.transform.localScale = Vector3.one * 2.5f;
        }

        // 1. SESİ ÇAL (Cloudinary üzerinden)
        if (!string.IsNullOrEmpty(asset.Audio) && APIManager.Instance != null)
        {
            string fullAudioUrl = APIManager.Instance.GetAudioBaseUrl() + asset.Audio;
            AudioClip voiceClip = await AssetLoader.Instance.GetAudioAsync(fullAudioUrl, asset.Audio);
            if (voiceClip != null)
            {
                AudioSource audioSource = GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.clip = voiceClip;
                    audioSource.Play();
                    while (audioSource.isPlaying) await Task.Yield();
                }
            }
        }

        bool kelimeDogruMu = false;

        while (!kelimeDogruMu)
        {
            UpdateUI($"{wordToProcess} bekleniyor...", "");
            _currentApiTask = new TaskCompletionSource<string>();

            string sonucJson = await _currentApiTask.Task;

            if (!string.IsNullOrEmpty(sonucJson))
            {
                var responseList = JsonConvert.DeserializeObject<List<PronunciationResponseDto>>(sonucJson);
                if (responseList != null && responseList.Count > 1)
                {
                    var puan = (int)responseList[1].OverallResult[0].overall_points;
                    UpdateUI($"Puanın: {puan}", "");

                    // VERİTABANINA KAYDET
                    await APIManager.Instance.SavePronunciationScoreAsync(puan, wordToProcess);

                    if (puan >= 70)
                    {
                        kelimeDogruMu = true;
                        await Task.Delay(1000);
                        if (activeCard != null) Destroy(activeCard);
                    }
                }
            }
        }
    }
    if (UIPanelManager.Instance != null) UIPanelManager.Instance.ShowVictoryPanel(true);
}

        public void StartRecording()
        {
            if (_isRecording) return;
            if (Microphone.IsRecording(_microphoneDevice)) Microphone.End(_microphoneDevice);
            if (string.IsNullOrEmpty(_microphoneDevice)) CacheMicrophoneDevice();

            // Kayıt süresince Dinle butonunu kapat
            if (listenButton != null) listenButton.interactable = false;

            _recordingClip = Microphone.Start(_microphoneDevice, false, 10, 16000);
            _isRecording = true;
            UpdateUI("Kaydediyor... Konuş!", "");
        }

        public void StopRecording()
        {
            if (!_isRecording) return;

            // Kayıt bitince Dinle butonunu geri aç
            if (listenButton != null) listenButton.interactable = true;

            int samplePos = Microphone.GetPosition(_microphoneDevice);
            Microphone.End(_microphoneDevice);
            _isRecording = false;
            float stopRecordingTime = Time.realtimeSinceStartup;

            // Task'ı her koşulda sonlandıracak ve kilitlenmeyi önleyecek yardımcı metod
            System.Action<string> safeCallback = (json) =>
            {
                float callbackTime = Time.realtimeSinceStartup;
                Debug.Log($"[PronunciationTiming] Callback alındı, toplam süre (StopRecording->Callback): {callbackTime - stopRecordingTime:F3} sn");
                if (_currentApiTask != null && !_currentApiTask.Task.IsCompleted)
                {
                    _currentApiTask.TrySetResult(json);
                }
            };

            if (samplePos <= 0)
            {
                UpdateUI("Ses alınamadı!", "");
                safeCallback("");
                return;
            }

            try
            {
                float encodeStart = Time.realtimeSinceStartup;
                byte[] wavData = WavEncoder.FromAudioClip(_recordingClip, samplePos);
                float encodeEnd = Time.realtimeSinceStartup;
                Debug.Log($"[PronunciationTiming] WAV encode süresi: {encodeEnd - encodeStart:F3} sn, örnek sayısı: {samplePos}");

                UpdateUI("Analiz ediliyor...", "");
                StartCoroutine(SendAudioToBackend(wavData, _activeTargetWord, safeCallback));
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Kayıt işleme hatası: " + ex.Message);
                safeCallback("");
            }
        }

        private IEnumerator SendAudioToBackend(byte[] wavData, string textReference, Action<string> callback)
        {
            float requestStart = Time.realtimeSinceStartup;
            Debug.Log($"[PronunciationTiming] Backend isteği başlıyor. Payload uzunluğu: {wavData?.Length ?? 0} byte, text='{textReference}'");

            List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
            formData.Add(new MultipartFormFileSection("audioFile", wavData, "recording.wav", "audio/wav"));
            formData.Add(new MultipartFormDataSection("text", textReference));

            using (UnityWebRequest www = UnityWebRequest.Post(backendUrl, formData))
            {
                yield return www.SendWebRequest();
                float requestEnd = Time.realtimeSinceStartup;
                Debug.Log($"[PronunciationTiming] Backend isteği bitti. Süre: {requestEnd - requestStart:F3} sn, HTTP Kod: {www.responseCode}, Result: {www.result}");

                // --- BURASI KRİTİK: HATA NE OLURSA OLSUN KİLİTLENMEYİ ÖNLEMEK İÇİN BOŞ STRING DÖN ---
                if (www.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(www.downloadHandler.text);
                }
                else
                {
                    // HTTP 400, 500 veya internet hatası fark etmez, burası çalışır
                    Debug.LogWarning($"[Backend Hatası] Kod: {www.responseCode}. Skor 0 sayılıyor.");
                    callback?.Invoke("");
                }
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
                if (string.IsNullOrEmpty(_imageBaseUrl))
                {
                    Debug.LogError("[PronunciationManager] HATA: _imageBaseUrl boş, kart görseli yüklenemiyor.");
                    return cardObj;
                }

                string fullUrl = _imageBaseUrl + asset.File;
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

        // Türkçe karakterleri düzleştirerek eşleştirme kolaylığı sağlar
        private string NormalizeKey(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            string s = value.ToLower();
            s = s.Replace("ö", "o")
                 .Replace("ü", "u")
                 .Replace("ş", "s")
                 .Replace("ğ", "g")
                 .Replace("ı", "i")
                 .Replace("ç", "c");

            return s;
        }

        public async void PlayCurrentWordAudio()
{
    if (string.IsNullOrEmpty(_activeTargetWord) || APIManager.Instance == null) return;
    
    string fullAudioUrl = APIManager.Instance.GetAudioBaseUrl() + _activeTargetWord;
    AudioClip clip = await AssetLoader.Instance.GetAudioAsync(fullAudioUrl, _activeTargetWord);
    
    if (clip != null)
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}

        private void UpdateUI(string status, string result)
        {
            if (statusText != null) statusText.text = status;
        }
    }
}