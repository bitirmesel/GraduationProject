
using UnityEngine;              // Unity'nin temel kütüphanesi (GameObject, Transform vb. için)
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;   // API iletişimi için
using UnityEngine.UI;           // Text, Image ve Buttonlar için
using System;
using System.Threading.Tasks;
using GraduationProject.Models; // Modellerin (DTO) için
using GraduationProject.Utilities; // WavUtility vb. için
using UnityEngine.Android;      // Mikrofon izinleri için (Permission hatasını çözer)
using Newtonsoft.Json;


namespace GraduationProject.Managers
{
    public class PronunciationManager : MonoBehaviour
    {
        public static PronunciationManager Instance;

        [Header("Backend Ayarları")]
        public string backendUrl = "https://backendapi-8nfn.onrender.com/api/pronunciation/check";

        [Header("UI & Görsel Referanslar")]
        public Text statusText;
        [SerializeField] private Transform focusPosition; // Kartın duracağı merkez nokta
        [SerializeField] private GameObject cardPrefab;    // Kart görseli için prefab

        private AudioClip _recordingClip;
        private string _microphoneDevice;
        private bool _isRecording = false;

        private List<AssetItem> _levelAssets = new List<AssetItem>();
        private TaskCompletionSource<string> _currentApiTask;
        private string _activeTargetWord;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (Microphone.devices.Length > 0) _microphoneDevice = Microphone.devices[0];

        }

        private void Start()
        {
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
#endif
        }

        public void StartPronunciationSession(List<AssetItem> levelData)
        {
            _levelAssets = new List<AssetItem>(levelData);

            // 1. ADIM: Hafıza oyunundaki 12 kartı tamamen gizle
            GameObject memoryRoot = GameObject.Find("MemoryRoot");
            if (memoryRoot != null) memoryRoot.SetActive(false);

            // Izgara konteynerini de bulup kapatmak iyi olabilir (MemoryGameManager referansına göre)
            // Ancak MemoryGameManager.OnGameComplete içinde zaten kapatmıştık.

            StartSequence();
        }

