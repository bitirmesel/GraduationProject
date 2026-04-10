using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using GraduationProject.Models;

namespace GraduationProject.Managers
{
    public class APIManager : MonoBehaviour
    {
        public static APIManager Instance { get; private set; }

        [Header("API Settings")]
        [SerializeField] private string _baseUrl = "https://backendapi-8nfn.onrender.com";
        [SerializeField] private string _audioBaseUrl = "https://res.cloudinary.com/dd6zijhry/video/upload/";

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
                return;
            }

            if (!string.IsNullOrEmpty(_baseUrl) && _baseUrl.EndsWith("/"))
                _baseUrl = _baseUrl.TrimEnd('/');
        }

        public string GetBaseUrl() => _baseUrl;
        public string GetAudioBaseUrl() => _audioBaseUrl;

        // ==========================================
        // 1. AUTHENTICATION (LOGIN & REGISTER)
        // ==========================================

        public async Task<bool> RegisterPlayerAsync(string fullName, string email, string password, string passwordAgain, bool isGoingToClinic)
        {
            string url = $"{_baseUrl}/api/player/auth/register";

            var requestData = new PlayerRegisterRequest
            {
                FullName = fullName,
                Email = email,
                Nickname = email,
                Password = password,
                PasswordAgain = passwordAgain,
                IsGoingToClinic = isGoingToClinic
            };

            string jsonBody = JsonConvert.SerializeObject(requestData);
            string response = await SendPostRequest(url, jsonBody, false);
            return !string.IsNullOrEmpty(response);
        }

        public async Task<PlayerLoginResponseDto> PlayerLoginAsync(string nickname, string password)
        {
            string url = $"{_baseUrl}/api/player/auth/login";
            var requestData = new PlayerLoginRequestDto { Nickname = nickname, Password = password };
            string jsonBody = JsonConvert.SerializeObject(requestData);

            string response = await SendPostRequest(url, jsonBody, false);
            Debug.Log("[API RESPONSE RAW]: " + response);

            if (string.IsNullOrEmpty(response)) return null;

            try { return JsonConvert.DeserializeObject<PlayerLoginResponseDto>(response); }
            catch (Exception ex)
            {
                Debug.LogError("[API] Deserialization hatası: " + ex.Message);
                return null;
            }
        }

        // ==========================================
        // 2. GAME DATA (TASKS & CONFIGS)
        // ==========================================

        public async Task<List<TaskItem>> GetTasksAsync(long playerId)
        {
            string url = $"{_baseUrl}/api/players/{playerId}/tasks?t={DateTime.Now.Ticks}";
            string json = await SendGetRequest(url, false);

            if (string.IsNullOrEmpty(json)) return new List<TaskItem>();
            return JsonConvert.DeserializeObject<List<TaskItem>>(json);
        }

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

        public async Task<AssetSetDto> GetAssetSetAsync(long letterId, string gameType, int difficulty)
        {
            string serverGameType = gameType switch
            {
                "Kelime" => "WORD",
                "Hece" => "SYLLABLE",
                "Cümle" => "SENTENCE",
                _ => gameType
            };

            string url = $"{_baseUrl}/api/assets/sets?letterId={letterId}&gameType={serverGameType}&difficulty={difficulty}";
            string json = await SendGetRequest(url, false);

            if (!string.IsNullOrEmpty(json) && json != "[]" && json != "{}")
            {
                try
                {
                    var realData = JsonConvert.DeserializeObject<AssetSetDto>(json);
                    if (realData != null && realData.items != null && realData.items.Count > 0) return realData;
                }
                catch { /* parse hatası */ }
            }
            return CreateMockAssetData(letterId, serverGameType, difficulty);
        }

        // ==========================================
        // 3. ANALYTICS (SAVE SCORE)
        // ==========================================

        public async Task<bool> SavePronunciationScoreAsync(int score, string targetWord)
        {
            string url = $"{_baseUrl}/api/gamesessions/finish";

            var requestData = new
            {
                playerId = GameContext.PlayerId,
                gameId = 4,
                letterId = GameContext.SelectedLetterId,
                taskId = GameContext.CurrentTaskId,
                score = score,
                targetWord = targetWord,
                maxScore = 100
            };

            string jsonBody = JsonConvert.SerializeObject(requestData);
            string response = await SendPostRequest(url, jsonBody, false);
            return response != null;
        }

        // ==========================================
        // 4. NOTIFICATIONS & FEEDBACKS
        // ==========================================

        public async Task<int> GetUnreadNotificationCount(long playerId)
        {
            string url = $"{_baseUrl}/api/notifications/unread-count/{playerId}";
            string response = await SendGetRequest(url, false);
            if (int.TryParse(response, out int count)) return count;
            return 0;
        }

        public async Task<List<NotificationItem>> GetNotificationsAsync(long playerId)
        {
            string url = $"{_baseUrl}/api/notifications/player/{playerId}";
            string json = await SendGetRequest(url, false);

            if (string.IsNullOrEmpty(json)) return new List<NotificationItem>();
            return JsonConvert.DeserializeObject<List<NotificationItem>>(json);
        }

        public async Task<bool> MarkNotificationAsReadAsync(long notificationId)
        {
            string url = $"{_baseUrl}/api/notifications/{notificationId}/read";
            string response = await SendPostRequest(url, "{}", false);
            return response != null;
        }

        // ==========================================
        // 5. CORE ENGINE (NETWORK REQUESTS)
        // ==========================================

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

        private AssetSetDto CreateMockAssetData(long letterId, string type, int diff)
        {
            return new AssetSetDto
            {
                assetSetId = 1,
                letterId = letterId,
                gameType = type,
                difficulty = diff,
                items = new List<AssetItemDto> { new AssetItemDto { imageUrl = "", audioUrl = "" } }
            };
        }
    }
}