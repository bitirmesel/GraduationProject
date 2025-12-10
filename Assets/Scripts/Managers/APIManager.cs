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
        [SerializeField] private string _baseUrl = "https://api.senin-backend.com/api"; 
        
        // Token'ı private tutuyoruz, dışarıdan kimse değiştiremez.
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
        }

        public async Task<User> Login(string username, string password)
        {
            string url = $"{_baseUrl}/auth/login";
            var requestData = new LoginRequestDTO { Username = username, Password = password };
            string jsonBody = JsonConvert.SerializeObject(requestData);

            // POST İsteği at
            string response = await SendPostRequest(url, jsonBody, requiresAuth: false);

            if (!string.IsNullOrEmpty(response))
            {
                // Gelen JSON'ı User objesine çevir
                User user = JsonConvert.DeserializeObject<User>(response);
                _jwtToken = user.Token; // Token'ı sakla
                Debug.Log($"[APIManager] Login Başarılı: {user.Username}");
                return user;
            }
            return null;
        }

        // --- Helper Methods ---

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

                if (request.result == UnityWebRequest.Result.Success)
                {
                    return request.downloadHandler.text;
                }
                else
                {
                    Debug.LogError($"[API Error] {request.error} : {request.downloadHandler.text}");
                    return null;
                }
            }
        }
    }
}