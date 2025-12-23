using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using GraduationProject.Models;
using System;

namespace GraduationProject.Managers
{
    public class APIManager : MonoBehaviour
    {


        public static APIManager Instance { get; private set; }

        [Header("API Settings")]
        [SerializeField] private string _baseUrl = "https://backendapi-8nfn.onrender.com";

        private string _jwtToken;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            if (!string.IsNullOrEmpty(_baseUrl) && _baseUrl.EndsWith("/"))
                _baseUrl = _baseUrl.TrimEnd('/');
        }

        // -------------------- PLAYER LOGIN --------------------
        public async Task<PlayerLoginResponseDto> PlayerLoginAsync(string nickname, string password)
        {
            string url = $"{_baseUrl}/api/player/auth/login";
            var requestData = new PlayerLoginRequestDto { Nickname = nickname, Password = password };
            string jsonBody = JsonConvert.SerializeObject(requestData);
            string response = await SendPostRequest(url, jsonBody, false);

            if (string.IsNullOrEmpty(response)) return null;
            try { return JsonConvert.DeserializeObject<PlayerLoginResponseDto>(response); }
            catch { return null; }
        }

        // -------------------- GET TASKS --------------------
        public async Task<List<TaskItem>> GetTasksAsync(long playerId)
        {
            // URL'ye benzersiz bir sayı ekleyerek Render'ın hafızasını (cache) devre dışı bırakıyoruz
            string url = $"{_baseUrl}/api/players/{playerId}/tasks?t={System.DateTime.Now.Ticks}";

            Debug.Log($"[API] İstek: {url}");
            string json = await SendGetRequest(url, false);

            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<List<TaskItem>>(json);
        }
        // -------------------- GAME CONFIG --------------------
        public async Task<GameAssetConfig> GetGameConfigAsync(long gameId, long letterId)
        {
            string url = $"{_baseUrl}/api/gameconfig/{gameId}/{letterId}";
            string json = await SendGetRequest(url, false);

            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonConvert.DeserializeObject<GameAssetConfig>(json); }
            catch (Exception ex)
            {
                Debug.LogError("[APIManager] GameConfig parse hatası: " + ex.Message);
                return null;
            }
        }

        // -------------------- ASSET SET (BUTONLARI GETİREN KISIM) --------------------

        public async Task<AssetSetDto> GetAssetSetAsync(long letterId, string gameType, int difficulty)
        {
            // 1. İsim Çevirisi (Backend'deki 'WORD' vs 'Kelime' uyuşmazlığını çözer)
            string serverGameType = gameType;
            if (gameType == "Kelime") serverGameType = "WORD";
            else if (gameType == "Hece") serverGameType = "SYLLABLE";
            else if (gameType == "Cümle") serverGameType = "SENTENCE";

            string url = $"{_baseUrl}/api/assets/sets?letterId={letterId}&gameType={serverGameType}&difficulty={difficulty}";

            Debug.Log($"[API] Backend'den veri isteniyor: {url}");

            string json = await SendGetRequest(url, false);

            // --- GERÇEK VERİ KONTROLÜ ---
            if (!string.IsNullOrEmpty(json) && json != "[]" && json != "{}")
            {
                try
                {
                    AssetSetDto realData = JsonConvert.DeserializeObject<AssetSetDto>(json);
                    if (realData != null && realData.items != null && realData.items.Count > 0)
                    {
                        Debug.Log($"[API] BAŞARILI: Backend'den {realData.items.Count} adet gerçek görev/buton geldi.");
                        return realData; // Gerçek veriyi bulduk, hemen döndür
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("[API] JSON Çözümleme Hatası: " + ex.Message);
                }
            }

            // --- YEDEK VERİ (SADECE HATA VARSA ÇALIŞIR) ---
            Debug.LogWarning("[API] Backend'de veri bulunamadı, geçici butonlar oluşturuluyor.");
            AssetSetDto mockData = new AssetSetDto
            {
                assetSetId = 1,
                letterId = letterId,
                gameType = serverGameType,
                difficulty = difficulty,
                items = new List<AssetItemDto>()
            };

            for (int i = 1; i <= 5; i++)
            {
                mockData.items.Add(new AssetItemDto { imageUrl = "", audioUrl = "" });
            }

            return mockData;
        }

        // -------------------- NOTIFICATIONS --------------------
        public async Task<int> GetUnreadNotificationCount(long playerId)
        {
            string url = $"{_baseUrl}/api/notifications/unread-count/{playerId}";
            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Content-Type", "application/json");
                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = request.downloadHandler.text;
                    if (int.TryParse(jsonResponse, out int count)) return count;
                }
                return 0;
            }
        }

        // -------------------- CORE REQUEST METHODS --------------------
        private async Task<string> SendPostRequest(string url, string jsonBody, bool requiresAuth)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                if (requiresAuth && !string.IsNullOrEmpty(_jwtToken))
                    request.SetRequestHeader("Authorization", $"Bearer {_jwtToken}");

                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                return request.result == UnityWebRequest.Result.Success ? request.downloadHandler.text : null;
            }
        }

        private async Task<string> SendGetRequest(string url, bool requiresAuth)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                if (requiresAuth && !string.IsNullOrEmpty(_jwtToken))
                    request.SetRequestHeader("Authorization", $"Bearer {_jwtToken}");

                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                return request.result == UnityWebRequest.Result.Success ? request.downloadHandler.text : null;
            }
        }
        public async Task<PronunciationResult> CheckPronunciationAsync(byte[] audioData)
        {
            // Mevcut API adresini kullan (Render üzerindeki endpoint)
            string url = $"{_baseUrl}/api/games/check-pronunciation";

            // audioData'yı MultipartFormDataSection olarak gönder
            // API'den dönen JSON'ı PronunciationResult'a çevir
            // ... (UnityWebRequest POST işlemleri)
            return new PronunciationResult { CorrectWords = new List<string> { "kedi", "kuş", "kurbağa", "köpek", "koyun", "kartal" } };
        }
    }


}