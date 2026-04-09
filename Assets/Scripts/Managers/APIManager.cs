using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking; // UnityWebRequest için gerekli
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

        // APIManager.cs içerisine, sınıf parantezleri içine ekle
        [SerializeField] private string _audioBaseUrl = "https://res.cloudinary.com/dd6zijhry/video/upload/";

        public string GetAudioBaseUrl()
        {
            return _audioBaseUrl;
        }

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
                Destroy(this);
                return;
            }

            Debug.Log("PERSISTENT PATH = " + Application.persistentDataPath);

            if (!string.IsNullOrEmpty(_baseUrl) && _baseUrl.EndsWith("/"))
                _baseUrl = _baseUrl.TrimEnd('/');
        }

        public string GetBaseUrl()
        {
            return _baseUrl;
        }

        // -------------------- VERİ ANALİTİĞİ: SKOR KAYDETME (YENİ) --------------------
        /// <summary>
        /// Her kelime telaffuzundan sonra skoru backend'e kalıcı olarak kaydeder.
        /// </summary>
        // APIManager.cs
        // APIManager.cs içindeki ilgili metot
        public async Task<bool> SavePronunciationScoreAsync(int score, string targetWord)
{
    string url = $"{_baseUrl}/api/gamesessions/finish";

    var requestData = new
    {
        playerId = GameContext.PlayerId,
        gameId = 4, // Hafıza Oyunu
        letterId = GameContext.SelectedLetterId,
        taskId = GameContext.CurrentTaskId, // KRİTİK: Bu oyun hangi göreve ait?
        score = score,
        targetWord = targetWord, // "kedi", "köpek" vb.
        maxScore = 100
    };

    string jsonBody = JsonConvert.SerializeObject(requestData);
    Debug.Log($"[API] Kelime Skoru Gönderiliyor: {targetWord} (TaskId: {GameContext.CurrentTaskId})");

    string response = await SendPostRequest(url, jsonBody, false);
    return response != null;
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

<<<<<<< Updated upstream
        // -------------------- GET TASKS --------------------
        public async Task<List<TaskItem>> GetTasksAsync(long playerId)
        {
            string url = $"{_baseUrl}/api/players/{playerId}/tasks?t={System.DateTime.Now.Ticks}";
            string json = await SendGetRequest(url, false);

            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<List<TaskItem>>(json);
        }

        // -------------------- FEEDBACKS --------------------
        /// <summary>
        /// Terapistin bu oyuncu için yazdığı geri bildirimleri (mesajları) getirir.
        /// </summary>
        public async Task<List<FeedbackDto>> GetTherapistFeedbacksAsync(long playerId)
        {
            string url = $"{_baseUrl}/api/players/{playerId}/feedbacks";

            // Kendi yardımcı metodun olan SendGetRequest'i kullanarak tutarlılık sağlıyoruz
            string json = await SendGetRequest(url, false);

            if (string.IsNullOrEmpty(json))
            {
                return new List<FeedbackDto>();
            }

            try
            {
                // Backend'den dönen FeedbackResponseDto listesini Unity tarafındaki FeedbackDto'ya çevirir
                return JsonConvert.DeserializeObject<List<FeedbackDto>>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIManager] Feedback parse hatası: {ex.Message}");
                return new List<FeedbackDto>();
            }
        }

        // -------------------- GAME CONFIG --------------------
        public async Task<GameAssetConfig> GetGameConfigAsync(long gameId, long letterId)
        {
            string url = $"{_baseUrl}/api/gameconfig/{gameId}/{letterId}";
            string json = await SendGetRequest(url, false);

            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                // Backend doğrudan GameAssetConfig şemasına uyan JSON döner
                return JsonConvert.DeserializeObject<GameAssetConfig>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError("[APIManager] GameConfig parse hatası: " + ex.Message);
                return null;
            }
        }

        // -------------------- ASSET SET --------------------
        public async Task<AssetSetDto> GetAssetSetAsync(long letterId, string gameType, int difficulty)
        {
            string serverGameType = gameType;
            if (gameType == "Kelime") serverGameType = "WORD";
            else if (gameType == "Hece") serverGameType = "SYLLABLE";
            else if (gameType == "Cümle") serverGameType = "SENTENCE";

            string url = $"{_baseUrl}/api/assets/sets?letterId={letterId}&gameType={serverGameType}&difficulty={difficulty}";
            string json = await SendGetRequest(url, false);

            if (!string.IsNullOrEmpty(json) && json != "[]" && json != "{}")
            {
                try
                {
                    AssetSetDto realData = JsonConvert.DeserializeObject<AssetSetDto>(json);
                    if (realData != null && realData.items != null && realData.items.Count > 0) return realData;
                }
                catch (Exception ex) { Debug.LogError("[API] JSON Çözümleme Hatası: " + ex.Message); }
            }

            // Mock Data (Yedek)
            AssetSetDto mockData = new AssetSetDto
            {
                assetSetId = 1,
                letterId = letterId,
                gameType = serverGameType,
                difficulty = difficulty,
                items = new List<AssetItemDto>()
            };
            for (int i = 1; i <= 5; i++) mockData.items.Add(new AssetItemDto { imageUrl = "", audioUrl = "" });
            return mockData;
        }

        // -------------------- NOTIFICATIONS --------------------
