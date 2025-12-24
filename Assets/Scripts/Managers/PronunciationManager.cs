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

        [Header("Test UI (Opsiyonel)")]
        public Text statusText;
        public Text resultText;

        [Header("Eksik Referanslar (Inspector'dan Atayın)")]
        [SerializeField] private Transform focusPosition;
        [SerializeField] private GameObject cardHolder;

        private AudioClip _recordingClip;
        private string _microphoneDevice;
        private bool _isRecording = false;

        private List<string> eslesenKelimeler = new List<string>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            if (Microphone.devices.Length > 0)
                _microphoneDevice = Microphone.devices[0];
            else
                UpdateUI("Hata: Mikrofon Bulunamadı!", "");
        }

        // --- ENTEGRASYON METODU ---
        // HATA DÜZELTME: Parametre List<AssetItem> olarak güncellendi (CS1503 çözümü)
        public void StartPronunciationSession(List<AssetItem> levelData)
        {
            eslesenKelimeler.Clear();
            foreach (var data in levelData)
            {
                // HATA DÜZELTME: AssetItem içerisindeki 'Key' kullanılıyor.
                eslesenKelimeler.Add(data.Key);
            }

            StartSequence();
        }

        public void StartRecording()
        {
            if (_isRecording || string.IsNullOrEmpty(_microphoneDevice)) return;

            _recordingClip = Microphone.Start(_microphoneDevice, false, 15, 44100);
            _isRecording = true;
            UpdateUI("Kaydediyor... Konuşun!", "");
        }

        public void StopRecording(string targetText, Action<string> onResult)
        {
            if (!_isRecording) return;

            Microphone.End(_microphoneDevice);
            _isRecording = false;

            UpdateUI($"Gönderiliyor: {targetText}...", "");
            StartCoroutine(SendAudioToBackend(_recordingClip, targetText, onResult));
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

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("API Hatası: " + www.error);
                    UpdateUI("Hata Oluştu!", www.error);
                    callback?.Invoke(null);
                }
                else
                {
                    string jsonResult = www.downloadHandler.text;
                    UpdateUI("Sonuç Geldi!", jsonResult);
                    callback?.Invoke(jsonResult);
                }
            }
        }

        private void UpdateUI(string status, string result)
        {
            if (statusText != null) statusText.text = status;
            if (resultText != null && !string.IsNullOrEmpty(result)) resultText.text = result;
            Debug.Log($"PronunciationManager: {status}");
        }

        private async Task FocusOnCard(string kelime)
        {
            // 1. MemoryRoot altındaki tüm kartları kontrol et ve doğru olanı bul
            MemoryCard[] allCards = GameObject.FindObjectsOfType<MemoryCard>();
            MemoryCard targetCard = null;

            foreach (var card in allCards)
            {
                // Kartın sprite ismi kelimeyle eşleşiyor mu kontrolü
                if (card.gameObject.name.Contains(kelime))
                {
                    targetCard = card;
                    break;
                }
            }

            if (targetCard != null)
            {
                // 2. Kartı hiyerarşide CardHolder altına taşı ki panelin önünde görünsün
                targetCard.transform.SetParent(cardHolder.transform);

                // 3. Kartı merkeze (focusPosition) taşı ve büyüt
                targetCard.transform.position = focusPosition.position;
                targetCard.transform.localScale = Vector3.one * 2.5f; // Kartı 2.5 kat büyüt
            }

            await Task.Yield();
        }

        public async void StartSequence()
        {
            foreach (var kelime in eslesenKelimeler)
            {
                await FocusOnCard(kelime);

                if (AudioManager.Instance != null)
                    await AudioManager.Instance.PlayVoiceOverAsync(kelime);

                bool basarili = false;
                while (!basarili)
                {
                    // Şimdilik test simülasyonu
                    await Task.Delay(3000);
                    basarili = true;
                }
            }

            if (UIPanelManager.Instance != null)
                UIPanelManager.Instance.ShowVictoryPanel(true);
        }
    }
}