        public async void StartSequence()
        {
            foreach (var asset in _levelAssets)
            {
                _activeTargetWord = asset.Key;

                // 2. ADIM: Telaffuz için tek bir kart oluştur ve merkeze yerleştir
                GameObject activeCard = CreatePronunciationCard(asset);
                if (activeCard != null)
                {
                    activeCard.transform.position = focusPosition.position;
                    activeCard.transform.localScale = Vector3.one * 2.5f; // Kartı büyüt
                }

                // 3. ADIM: Kelimeyi seslendir
                if (AudioManager.Instance != null)
                    await AudioManager.Instance.PlayVoiceOverAsync(asset.Key);

                bool kelimeDogruMu = false;

                // --- DÖNGÜ BAŞLIYOR ---
                while (!kelimeDogruMu)
                {
                    UpdateUI($"{asset.Key} demen bekleniyor...", "");

                    // 4. ADIM: Mikrofon girişini bekle (StopRecording çağrılana kadar burası bekler)
                    _currentApiTask = new TaskCompletionSource<string>();
                    string sonucJson = await _currentApiTask.Task;

                    // --- PUAN KONTROLÜ (GÜNCELLENDİ) ---
                    if (!string.IsNullOrEmpty(sonucJson))
                    {
                        try
                        {
                            // JSON'ı modele çevir
                            var response = JsonConvert.DeserializeObject<PronunciationResponseDto>(sonucJson);

                            if (response != null && response.Score != null)
                            {
                                double puan = response.Score.OverallPoints;
                                Debug.Log($"[Pronunciation] Gelen Puan: {puan}");

                                // EŞİK DEĞER: 70 PUAN
                                if (puan >= 70)
                                {
                                    kelimeDogruMu = true;
                                    UpdateUI($"Harika! Puanın: {puan:F0}", "");

                                    // Başarı sesi
                                    if (AudioManager.Instance != null)
                                        AudioManager.Instance.PlayEffect("CorrectSound"); // Varsa

                                    await Task.Delay(1500); // 1.5 saniye bekle
                                    if (activeCard != null) Destroy(activeCard); // Kartı yok et ve sıradakine geç
                                }
                                else
                                {
                                    UpdateUI($"Puanın: {puan:F0}. Tekrar dene!", "");

                                    // Başarısızlık sesi ve kelimeyi tekrar hatırlat
                                    if (AudioManager.Instance != null)
                                    {
                                        // AudioManager.Instance.PlayEffect("WrongSound");
                                        await AudioManager.Instance.PlayVoiceOverAsync(asset.Key);
                                    }
                                }
                            }
                            else
                            {
                                UpdateUI("Sonuç okunamadı, tekrar dene.", "");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError("JSON Parse Hatası: " + ex.Message);
                            UpdateUI("Hata oluştu, tekrar dene.", "");
                        }
                    }
                    else
                    {
                        UpdateUI("Ses veya bağlantı hatası, tekrar dene.", "");
                    }
                }
            }

            // Tüm kelimeler bitince
            UpdateUI("Tüm kelimeler bitti!", "");
            if (UIPanelManager.Instance != null)
                UIPanelManager.Instance.ShowVictoryPanel(true);
        }

        private GameObject CreatePronunciationCard(AssetItem asset)
        {
            if (focusPosition == null)
            {
                Debug.LogError("FocusPosition atanmamış!");
                return null;
            }

            // 1. Prefab'dan taze bir kart kopyala
            GameObject cardObj = Instantiate(cardPrefab, focusPosition.parent); // Parent'ı canvas/panel olsun diye

            // 2. Kartın üzerindeki MemoryCard scriptine eriş (Setup için)
            MemoryCard cardScript = cardObj.GetComponent<MemoryCard>();

            if (cardScript != null)
            {
                // 3. AssetLoader üzerinden resmin URL'sini al ve karta yükle
                string fullUrl = APIManager.Instance.GetBaseUrl() + asset.File;

                // Asenkron olarak resmi yükle ve kartın Setup metodunu çağır
                StartCoroutine(LoadCardSprite(cardScript, fullUrl, asset.File));
            }

            return cardObj;
        }

        private IEnumerator LoadCardSprite(MemoryCard card, string url, string fileName)
        {
            // AssetLoader'ı kullanarak resmi internetten çekiyoruz
            var task = AssetLoader.Instance.GetSpriteAsync(url, fileName);
            yield return new WaitUntil(() => task.IsCompleted);

            Sprite loadedSprite = task.Result;
            if (loadedSprite != null)
            {
                // Kartı ön yüzü açık (flipped) ve doğru resimle hazırla
                // CardBackSprite null olabilir çünkü bu kart hiç dönmeyecek
                card.Setup(url.GetHashCode(), loadedSprite, null, null);
                card.FlipOpen(); // Kartın resmini hemen göster
            }
        }

        public void StartRecording()
        {
            if (_isRecording || string.IsNullOrEmpty(_microphoneDevice)) return;

            // Unity Mikrofonu başlatır (maks 10 sn)
            _recordingClip = Microphone.Start(_microphoneDevice, false, 10, 44100);
            _isRecording = true;
            UpdateUI("Kaydediyor... Konuşun!", "");
        }

        public void StopRecording()
        {
            if (!_isRecording) return;

            Microphone.End(_microphoneDevice);
            _isRecording = false;

            UpdateUI("Analiz ediliyor...", "");

            // Coroutine ile backend'e yolla
            StartCoroutine(SendAudioToBackend(_recordingClip, _activeTargetWord, (json) =>
            {
                // Sonuç geldiğinde TaskCompletionSource'u tetikle
                // Böylece StartSequence döngüsü kaldığı yerden devam eder
                _currentApiTask?.TrySetResult(json);
            }));
        }

        private IEnumerator SendAudioToBackend(AudioClip clip, string textReference, Action<string> callback)
        {
            if (clip == null) { yield break; }

            byte[] audioData = WavUtility.FromAudioClip(clip);
            WWWForm form = new WWWForm();

            // --- DÜZELTME BURADA ---
            // Sunucu kesinlikle "audioFile" istiyor
            form.AddBinaryData("audioFile", audioData, "recording.wav", "audio/wav");

            form.AddField("text", textReference);

            using (UnityWebRequest www = UnityWebRequest.Post(backendUrl, form))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[API Başarılı]: {www.downloadHandler.text}");
                    callback?.Invoke(www.downloadHandler.text);
                }
                else
                {
                    Debug.LogError($"[API Hatası] Kod: {www.responseCode} | Hata: {www.downloadHandler.text}");
                    callback?.Invoke(null);
                }
            }
        }

        private void UpdateUI(string status, string result)
        {
            if (statusText != null) statusText.text = status;
            Debug.Log($"[UI]: {status}");
        }
    }
}