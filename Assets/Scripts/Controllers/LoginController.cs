using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using GraduationProject.Managers;
using GraduationProject.Models; 
using TMPro; // <--- BAK BU EKSİKTİ, BU YÜZDEN HATA VERİYORDU

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
                Debug.LogError("APIManager yok!");
                return;
            }

            var player = await APIManager.Instance.PlayerLoginAsync(email, pass);

            if (player != null)
            {
                // Bilgileri Kaydet
                GameContext.PlayerId = (int)player.PlayerId;
                
                // Sıfırlamalar
                GameContext.SelectedLetterId = 0;
                GameContext.SelectedLetterCode = "";
                GameContext.ImageUrls.Clear(); // Temiz bir başlangıç

                if (statusText != null) statusText.text = $"Hoşgeldin {player.Nickname}!";
                SceneManager.LoadScene("SelectionScene");
            }
            else
            {
                if (statusText != null) statusText.text = "Giriş başarısız.";
                if (loginButton != null) loginButton.interactable = true;
            }
        }
    }
}