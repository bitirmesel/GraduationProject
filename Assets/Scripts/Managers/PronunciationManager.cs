using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;
using System; // Action (Callback) kullanmak için gerekli

namespace GraduationProject.Managers
{
    public class PronunciationManager : MonoBehaviour
    {
        public static PronunciationManager Instance;

        [Header("Backend Ayarları")]
        public string backendUrl = "https://backendapi-8nfn.onrender.com/api/pronunciation/check";

        [Header("Test UI (Opsiyonel)")]
        // Bu alanlar boş kalsa da sistem çalışır, sadece debug için tutuyoruz.
        public Text statusText;        
        public Text resultText;        

        private AudioClip _recordingClip;
        private string _microphoneDevice;
        private bool _isRecording = false;

        private void Awake()
        {
            // Singleton: Sahne değişse bile yok olmasın, her yerden erişilsin
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            // Mikrofon Kontrolü
            if (Microphone.devices.Length > 0)
                _microphoneDevice = Microphone.devices[0];
            else
                UpdateUI("Hata: Mikrofon Bulunamadı!", "");
        }

        // --- KAYDI BAŞLAT ---
        public void StartRecording()
        {
            if (_isRecording || string.IsNullOrEmpty(_microphoneDevice)) return;

            _recordingClip = Microphone.Start(_microphoneDevice, false, 15, 44100);
            _isRecording = true;
            UpdateUI("Kaydediyor... Konuşun!", "");
        }

        // --- KAYDI DURDUR VE GÖNDER (GENERIC) ---
        // targetText: Okunan cümle
        // onResult: Sonuç gelince çalıştırılacak fonksiyon (Callback)
        public void StopRecording(string targetText, Action<string> onResult)
        {
            if (!_isRecording) return;

            Microphone.End(_microphoneDevice);
            _isRecording = false;

            UpdateUI($"Gönderiliyor: {targetText}...", "");

            // API işlemini başlat ve bitince 'onResult' fonksiyonunu çağır
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
                    
                    // Hata durumunda null döndürebiliriz veya hata mesajı
                    callback?.Invoke(null); 
                }
                else
                {
                    string jsonResult = www.downloadHandler.text;
                    UpdateUI("Sonuç Geldi!", jsonResult);

                    // --- SİHİRLİ DOKUNUŞ ---
                    // Sonucu, bizi çağıran fonksiyona geri yolluyoruz.
                    // Burası MemoryGame de olabilir, StoryMode da olabilir.
                    callback?.Invoke(jsonResult);
                }
            }
        }

        // UI Güncelleme (Sadece Debug Amaçlı)
        private void UpdateUI(string status, string result)
        {
            if (statusText != null) statusText.text = status;
            if (resultText != null && !string.IsNullOrEmpty(result)) resultText.text = result;
            Debug.Log($"PronunciationManager: {status}");
        }
    }
}