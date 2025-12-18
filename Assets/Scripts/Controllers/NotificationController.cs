using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraduationProject.Managers;
using GraduationProject.Models;
using GraduationProject.Utilities;
using Newtonsoft.Json; // Eğer proje Newtonsoft kullanıyorsa debug için harika olur

namespace GraduationProject.Controllers
{
    public class NotificationController : MonoBehaviour
    {
        [Header("Geliştirici Ayarları")]
        [SerializeField] private int _debugPlayerId = 3;

        [Header("Panel Yönetimi")]
        public GameObject activeTaskPanel;
        public GameObject emptyStatePanel;

        [Header("İçerik Referansları")]
        public TextMeshProUGUI notificationText;
        public Button actionButton;

        private int _targetLevelId = 0;

        private async void Start()
        {
            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(OnActionClick);
            }
            await RefreshTasksAsync();
        }

        public async Task RefreshTasksAsync()
        {
            long currentId = GameContext.PlayerId;
            if (currentId <= 0) currentId = _debugPlayerId;

            var tasks = await APIManager.Instance.GetTasksAsync(currentId);

            if (tasks != null && tasks.Count > 0)
            {
                // GÖREV VARSA: "Yok" yazısını kesinlikle gizle, görevleri göster
                if (emptyStatePanel != null) emptyStatePanel.SetActive(false);
                if (activeTaskPanel != null) activeTaskPanel.SetActive(true);

                // Metni oluştururken sahte veriyi tamamen ezdiğinden emin ol
                string fullText = "<size=120%><b>Bugünün Görevleri</b></size>\n\n";
                foreach (var task in tasks)
                {
                    string letter = !string.IsNullOrEmpty(task.letterCode) ? task.letterCode : "K";
                    string game = !string.IsNullOrEmpty(task.gameName) ? task.gameName : "Görev";
                    fullText += $"<color=#FFA500>●</color> <b>Harf '{letter}'</b> : {game}\n\n";
                }

                if (notificationText != null)
                    notificationText.text = fullText;

                Debug.Log("[UI] Ekran Gerçek Veriyle Güncellendi.");
            }
            else
            {
                ShowEmptyState(); // Görev yoksa sadece boş ekranı göster
            }

            // APIManager.cs içindeki GetTasksAsync metodunu şu satırla güncelle:
            //  string url = $"{baseUrl}/api/players/{playerId}/tasks?nocache={System.DateTime.Now.Ticks}";
        }

        // ... (ShowEmptyState ve OnActionClick fonksiyonları aynen kalacak) ...
        private void ShowEmptyState()
        {
            if (activeTaskPanel != null) activeTaskPanel.SetActive(false);
            if (emptyStatePanel != null) emptyStatePanel.SetActive(true);
        }

        private void OnActionClick()
        {
            // ID göndermiyoruz, sadece "Modu Aç" diyoruz.
            // Çünkü SelectionScene zaten görevleri API'den çekip hangileri olduğunu bilecek.

            GameContext.IsFocusMode = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene("SelectionScene");
        }
    }
}