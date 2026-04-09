using GraduationProject.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Threading.Tasks;
using GraduationProject.Managers;
using System.Collections.Generic;
using System.Linq;

namespace GraduationProject.Controllers
{
    public class NotificationController : MonoBehaviour
    {
        [Header("Panel Yönetimi")]
        public GameObject activeTaskPanel;
        public GameObject emptyStatePanel;
        public GameObject titleHeaderLabel;

        [Header("İçerik Referansları")]
        public TextMeshProUGUI notificationText;
        public Button actionButton; // Bu artık "Okudum / Göreve Git" butonu
        public Button refreshButton;

        private List<TaskItem> _tasks;
        private List<NotificationItem> _notifications; // Yeni Model Listesi

        private void Start()
        {
            if (actionButton != null)
                actionButton.onClick.AddListener(() => _ = OnActionClick());

            if (refreshButton != null)
                refreshButton.onClick.AddListener(() => _ = RefreshAllDataAsync());

            _ = RefreshAllDataAsync();
        }

        public async Task RefreshAllDataAsync()
        {
            long usedPlayerId = GameContext.PlayerId;
            if (usedPlayerId <= 0) return;

            // --- 1. Verileri Çek ---
            // APIManager'da GetNotificationsAsync metodun olduğunu varsayıyoruz (yoksa ekleyelim)
            _notifications = await APIManager.Instance.GetNotificationsAsync(usedPlayerId);
            _tasks = await APIManager.Instance.GetTasksAsync(usedPlayerId);

            int notifCount = _notifications?.Count ?? 0;
            int taskCount = _tasks?.Count ?? 0;

            if (notifCount == 0 && taskCount == 0)
            {
                ShowEmptyState();
                return;
            }

            // UI Hazırla
            if (emptyStatePanel != null) emptyStatePanel.SetActive(false);
            if (activeTaskPanel != null) activeTaskPanel.SetActive(true);
            if (titleHeaderLabel != null) titleHeaderLabel.SetActive(true);

            var sb = new StringBuilder();

            // --- 2. Terapistten Gelen Genel Mesajlar (Notifications) ---
            if (notifCount > 0)
            {
                sb.AppendLine("<size=120%><color=#3498DB>✉️ Terapist Notu</color></size>");
                sb.AppendLine();
                foreach (var notif in _notifications)
                {
                    sb.AppendLine($"<color=#3498DB><b>Not:</b></color> {notif.message}");
                    sb.AppendLine("------------------------------------");
                }
                sb.AppendLine();
            }

            // --- 3. Atanan Görevler ---
            if (taskCount > 0)
            {
                sb.AppendLine("<size=120%><color=#FFA500>📅 Görevlerin</color></size>");
                sb.AppendLine();
                foreach (var task in _tasks)
                {
                    string letter = !string.IsNullOrEmpty(task.letterCode) ? task.letterCode : "?";
                    string game = !string.IsNullOrEmpty(task.gameName) ? task.gameName : "Oyun";
                    sb.AppendLine($"<color=#FFA500>•</color> {letter} Harfi: {game}");
                }
            }

            if (notificationText != null)
                notificationText.text = sb.ToString();

            // Buton ismini duruma göre güncelle
            if (actionButton != null)
            {
                var btnTxt = actionButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnTxt != null) btnTxt.text = notifCount > 0 ? "Okudum!" : "Göreve Git!";
            }
        }

        private async Task OnActionClick()
        {
            // 1. Önce okunmamış bildirimleri temizle (Bu kısım zaten çalışıyor demiştin)
            if (_notifications != null && _notifications.Count > 0)
            {
                foreach (var notif in _notifications)
                {
                    await APIManager.Instance.MarkNotificationAsReadAsync(notif.id);
                }
                // Bildirimler temizlendiği için listeyi tazele, buton "Göreve Git!" olacak
                await RefreshAllDataAsync();
                return; // İlk tıklamada sadece okundu yapar, ikinci tıklamada göreve gider
            }

            // 2. Buton "Göreve Git!" halindeyken (yani bildirim kalmadığında) burası çalışır
            if (_tasks != null && _tasks.Count > 0)
            {
                var targetTask = _tasks[0]; // İlk sıradaki görevi al

                // Verileri Context'e aktar (Oyunun Ali'nin ödevini anlaması için)
                GameContext.CurrentTaskId = targetTask.taskId;
                GameContext.SelectedLetterId = targetTask.letterId;
                GameContext.SelectedLetterCode = targetTask.letterCode;
                GameContext.IsFocusMode = true; // Sadece bu göreve odaklanması için

                Debug.Log($"[Navigation] Göreve gidiliyor: {targetTask.gameName} | Harf ID: {targetTask.letterId}");

                // SAHNE GEÇİŞİ
                // ÖNEMLİ: SelectionScene adının Build Settings'de olduğundan emin ol!
                UnityEngine.SceneManagement.SceneManager.LoadScene("SelectionScene");
            }
            else
            {
                Debug.LogWarning("[Navigation] Gidilecek aktif bir görev bulunamadı.");
            }
        }

        private void ShowEmptyState()
        {
            if (activeTaskPanel != null) activeTaskPanel.SetActive(false);
            if (emptyStatePanel != null) emptyStatePanel.SetActive(true);
            if (titleHeaderLabel != null) titleHeaderLabel.SetActive(false);
        }
    }
}