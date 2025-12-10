using System.Collections.Generic; // List<> kullanmak için bunu ekledik
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
        private string _jwtToken;

        private void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else { Destroy(gameObject); }
        }

        // ─────────────────────────────────────────────────────
        //  PLAYER LOGIN  (Unity'den çocuk girişi)
        //  POST /api/player/auth/login
        // ─────────────────────────────────────────────────────
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
                Debug.LogError("[APIManager] PlayerLogin başarısız, boş response.");
                return null;
            }

            try
            {
                var player = JsonConvert.DeserializeObject<PlayerLoginResponseDto>(response);
                if (player == null)
                {
                    Debug.LogError("[APIManager] PlayerLogin: response deserialize edilemedi.");
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
        //  Normalde: GET /api/player/{id}/tasks
        // ─────────────────────────────────────────────────────
// APIManager.cs içindeki GetTasksAsync fonksiyonunu bununla değiştir:

public async Task<List<TaskItem>> GetTasksAsync(long playerId)
        {
            await Task.Delay(100); 

            List<TaskItem> mockTasks = new List<TaskItem>();
            
            // TÜRKÇE ALFABE - Sırayla bunları butonlara yazacak
            string alfabe = "ABCÇDEFGĞHIİJKLMNOÖPRSŞTUÜVYZ"; 

            for (int i = 0; i < alfabe.Length; i++)
            {
                // Eğer harf sayısı nokta sayısını geçerse döngüyü durdur (Hata almamak için)
                // (Bu kontrolü SelectionController'da da yapıyoruz ama burada da olsun)
                 if (i >= alfabe.Length) break;

                mockTasks.Add(new TaskItem
                {
                    TaskId = i + 1,
                    LetterCode = alfabe[i].ToString(), // Sıradaki harfi al
                    // İlk 3 bitmiş (Yeşil), 4. sırada (Sarı), gerisi kilitli (Gri)
                    Status = (i < 3) ? "Completed" : (i == 3 ? "Assigned" : "Locked"),
                    GameType = 1
                });
            }

            return mockTasks;
        }

        // ─────────────────────────────────────────────────────
        // (İstersen TERAPİST LOGIN'i de burada bırakabilirsin)
        //  POST /api/auth/login
        // ─────────────────────────────────────────────────────
        public async Task<User> TherapistLoginAsync(string email, string password)
        {
            string url = $"{_baseUrl}/api/auth/login";

            var requestData = new LoginRequestDTO
            {
                Username = email,   // backend'de Email kullanıyorsan ona göre güncelle
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

        // ─────────────────────────────────────────────────────
        //  GENERAL HELPERS
        // ─────────────────────────────────────────────────────

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

        // GET istekleri için de lazım olacak (ör: aktif görev listesi)
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
    }
}