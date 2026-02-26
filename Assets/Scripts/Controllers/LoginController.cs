using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GraduationProject.Managers;
using GraduationProject.Models;
using TMPro;
using System;

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
            // Butonu kod üzerinden bağlar
            if (loginButton != null)
            {
                loginButton.onClick.RemoveAllListeners();
                loginButton.onClick.AddListener(OnLoginClicked);
                Debug.Log("[LOGIN] Buton başarıyla koda bağlandı.");
            }
        }

        public async void OnLoginClicked()
        {
            // Giriş işlemi başlarken butonu kilitliyoruz
            if (loginButton != null) loginButton.interactable = false;
            if (statusText != null) statusText.text = "Giriş yapılıyor...";

            string email = emailInput != null ? emailInput.text?.Trim() : "";
            string pass = passwordInput != null ? passwordInput.text : "";

            // Boş input kontrolü
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                if (statusText != null) statusText.text = "İsim ve şifre boş olamaz.";
                ResetButton(); // Hata durumunda butonu tekrar aç
                return;
            }

            // APIManager kontrolü
            if (APIManager.Instance == null)
            {
                Debug.LogError("[LOGIN] APIManager.Instance bulunamadı!");
                if (statusText != null) statusText.text = "Sistem hatası: Bağlantı kurulamadı.";
                ResetButton();
                return;
            }

            try
            {
                Debug.Log($"[LOGIN] Deneniyor... nickname={email}");

                // ---- API Giriş İsteği ----
                var player = await APIManager.Instance.PlayerLoginAsync(email, pass);

                if (player == null)
                {
                    Debug.LogWarning("[LOGIN] Giriş başarısız: Yanıt boş döndü.");
                    if (statusText != null) statusText.text = "Giriş başarısız. Bilgilerinizi kontrol edin.";
                    ResetButton(); // Tekrar denemeye izin ver
                    return;
                }

                // ---- PlayerId Kontrolü ----
                // Backend'den gelen 'playerId' alanı modele doğru eşleşmeli
                if (player.PlayerId <= 0)
                {
                    Debug.LogError($"[LOGIN] HATA: PlayerId geçersiz! (ID: {player.PlayerId})");
                    if (statusText != null) statusText.text = "Kullanıcı verisi alınamadı.";
                    ResetButton();
                    return;
                }

                // ---- Başarılı Giriş: Context Verilerini Set Et ----
                GameContext.PlayerId = player.PlayerId;

                // Seçimleri ve önceki verileri temizle
                ClearGameContext();

                if (statusText != null) statusText.text = $"Hoşgeldin {player.Nickname}!";
                Debug.Log($"[LOGIN] Giriş Başarılı! PlayerId: {GameContext.PlayerId}");

                // Bir sonraki sahneye geç
                SceneManager.LoadScene("NotificationScene");
            }
            catch (Exception ex)
            {
                // İnternet kesintisi veya sunucu hatası durumunda burası çalışır
                Debug.LogError($"[LOGIN] Beklenmedik Hata: {ex.Message}");
                if (statusText != null) statusText.text = "Bağlantı hatası oluştu.";
                ResetButton();
            }
        }

        // Butonu tekrar tıklanabilir hale getiren yardımcı metod
        private void ResetButton()
        {
            if (loginButton != null) loginButton.interactable = true;
        }

        // Önceki oyun oturumundan kalan verileri temizler
        private void ClearGameContext()
        {
            GameContext.SelectedLetterId = 0;
            GameContext.SelectedLetterCode = "";
            GameContext.IsFocusMode = false;
            GameContext.SelectedDifficulty = 0;
            GameContext.SelectedGameType = null;
            GameContext.SelectedAssetSetId = 0;
            GameContext.SelectedGameId = 0;

            if (GameContext.ImageUrls != null) GameContext.ImageUrls.Clear();
            if (GameContext.AudioUrls != null) GameContext.AudioUrls.Clear();
        }
    }
}