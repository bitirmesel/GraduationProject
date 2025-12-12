using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using GraduationProject.Models; // GameAssetConfig burada olmalı

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
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else { Destroy(gameObject); }
        }

        // -------------------- PLAYER LOGIN --------------------
        public async Task<PlayerLoginResponseDto> PlayerLoginAsync(string nickname, string password)
        {
            string url = $"{_baseUrl}/api/player/auth/login";

            var requestData = new PlayerLoginRequestDto
            {
                Nickname = nickname,
                Password = password
            };

            string jsonBody = JsonConvert.SerializeObject(requestData);
            string response = await SendPostRequest(url, jsonBody, requiresAuth: false);

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("[APIManager] PlayerLogin boş response döndü.");
                return null;
            }

            try
            {
                var player = JsonConvert.DeserializeObject<PlayerLoginResponseDto>(response);
                if (player == null)
                {
                    Debug.LogError("[APIManager] PlayerLogin deserialize edilemedi.");
                    return null;
                }

                Debug.Log($"[APIManager] Player login başarılı: {player.Nickname} (Id={player.PlayerId})");
                return player;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[APIManager] PlayerLogin parse hatası: " + ex);
                return null;
            }
        }

        // ─────────────────────────────────────────────────────
        //  GET TASKS (Harita için Mock Data)
        // ─────────────────────────────────────────────────────
        public async Task<List<TaskItem>> GetTasksAsync(long playerId)
        {
            await Task.Delay(100);

            List<TaskItem> mockTasks = new List<TaskItem>();
            string[] sessizHarfler = { "B", "C", "Ç", "D", "F", "G", "Ğ", "H", "J", "K", "L", "M", "N", "P", "R", "S", "Ş", "T", "V", "Y", "Z" };

            for (int i = 0; i < sessizHarfler.Length; i++)
            {
                mockTasks.Add(new TaskItem
                {
                    TaskId = i + 1,
                    LetterCode = sessizHarfler[i],
                    Status = (i < 2) ? "Completed" : (i == 2 ? "Assigned" : "Locked"),
                    GameType = 1
                });
            }
            return mockTasks;
        }

        public async Task<User> TherapistLoginAsync(string email, string password)
        {
            string url = $"{_baseUrl}/api/auth/login";

            var requestData = new LoginRequestDTO
            {
                Username = email,
                Password = password
            };

            string jsonBody = JsonConvert.SerializeObject(requestData);
            string response = await SendPostRequest(url, jsonBody, requiresAuth: false);

            if (string.IsNullOrEmpty(response))
                return null;

            var user = JsonConvert.DeserializeObject<User>(response);
            if (user != null && !string.IsNullOrEmpty(user.Token))
            {
                _jwtToken = user.Token;
            }

            return user;
        }

        public async Task<List<PlayerTaskDto>> GetAllTasksForPlayer(long playerId)
        {
            string url = $"{_baseUrl}/api/players/{playerId}/tasks";
            string json = await SendGetRequest(url, requiresAuth: false);

            if (string.IsNullOrEmpty(json))
                return null;

            return JsonConvert.DeserializeObject<List<PlayerTaskDto>>(json);
        }

        // ---------------------------------------------------------
        //  [NEW] ASSET CONFIGURATION (Resim Listesini Çeken Yer)
        // ---------------------------------------------------------
        public async Task<GameAssetConfig> GetGameConfigAsync(long gameId, long letterId)
        {
            // Backend'de tanımladığımız endpoint: api/gameconfig/{gameId}/{letterId}
            string url = $"{_baseUrl}/api/gameconfig/{gameId}/{letterId}";

            // Şu anlık requiresAuth: false yaptık. İlerde token gerekirse true yaparsın.
            string json = await SendGetRequest(url, requiresAuth: false);

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError($"[APIManager] GameConfig alınamadı. URL: {url}");
                return null;
            }

            try
            {
                // JSON'ı bizim oluşturduğumuz GameAssetConfig modeline çevir
                var config = JsonConvert.DeserializeObject<GameAssetConfig>(json);
                return config;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[APIManager] GameConfig parse hatası: " + ex.Message);
                return null;
            }
        }

        // -------------------- HELPERS --------------------
        private async Task<string> SendPostRequest(string url, string jsonBody, bool requiresAuth)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                if (requiresAuth && !string.IsNullOrEmpty(_jwtToken))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {_jwtToken}");
                }

                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

#if UNITY_2020_1_OR_NEWER
                if (request.result == UnityWebRequest.Result.Success)
#else
                if (!request.isNetworkError && !request.isHttpError)
#endif
                {
                    return request.downloadHandler.text;
                }
                else
                {
                    Debug.LogError($"[API Error] {request.responseCode} - {request.error} : {request.downloadHandler.text}");
                    return null;
                }
            }
        }

        public async Task<string> SendGetRequest(string url, bool requiresAuth)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                if (requiresAuth && !string.IsNullOrEmpty(_jwtToken))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {_jwtToken}");
                }

                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

#if UNITY_2020_1_OR_NEWER
                if (request.result == UnityWebRequest.Result.Success)
#else
                if (!request.isNetworkError && !request.isHttpError)
#endif
                {
                    return request.downloadHandler.text;
                }
                else
                {
                    Debug.LogError($"[API Error] {request.responseCode} - {request.error} : {request.downloadHandler.text}");
                    return null;
                }
            }
        }

        public async Task<AssetSetDto> GetAssetSetAsync(long letterId, string gameType, int difficulty)
        {
            string url = $"{_baseUrl}/api/assets/sets?letterId={letterId}&gameType={gameType}&difficulty={difficulty}";

            string json = await SendGetRequest(url, requiresAuth: false);
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<AssetSetDto>(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[APIManager] GetAssetSetAsync parse hatası: " + ex);
                return null;
            }
        }

    }
}