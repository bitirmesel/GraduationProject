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

        // -------------------- NOTIFICATIONS (EKLENDİ) --------------------
        // Bu metot silindiği için hata alıyordunuz, geri eklendi.
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

        // -------------------- NOTIFICATIONS (YENİ) --------------------

        /// <summary>
        /// Öğrenciye ait tüm okunmamış bildirimleri (terapist mesajlarını) getirir.
        /// </summary>
        public async Task<List<NotificationItem>> GetNotificationsAsync(long playerId)
        {
            // Backend'de yazdığımız yeni endpoint: api/notifications/player/{id}
            string url = $"{_baseUrl}/api/notifications/player/{playerId}";
            string json = await SendGetRequest(url, false);

            if (string.IsNullOrEmpty(json))
            {
                return new List<NotificationItem>();
            }

            try
            {
                // Gelen listeyi NotificationItem modeline çevirir
                return JsonConvert.DeserializeObject<List<NotificationItem>>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIManager] Notification parse hatası: {ex.Message}");
                return new List<NotificationItem>();
            }
        }

        /// <summary>
        /// Belirli bir bildirimi veritabanında "okundu" olarak işaretler.
        /// </summary>
        public async Task<bool> MarkNotificationAsReadAsync(long notificationId)
        {
            // URL'nin sonuna /read eklediğimizden emin oluyoruz
            string url = $"{_baseUrl}/api/notifications/{notificationId}/read";

            // Body boş olsa bile {} göndererek API'yi mutlu ediyoruz
            string jsonBody = "{}";

            Debug.Log($"[API] Okundu isteği gönderiliyor: {url}");
            string response = await SendPostRequest(url, jsonBody, false);

            if (response != null)
            {
                Debug.Log("[API] Başarıyla okundu işaretlendi!");
                return true;
            }
            return false;
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