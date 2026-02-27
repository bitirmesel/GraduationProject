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

        public void StartPronunciationSession(List<AssetItem> levelData, string audioBaseUrl = null)
        {
            if (levelData == null || levelData.Count == 0) return;

            if (UIPanelManager.Instance != null)
                UIPanelManager.Instance.ShowPronunciationPanel(true);

            EnsureUIRefs();
            _levelAssets = new List<AssetItem>(levelData);
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

                // Ses dosyasını Cloudinary'den yükle ve çal
                if (!string.IsNullOrEmpty(asset.Audio) && !string.IsNullOrEmpty(_audioBaseUrl))
                {
                    string fullAudioUrl = _audioBaseUrl + asset.Audio;
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
                    UpdateUI($"{wordToProcess} demen bekleniyor...", "");

                    _currentApiTask = new TaskCompletionSource<string>();
                    string sonucJson = await _currentApiTask.Task;

                    // --- KRİTİK DEĞİŞİKLİK: HERHANGİ BİR HATA DURUMUNDA SKORU 0 SAY VE DEVAM ET ---
                    if (string.IsNullOrEmpty(sonucJson))
                    {
                        Debug.LogWarning("[Pronunciation] API Hata döndü veya kilitlenme önlendi. Skor 0 sayılıyor.");
                        UpdateUI("Puanın: 0. Lütfen tekrar dene!", "");
                        continue;
                    }

                    try
                    {
                        var responseList = JsonConvert.DeserializeObject<List<PronunciationResponseDto>>(sonucJson);

                        if (responseList == null || responseList.Count <= 1 || responseList[1].OverallResult == null || responseList[1].OverallResult.Count == 0)
                        {
                            UpdateUI("Puanın: 0. Tekrar dene!", "");
                            continue;
                        }

                        var resultData = responseList[1].OverallResult[0];
                        double puan = resultData.overall_points;

                        Debug.Log($"[Pronunciation] Skor: {puan} | Kelime: {wordToProcess}");

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
                            UpdateUI($"Puanın düşük: {puan:F0}. Tekrar dene!", "");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("[JSON Error] " + ex.Message);
                        UpdateUI("Puanın: 0. Tekrar dene!", "");
                    }
                }
            }

            if (UIPanelManager.Instance != null) UIPanelManager.Instance.ShowVictoryPanel(true);
        }

        // UI'daki "Dinle" butonundan çağrılır — aktif kelimenin sesini tekrar çalar
        public async void PlayCurrentWordAudio()
        {
            if (string.IsNullOrEmpty(_audioBaseUrl)) return;

            // _levelAssets içinde şu an sırası gelen item'ı bul
            // _activeTargetWord kelime adını tutuyor (ör: "kedi", "kopek")
            AssetItem currentItem = _levelAssets.Find(a =>
            {
                string normalized = a.Key.ToLower();
                return _activeTargetWord != null && _activeTargetWord.ToLower().StartsWith(normalized)
                       || normalized == _activeTargetWord?.ToLower();
            });

            if (currentItem == null || string.IsNullOrEmpty(currentItem.Audio)) return;

            string fullAudioUrl = _audioBaseUrl + currentItem.Audio;
            AudioClip clip = await AssetLoader.Instance.GetAudioAsync(fullAudioUrl, currentItem.Audio);

            if (clip != null)
            {
                AudioSource audioSource = GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.Stop(); // Önceki ses varsa durdur
                    audioSource.clip = clip;
                    audioSource.Play();
                }
            }
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

            // Task'ı her koşulda sonlandıracak ve kilitlenmeyi önleyecek yardımcı metod
            System.Action<string> safeCallback = (json) =>
            {
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
                byte[] wavData = WavEncoder.FromAudioClip(_recordingClip, samplePos);
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
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
            formData.Add(new MultipartFormFileSection("audioFile", wavData, "recording.wav", "audio/wav"));
            formData.Add(new MultipartFormDataSection("text", textReference));

            using (UnityWebRequest www = UnityWebRequest.Post(backendUrl, formData))
            {
                yield return www.SendWebRequest();

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