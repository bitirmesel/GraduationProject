using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;
using GraduationProject.Models;
using GraduationProject.Utilities;

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

        public void StartPronunciationSession(List<AssetItem> levelData)
        {
            _levelAssets = new List<AssetItem>(levelData);

            // 1. ADIM: Hafıza oyunundaki 12 kartı tamamen gizle
            GameObject memoryRoot = GameObject.Find("MemoryRoot");
            if (memoryRoot != null) memoryRoot.SetActive(false);

            StartSequence();
        }

        public async void StartSequence()
        {
            foreach (var asset in _levelAssets)
            {
                _activeTargetWord = asset.Key;

                // 2. ADIM: Telaffuz için tek bir kart oluştur ve merkeze yerleştir
                GameObject activeCard = CreatePronunciationCard(asset);
                activeCard.transform.position = focusPosition.position;
                activeCard.transform.localScale = Vector3.one * 2.5f; // Kartı büyüt

                // 3. ADIM: Kelimeyi seslendir
                if (AudioManager.Instance != null)
                    await AudioManager.Instance.PlayVoiceOverAsync(asset.Key);

                bool kelimeDogruMu = false;
                while (!kelimeDogruMu)
                {
                    UpdateUI($"{asset.Key} demen bekleniyor...", "");

                    // 4. ADIM: Mikrofon girişini bekle
                    _currentApiTask = new TaskCompletionSource<string>();
                    string sonucJson = await _currentApiTask.Task;

                    // API kontrolü (Örn: "isMatch":true)
                    if (!string.IsNullOrEmpty(sonucJson) && sonucJson.Contains("\"isMatch\":true"))
                    {
                        kelimeDogruMu = true;
                        UpdateUI("Harika! Doğru bildin.", "");
                        await Task.Delay(1000);
                        Destroy(activeCard); // Doğru bilinince kartı yok et
                    }
                    else
                    {
                        UpdateUI("Tekrar dene!", "");
                        if (AudioManager.Instance != null)
                            await AudioManager.Instance.PlayVoiceOverAsync(asset.Key);
                    }
                }
            }

            // Tüm 6 kelime bitince
            if (UIPanelManager.Instance != null)
                UIPanelManager.Instance.ShowVictoryPanel(true);
        }

        private GameObject CreatePronunciationCard(AssetItem asset)
        {
            // 1. Prefab'dan taze bir kart kopyala
            GameObject cardObj = Instantiate(cardPrefab, focusPosition.parent);

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
                // CardBackSprite kısmına hafıza oyunundaki arka kapak resmini de verebilirsin
                card.Setup(url.GetHashCode(), loadedSprite, null, null);
                card.FlipOpen(); // Kartın resmini hemen göster
            }
        }

        public void StartRecording()
        {
            if (_isRecording || string.IsNullOrEmpty(_microphoneDevice)) return;
            _recordingClip = Microphone.Start(_microphoneDevice, false, 10, 44100);
            _isRecording = true;
            UpdateUI("Kaydediyor... Konuşun!", "");
        }

        public void StopRecording()
        {
            if (!_isRecording) return;
            Microphone.End(_microphoneDevice);
            _isRecording = false;
            StartCoroutine(SendAudioToBackend(_recordingClip, _activeTargetWord, (json) =>
            {
                _currentApiTask?.TrySetResult(json);
            }));
        }

        private IEnumerator SendAudioToBackend(AudioClip clip, string textReference, Action<string> callback)
        {
            byte[] audioData = WavUtility.FromAudioClip(clip);
            WWWForm form = new WWWForm();
            form.AddBinaryData("audioFile", audioData, "recording.wav", "audio/wav");
            form.AddField("text", textReference);

            using (UnityWebRequest www = UnityWebRequest.Post(backendUrl, form))
            {
                yield return www.SendWebRequest();
                callback?.Invoke(www.result == UnityWebRequest.Result.Success ? www.downloadHandler.text : null);
            }
        }

        private void UpdateUI(string status, string result) { if (statusText != null) statusText.text = status; }
    }
}