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

        [Header("Backend Ayarları")]
        [SerializeField] private string backendUrl = "https://backendapi-8nfn.onrender.com/api/pronunciation/check";

        [Header("UI & Görsel Referanslar")]
        [SerializeField] private TMP_Text statusText;              // StatusText (TMP)
        [SerializeField] private RectTransform focusPosition;      // FocusPosition (RectTransform)
        [SerializeField] private Transform cardParent;             // CardHolder önerilir
        [SerializeField] private GameObject cardPrefab;            // MemoryCard_Prefab

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
                Destroy(gameObject); // Destroy(this) değil
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Log($"[PronunciationManager] Awake OK | id={GetInstanceID()} scene={gameObject.scene.name}");

            // Awake'te bazen boş gelir; Start'ta tekrar cache edeceğiz.
            CacheMicrophoneDevice();
        }

        private void Start()
        {
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Log("[PronunciationManager] Mic izni yok, isteniyor...");
                Permission.RequestUserPermission(Permission.Microphone);
            }
#endif
            // Bazı sistemlerde mic listesi Start'ta hazır olur
            CacheMicrophoneDevice();
        }

        // =======================
        // UI REF HANDLING
        // =======================

        private void EnsureUIRefs()
        {
            // Inactive objeleri de yakalıyoruz.
            if (statusText == null)
                statusText = FindSceneObjectByName<TMP_Text>("StatusText");

            if (focusPosition == null)
                focusPosition = FindSceneObjectByName<RectTransform>("FocusPosition");

            if (cardParent == null)
            {
                var holder = FindSceneObjectByName<RectTransform>("CardHolder");
                if (holder != null) cardParent = holder.transform;
            }

            // CardHolder yoksa FocusPosition parent
            if (cardParent == null && focusPosition != null)
                cardParent = focusPosition.parent;

            Log($"[PronunciationManager] EnsureUIRefs => statusText={(statusText != null)} focusPosition={(focusPosition != null)} cardParent={(cardParent != null)}");
        }

        // Prefab assetlerini elemek için: sadece sahnede yüklü objeleri döndür
        private T FindSceneObjectByName<T>(string targetName) where T : UnityEngine.Object
        {
            var all = Resources.FindObjectsOfTypeAll<T>();
            foreach (var obj in all)
            {
                if (obj == null) continue;
                if (obj.name != targetName) continue;

                // Component ise sahnede mi?
                if (obj is Component c)
                {
                    if (c.gameObject.scene.IsValid() && c.gameObject.scene.isLoaded)
                        return obj;
                }
                else
                {
                    // Non-component ise döndürelim (nadir)
                    return obj;
                }
            }
            return null;
        }

        // =======================
        // SESSION FLOW
        // =======================

        public void StartPronunciationSession(List<AssetItem> levelData)
        {
            if (levelData == null || levelData.Count == 0)
            {
                Debug.LogWarning("[PronunciationManager] StartPronunciationSession: levelData boş.");
                return;
            }

            // Paneli aç
            if (UIPanelManager.Instance != null)
                UIPanelManager.Instance.ShowPronunciationPanel(true);

            // Panel açıldıktan sonra UI referanslarını kesin yakala
            EnsureUIRefs();

            if (focusPosition == null)
            {
                Debug.LogError("[PronunciationManager] FocusPosition bulunamadı! GameScene > Canvas > PronunciationPanel altında 'FocusPosition' ismi birebir olmalı.");
                return;
            }

            if (cardPrefab == null)
            {
                Debug.LogError("[PronunciationManager] Card Prefab atanmamış! Inspector’dan MemoryCard_Prefab bağla.");
                return;
            }

            _levelAssets = new List<AssetItem>(levelData);

            // MemoryRoot kapatılacaksa sadece oyun UI'sı kapansın; Managers/Canvas kapatma.
            var memoryRoot = GameObject.Find("MemoryRoot");
            if (memoryRoot != null) memoryRoot.SetActive(false);

            StartSequence();
        }

        public async void StartSequence()
        {
            EnsureUIRefs();

            if (_levelAssets == null || _levelAssets.Count == 0)
            {
                UpdateUI("Seviye verisi yok!", "");
                return;
            }

            foreach (var asset in _levelAssets)
            {
                _activeTargetWord = asset.Key;

                // Kartı oluştur
                GameObject activeCard = CreatePronunciationCard(asset);
                if (activeCard != null && focusPosition != null)
                {
                    // UI olduğu için burada position yeterli (Canvas mode’a göre)
                    activeCard.transform.position = focusPosition.position;
                    activeCard.transform.localScale = Vector3.one * 2.5f;
                }

                // Kelimeyi seslendir
                if (AudioManager.Instance != null)
                    await AudioManager.Instance.PlayVoiceOverAsync(asset.Key);

                bool kelimeDogruMu = false;

                while (!kelimeDogruMu)
                {
                    UpdateUI($"{asset.Key} demen bekleniyor...", "");

                    _currentApiTask = new TaskCompletionSource<string>();
                    string sonucJson = await _currentApiTask.Task;

                    if (string.IsNullOrEmpty(sonucJson))
                    {
                        UpdateUI("Ses/bağlantı hatası. Tekrar dene.", "");
                        continue;
                    }

                    try
                    {
                        var response = JsonConvert.DeserializeObject<PronunciationResponseDto>(sonucJson);
                        if (response?.Score == null)
                        {
                            UpdateUI("Sonuç okunamadı, tekrar dene.", "");
                            continue;
                        }

                        double puan = response.Score.OverallPoints;
                        Log($"[Pronunciation] Puan: {puan}");

                        if (puan >= 70)
                        {
                            kelimeDogruMu = true;
                            UpdateUI($"Harika! Puanın: {puan:F0}", "");

                            if (AudioManager.Instance != null)
                                AudioManager.Instance.PlayEffect("CorrectSound");

                            await Task.Delay(800);
                            if (activeCard != null) Destroy(activeCard);
                        }
                        else
                        {
                            UpdateUI($"Puanın: {puan:F0}. Tekrar dene!", "");
                            if (AudioManager.Instance != null)
                                await AudioManager.Instance.PlayVoiceOverAsync(asset.Key);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("[Pronunciation] JSON Parse Hatası: " + ex.Message);
                        UpdateUI("Hata oluştu, tekrar dene.", "");
                    }
                }
            }

            UpdateUI("Tüm kelimeler bitti!", "");
            if (UIPanelManager.Instance != null)
                UIPanelManager.Instance.ShowVictoryPanel(true);
        }

        private GameObject CreatePronunciationCard(AssetItem asset)
        {
            EnsureUIRefs();

            if (focusPosition == null)
            {
                Debug.LogError("[PronunciationManager] FocusPosition atanmamış!");
                return null;
            }

            if (cardPrefab == null)
            {
                Debug.LogError("[PronunciationManager] Card Prefab atanmamış!");
                return null;
            }

            Transform parent = cardParent != null ? cardParent : focusPosition.parent;
            GameObject cardObj = Instantiate(cardPrefab, parent);

            MemoryCard cardScript = cardObj.GetComponent<MemoryCard>();
            if (cardScript != null)
            {
                // asset.File: "/images/..." gibi geliyorsa base ile birleştiriyoruz
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
        // MIC
        // =======================

        private void CacheMicrophoneDevice()
        {
            var devices = Microphone.devices;
            if (devices != null && devices.Length > 0)
            {
                _microphoneDevice = devices[0];
                Log("[PronunciationManager] Mic device: " + _microphoneDevice);
            }
            else
            {
                _microphoneDevice = null;
                Log("[PronunciationManager] Microphone.devices boş (Editor/izin/yok).");
            }
        }

        public void StartRecording()
        {
            Debug.Log("[PronunciationManager] StartRecording CLICK");

#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Debug.LogWarning("[PronunciationManager] Mic permission yok. İzin isteniyor...");
                Permission.RequestUserPermission(Permission.Microphone);
                return;
            }
#endif

            if (_isRecording)
            {
                Debug.LogWarning("[PronunciationManager] Zaten kayıt alıyor.");
                return;
            }

            if (string.IsNullOrEmpty(_microphoneDevice))
                CacheMicrophoneDevice();

            Debug.Log($"[MIC] devices={Microphone.devices.Length} device='{_microphoneDevice}' targetWord='{_activeTargetWord}'");

            if (string.IsNullOrEmpty(_microphoneDevice))
            {
                Debug.LogError("[PronunciationManager] Mikrofon cihazı bulunamadı!");
                return;
            }

            _recordingClip = Microphone.Start(_microphoneDevice, false, 10, 44100);
            _isRecording = true;

            UpdateUI("Kaydediyor... Konuş!", "");
        }

        public void StopRecording()
        {
            Debug.Log("[PronunciationManager] StopRecording CLICK");

            if (!_isRecording) return;

            // 1) Gerçek kayıt uzunluğu (kaç sample doldu?)
            int samplePos = Microphone.GetPosition(_microphoneDevice);
            Microphone.End(_microphoneDevice);
            _isRecording = false;

            if (samplePos <= 0)
            {
                Debug.LogError("[PronunciationManager] Mic samplePos=0 (boş kayıt).");
                _currentApiTask?.TrySetResult(null);
                return;
            }

            // 2) Clip’ten sadece dolu kısmı al
            int channels = _recordingClip.channels;
            int frequency = _recordingClip.frequency;

            float[] samples = new float[samplePos * channels];
            _recordingClip.GetData(samples, 0);

            var trimmedClip = AudioClip.Create("trimmed", samplePos, channels, frequency, false);
            trimmedClip.SetData(samples, 0);

            UpdateUI("Analiz ediliyor...", "");

            StartCoroutine(SendAudioToBackend(trimmedClip, _activeTargetWord, (json) =>
            {
                _currentApiTask?.TrySetResult(json);
            }));
        }


        private IEnumerator SendAudioToBackend(AudioClip clip, string textReference, Action<string> callback)
        {
            if (clip == null)
            {
                Debug.LogError("[PronunciationManager] clip null!");
                callback?.Invoke(null);
                yield break;
            }

            if (string.IsNullOrEmpty(textReference))
            {
                Debug.LogWarning("[PronunciationManager] textReference boş. (_activeTargetWord set edilmemiş olabilir)");
            }

            byte[] audioData = WavUtility.FromAudioClip_Mono_PCM16_Wav(clip);


            // WAV header'dan sampleRate okuyalım (byte 24-27)
            int wavSampleRate = BitConverter.ToInt32(audioData, 24);
            short wavChannels = BitConverter.ToInt16(audioData, 22);
            short bitsPerSample = BitConverter.ToInt16(audioData, 34);

            Debug.Log($"[WAV OUT] bytes={audioData.Length} wavHz={wavSampleRate} wavCh={wavChannels} bps={bitsPerSample} clipHz={clip.frequency} clipCh={clip.channels} dur={clip.length:F2}s");


            WWWForm form = new WWWForm();
            form.AddBinaryData("audioFile", audioData, "recording.wav", "audio/wav");
            form.AddField("text", textReference);


            using (UnityWebRequest www = UnityWebRequest.Post(backendUrl, form))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[Pronunciation API OK] " + www.downloadHandler.text);
                    callback?.Invoke(www.downloadHandler.text);
                }
                else
                {
                    Debug.LogError($"[Pronunciation API FAIL] code={www.responseCode} err={www.error} body={www.downloadHandler.text}");
                    callback?.Invoke(null);
                }
            }
        }

        // =======================
        // UI
        // =======================

        private void UpdateUI(string status, string result)
        {
            if (statusText != null)
                statusText.text = status;

            Debug.Log("[UI] " + status);
        }

        private void Log(string msg)
        {
            if (verboseLogs) Debug.Log(msg);
        }
    }
}
