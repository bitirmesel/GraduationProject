using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GraduationProject.Managers;
using GraduationProject.Models;
using TMPro;

namespace GraduationProject.Controllers
{
    public class LoginController : MonoBehaviour
    {
        [Header("UI Referansları")]
        public TMP_InputField emailInput;
        public TMP_InputField passwordInput;
        public Button loginButton;
        public TextMeshProUGUI statusText;

        private void Start()
        {
            // Butonu kod üzerinden bulur ve görevi atar
            if (loginButton != null)
            {
                loginButton.onClick.RemoveAllListeners();
                loginButton.onClick.AddListener(OnLoginClicked);
                Debug.Log("[LOGIN] Buton başarıyla koda bağlandı.");
            }
        }

        public async void OnLoginClicked()
        {
            if (loginButton != null) loginButton.interactable = false;
            if (statusText != null) statusText.text = "Giriş yapılıyor...";

            string email = emailInput != null ? emailInput.text?.Trim() : "";
            string pass = passwordInput != null ? passwordInput.text : "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                if (statusText != null) statusText.text = "Email ve şifre boş olamaz.";
                if (loginButton != null) loginButton.interactable = true;
                return;
            }

            if (APIManager.Instance == null)
            {
                Debug.LogError("[LOGIN] APIManager.Instance bulunamadı!");
                if (statusText != null) statusText.text = "Sistem hatası: APIManager yok.";
                if (loginButton != null) loginButton.interactable = true;
                return;
            }

            Debug.Log($"[LOGIN] Deneniyor... email={email}");

            // ---- LOGIN ----
            var player = await APIManager.Instance.PlayerLoginAsync(email, pass);

            if (player == null)
            {
                Debug.LogWarning("[LOGIN] player=null döndü. Login başarısız.");
                if (statusText != null) statusText.text = "Giriş başarısız. Bilgilerinizi kontrol edin.";
                if (loginButton != null) loginButton.interactable = true;
                return;
            }

            // ---- KRİTİK LOG (ID MAPPING TEŞHİSİ) ----
            // Player modelinde hangi alanlar varsa hepsini burada görürsün.
            // (Player sınıfında olmayan property’leri yazma; compile hatası verir.)
            Debug.Log($"[LOGIN] Player objesi geldi. Nickname={player.Nickname} | PlayerId(property)={player.PlayerId}");

            // Eğer PlayerId 0/negatif geliyorsa burada yakala
            if (player.PlayerId <= 0)
            {
                Debug.LogError($"[LOGIN] HATA: player.PlayerId <= 0 geldi! ({player.PlayerId}) " +
                               "Bu durumda backend yanlış alan dönüyor olabilir veya model mapping yanlış.");
            }

            // ---- GAMECONTEXT SET ----
            // LoginController.cs içindeki ilgili kısım
            Debug.Log($"[LOGIN] API'den Gelen -> Nickname: {player.Nickname}, ID: {player.PlayerId}");

            // Eğer burada ID hala 0 geliyorsa, Backend "id" değil "playerId" ismini kullanıyor olabilir.
            // O durumda [JsonProperty("playerId")] olarak değiştirmelisin.

            GameContext.PlayerId = player.PlayerId;

            // Seçimleri sıfırla
            GameContext.SelectedLetterId = 0;
            GameContext.SelectedLetterCode = "";
            GameContext.IsFocusMode = false;
            GameContext.SelectedDifficulty = 0;
            GameContext.SelectedGameType = null;
            GameContext.SelectedAssetSetId = 0;
            GameContext.SelectedGameId = 0;

            if (GameContext.ImageUrls != null) GameContext.ImageUrls.Clear();
            if (GameContext.AudioUrls != null) GameContext.AudioUrls.Clear();

            // UI
            if (statusText != null) statusText.text = $"Hoşgeldin {player.Nickname}!";

            // SON LOG
            Debug.Log($"[LOGIN] Giriş Başarılı! GameContext.PlayerId set edildi -> {GameContext.PlayerId}");

            // ---- SCENE ----
            SceneManager.LoadScene("SelectionScene");
        }
    }
}
