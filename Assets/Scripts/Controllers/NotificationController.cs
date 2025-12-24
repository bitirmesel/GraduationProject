using GraduationProject.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Threading.Tasks;
using GraduationProject.Managers;

namespace GraduationProject.Controllers
{
    public class NotificationController : MonoBehaviour
    {
        [Header("Panel Yönetimi")]
        public GameObject activeTaskPanel; // TaskCard objesi [cite: 8]
        public GameObject emptyStatePanel; // Txt_Empty (Görev yoksa görünür) [cite: 40]
        
        [Tooltip("Inspector'da Txt_Baslik objesini buraya sürükleyin")]
        public GameObject titleHeaderLabel; // Viewport dışındaki sabit başlık

        [Header("İçerik Referansları")]
        [Tooltip("Inspector'da Txt_TaskBody (Scroll içindeki) objesini buraya sürükleyin")]
        public TextMeshProUGUI notificationText; 
        
        public Button actionButton; // Göreve Git butonu [cite: 63]
        public Button refreshButton; // Refresh butonu

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

            var tasks = await APIManager.Instance.GetTasksAsync(usedPlayerId);
            int count = tasks?.Count ?? 0;

            if (count == 0)
            {
                ShowEmptyState();
                return;
            }

            if (emptyStatePanel != null) emptyStatePanel.SetActive(false);
            if (activeTaskPanel != null) activeTaskPanel.SetActive(true);
            
            // "Görevlerin:" yazan Txt_Baslik'ın tikini aç
            if (titleHeaderLabel != null) titleHeaderLabel.SetActive(true);

            // Liste içeriğini oluştur (Başlık artık burada DEĞİL)
            var sb = new StringBuilder();
            foreach (var task in tasks)
            {
                string letter = !string.IsNullOrEmpty(task.letterCode) ? task.letterCode : "?"; // [cite: 35]
                string game = !string.IsNullOrEmpty(task.gameName) ? task.gameName : "Task"; // [cite: 33]
                
                // Turuncu nokta ve görev detayını ekle
                sb.AppendLine($"<color=#FFA500>•</color> {letter} Harfi: {game}");
                sb.AppendLine(); 
            }

            // Txt_TaskBody içeriğini güncelle
            if (notificationText != null)
                notificationText.text = sb.ToString();
            
            Debug.Log($"[Tasks] {count} görev listelendi. ID: {usedPlayerId}");
        }

        private void ShowEmptyState()
        {
            if (activeTaskPanel != null) activeTaskPanel.SetActive(false);
            if (emptyStatePanel != null) emptyStatePanel.SetActive(true);
            if (titleHeaderLabel != null) titleHeaderLabel.SetActive(false);
        }

        private void OnActionClick()
        {
            GameContext.IsFocusMode = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene("SelectionScene");
        }
    }
}