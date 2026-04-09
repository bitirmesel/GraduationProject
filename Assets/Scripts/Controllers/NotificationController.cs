using GraduationProject.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Threading.Tasks;
using GraduationProject.Managers;
using System.Collections.Generic;

namespace GraduationProject.Controllers
{
    public class NotificationController : MonoBehaviour
    {
        [Header("Panel Yönetimi")]
        public GameObject activeTaskPanel; // TaskCard objesi
        public GameObject emptyStatePanel; // Txt_Empty (Görev yoksa görünür)
        
        [Tooltip("Inspector'da Txt_Baslik objesini buraya sürükleyin")]
        public GameObject titleHeaderLabel; // Viewport dışındaki sabit başlık

        [Header("İçerik Referansları")]
        [Tooltip("Inspector'da Txt_TaskBody (Scroll içindeki) objesini buraya sürükleyin")]
        public TextMeshProUGUI notificationText;

        public Button actionButton; // Göreve Git butonu
        public Button refreshButton; // Refresh butonu

        // --- GÜNCELLEME: Listeyi sınıf düzeyine taşıdık (Scope hatasını çözer) ---
        private List<TaskItem> _tasks;

        private void Start()
        {
            if (actionButton != null)
                actionButton.onClick.AddListener(OnActionClick);

            if (refreshButton != null)
                refreshButton.onClick.AddListener(() => _ = RefreshTasksAsync());

            // Başlangıçta sabit başlığı gizle
            if (titleHeaderLabel != null) titleHeaderLabel.SetActive(false);

            _ = RefreshTasksAsync();
        }

        public async Task RefreshTasksAsync()
        {
            long usedPlayerId = GameContext.PlayerId;

            if (usedPlayerId <= 0) return;

            // --- EKLEME: Hem görevleri hem feedbackleri çekiyoruz ---
            // 'var' kelimesini sildik, yukarıda tanımladığımız _tasks listesini dolduruyoruz
            _tasks = await APIManager.Instance.GetTasksAsync(usedPlayerId);
            var feedbacks = await APIManager.Instance.GetTherapistFeedbacksAsync(usedPlayerId);

            int taskCount = _tasks?.Count ?? 0;
            int feedbackCount = feedbacks?.Count ?? 0;

            if (taskCount == 0 && feedbackCount == 0)
            {
                ShowEmptyState();
                return;
            }

            if (emptyStatePanel != null) emptyStatePanel.SetActive(false);
            if (activeTaskPanel != null) activeTaskPanel.SetActive(true);
            if (titleHeaderLabel != null) titleHeaderLabel.SetActive(true);

            var sb = new StringBuilder();

            // --- 1. Terapist Mesajlarını Yazdır ---
            if (feedbackCount > 0)
            {
                sb.AppendLine("<size=110%><color=#2ECC71>🌟 Terapist Notları</color></size>");
                sb.AppendLine();
                foreach (var fb in feedbacks)
                {
                    sb.AppendLine($"<color=#2ECC71><b>{fb.therapistName}:</b></color> {fb.comment}");
                    sb.AppendLine($"<size=85%><color=#95A5A6>({fb.targetWord} - %{fb.score})</color></size>");
                    sb.AppendLine();
                }
                sb.AppendLine("------------------------------------");
                sb.AppendLine();
            }

            // --- 2. Mevcut Görev Listeleme Mantığı ---
            if (taskCount > 0)
            {
                sb.AppendLine("<size=110%><color=#FFA500>📅 Görevlerin</color></size>");
                sb.AppendLine();
                foreach (var task in _tasks)
                {
                    string letter = !string.IsNullOrEmpty(task.letterCode) ? task.letterCode : "?";
                    string game = !string.IsNullOrEmpty(task.gameName) ? task.gameName : "Task";

                    sb.AppendLine($"<color=#FFA500>•</color> {letter} Harfi: {game}");
                    sb.AppendLine();
                }
            }

            if (notificationText != null)
                notificationText.text = sb.ToString();

            Debug.Log($"[Notifications] {taskCount} görev, {feedbackCount} feedback yüklendi.");
        }

        private void ShowEmptyState()
        {
            if (activeTaskPanel != null) activeTaskPanel.SetActive(false);
            if (emptyStatePanel != null) emptyStatePanel.SetActive(true);
            if (titleHeaderLabel != null) titleHeaderLabel.SetActive(false);
        }

        private void OnActionClick()
        {
            // --- GÜNCELLEME: Sınıf düzeyindeki _tasks artık burada erişilebilir ---
            if (_tasks != null && _tasks.Count > 0)
            {
                // İlk sıradaki görevin bilgilerini GameContext'e aktar
                GameContext.CurrentTaskId = _tasks[0].taskId;
                GameContext.SelectedLetterId = _tasks[0].letterId;
                GameContext.SelectedLetterCode = _tasks[0].letterCode;
                
                Debug.Log($"[Task] Aktif görev set edildi: {_tasks[0].taskId} | Harf: {_tasks[0].letterCode}");
            }

            GameContext.IsFocusMode = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene("SelectionScene");
        }
    }
}