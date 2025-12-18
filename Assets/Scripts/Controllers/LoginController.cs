using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using GraduationProject.Managers;
using GraduationProject.Utilities; // GameContext buradaysa ekle
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
            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginClicked);
        }

        private async void OnLoginClicked()
        {
            if (loginButton != null) loginButton.interactable = false;
            if (statusText != null) statusText.text = "Giriş yapılıyor...";

            string email = emailInput.text;
            string pass = passwordInput.text;

            if (APIManager.Instance == null)
            {
                Debug.LogError("APIManager bulunamadı!");
                if (loginButton != null) loginButton.interactable = true;
                return;
            }

            var player = await APIManager.Instance.PlayerLoginAsync(email, pass);

            if (player != null)
            {
                // HATANIN ÇÖZÜMÜ: Küçük 'p' yerine büyük 'P' kullanıyoruz (player.PlayerId)
                // Ayrıca uzunluk uyuşmazlığı olmaması için başına (long) veya (int) ekliyoruz
                GameContext.PlayerId = (int)player.PlayerId;

                // Seçimleri sıfırlıyoruz
                GameContext.SelectedLetterId = 0;
                GameContext.SelectedLetterCode = "";
                
                // Eğer ImageUrls bir liste ise temizliyoruz
                if(GameContext.ImageUrls != null) GameContext.ImageUrls.Clear();

                if (statusText != null) statusText.text = $"Hoşgeldin {player.Nickname}!";
                
                Debug.Log($"Giriş Başarılı! ID: {GameContext.PlayerId} kaydedildi.");

                // Sahneye geçiş yapıyoruz (Tek sefer yeterli)
                SceneManager.LoadScene("SelectionScene");
            }
            else
            {
                if (statusText != null) statusText.text = "Giriş başarısız. Bilgilerinizi kontrol edin.";
                if (loginButton != null) loginButton.interactable = true;
            }
        }
    }
}