=======
        // -------------------- NOTIFICATIONS (EKLENDİ) --------------------
        // Bu metot silindiği için hata alıyordunuz, geri eklendi.
>>>>>>> Stashed changes
        public async Task<int> GetUnreadNotificationCount(long playerId)
        {
            string url = $"{_baseUrl}/api/notifications/unread-count/{playerId}";
            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Content-Type", "application/json");

                // Auth token varsa ekleyelim
                if (!string.IsNullOrEmpty(_jwtToken))
                    request.SetRequestHeader("Authorization", $"Bearer {_jwtToken}");

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

        // -------------------- GAME CONFIG --------------------
        public async Task<GameAssetConfig> GetGameConfigAsync(long gameId, long letterId)
        {
            string url;
            if (gameId == 4)
                url = $"{_baseUrl}/api/asset-sets?gameId={gameId}&letterId={letterId}";
            else
                url = $"{_baseUrl}/api/gameconfig/{gameId}/{letterId}";

            Debug.Log($"[APIManager] Config İsteniyor: {url}");

            string json = await SendGetRequest(url, false);

            if (string.IsNullOrEmpty(json) || json == "[]")
            {
                Debug.LogError($"[APIManager] Veri BOŞ döndü! (GameId:{gameId})");
                return null;
            }

            try
            {
                GameAssetConfig config = null;
                string cleanJson = json.Trim();

                // 1. Ana JSON Parse
                if (cleanJson.StartsWith("["))
                {
                    var list = JsonConvert.DeserializeObject<List<GameAssetConfig>>(cleanJson);
                    if (list != null && list.Count > 0) config = list[0];
                }
                else
                {
                    config = JsonConvert.DeserializeObject<GameAssetConfig>(cleanJson);
                }

                // 2. AssetJson Çözümleme (KUTUYU AÇMA)
                if (config != null)
                {
                    // Eğer Items boşsa ama AssetJson doluysa, string'i listeye çevir
                    if ((config.Items == null || config.Items.Count == 0) && !string.IsNullOrEmpty(config.AssetJson))
                    {
                        Debug.Log("[APIManager] AssetJson bulundu, listeye çevriliyor...");
                        try
                        {
                            // Senin backend yapına göre AssetJson direkt bir listedir:
                            config.Items = JsonConvert.DeserializeObject<List<AssetItem>>(config.AssetJson);
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"[APIManager] AssetJson parse hatası: {ex.Message}");
                        }
                    }

                    int finalCount = (config.Items != null) ? config.Items.Count : 0;
                    Debug.Log($"[APIManager] Config Hazır. Toplam Asset: {finalCount}");
                }

                return config;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[APIManager] JSON Parse Hatası: " + ex.Message);
                return null;
            }
        }

        // -------------------- GET TASKS --------------------
        public async Task<List<TaskItem>> GetTasksAsync(long playerId)
        {
            string url = $"{_baseUrl}/api/players/{playerId}/tasks?t={DateTime.Now.Ticks}";
            string json = await SendGetRequest(url, false);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<List<TaskItem>>(json);
        }

        // -------------------- ASSET SET --------------------
        public async Task<AssetSetDto> GetAssetSetAsync(long letterId, string gameType, int difficulty)
        {
            string serverGameType = gameType == "Kelime" ? "WORD" : (gameType == "Hece" ? "SYLLABLE" : "SENTENCE");
            string url = $"{_baseUrl}/api/assets/sets?letterId={letterId}&gameType={serverGameType}&difficulty={difficulty}";
            string json = await SendGetRequest(url, false);

            if (!string.IsNullOrEmpty(json) && json != "[]" && json != "{}")
            {
                try
                {
                    AssetSetDto realData = JsonConvert.DeserializeObject<AssetSetDto>(json);
                    if (realData != null && realData.items != null) return realData;
                }
                catch { }
            }
            return null;
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
            await Task.Yield();
            return new PronunciationResult();
        }

        public async Task<long> StartGameSessionAsync()
        {
            string url = $"{_baseUrl}/api/gamesessions/start";

            // GameContext'teki güncel seçimleri gönderiyoruz
            var requestData = new
            {
                playerId = GameContext.PlayerId,
                gameId = 4, // Hafıza oyunu sabit ID'si
                letterId = GameContext.SelectedLetterId,
                assetSetId = GameContext.SelectedAssetSetId
            };

            string jsonBody = JsonConvert.SerializeObject(requestData);
            string response = await SendPostRequest(url, jsonBody, false);

            if (!string.IsNullOrEmpty(response))
            {
                // Backend'den dönen sessionId'yi parse et
                var resObj = JsonConvert.DeserializeObject<Dictionary<string, long>>(response);
                return resObj["sessionId"];
            }
            return 0;
        }


    }
}